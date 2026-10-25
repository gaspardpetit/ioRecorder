using NAudio.CoreAudioApi;
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ioRecord
{
    public class ioRecorder
    {
        public bool IsRecording { get; private set; }
        public bool DiagnosticLogging { get; set; }
        public List<MMDevice> InputDevices { get; private set; }
        public List<MMDevice> LoopbackDevices { get; private set; }
        public string DeviceDiscoveryError { get; private set; }
        public DateTime _startTime { get; private set; }
        public double InputLevel { get; private set; }

        private CaptureFile _inputCapture;
        private CaptureFile _loopbackCapture;
        public double LoopbackLevel { get; private set; }
        private volatile bool muted;
        public bool IsMuted { get { return muted; } set { muted = value; } }
        private WaveFormat _waveFormat;
        private string _outputFilePath;
        private bool _encodeToMP3 = false;
        private Stopwatch _clock;
        private readonly object transcriptGate = new object();
        private volatile bool captureComplete;
        private bool outputOnly;
        private long inputDataOffset;
        private long loopbackDataOffset;
        public TimeSpan ElapsedTime => _clock == null ? TimeSpan.Zero : _clock.Elapsed;

        public ioRecorder()
        {
            IsRecording = false;
            _waveFormat = new WaveFormat(44100, 16, 2);
        }

        public void LoadDevices()
        {
            InputDevices = new List<MMDevice>();
            LoopbackDevices = new List<MMDevice>();
            DeviceDiscoveryError = null;
            try
            {
                var enumerator = new MMDeviceEnumerator();
                InputDevices = GetAudioDevices(enumerator, DataFlow.Capture);
                LoopbackDevices = GetAudioDevices(enumerator, DataFlow.Render);
            }
            catch (Exception ex)
            {
                DeviceDiscoveryError = "Audio devices are unavailable: " + ex.GetBaseException().Message;
            }
        }

        private static List<MMDevice> GetAudioDevices(MMDeviceEnumerator enumerator, DataFlow dataFlow)
        {
            var devices = new List<MMDevice>();
            MMDevice defaultDevice = null;
            try { defaultDevice = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Communications); }
            catch (COMException) { } // A remote session may have no default endpoint.
            catch (InvalidOperationException) { }
            if (defaultDevice != null) devices.Add(defaultDevice);
            try
            {
                foreach (var device in enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active))
                    if (defaultDevice == null || device.ID != defaultDevice.ID)
                        devices.Add(device);
            }
            catch (COMException) { } // Keep any default endpoint already found.
            catch (InvalidOperationException) { }
            return devices;
        }
        // A source WAV is indexed by elapsed recording time, including intervals in
        // which WASAPI loopback supplies no buffers at all.
        private sealed class CaptureFile : IDisposable
        {
            private readonly WasapiCapture capture;
            private readonly WaveFileWriter writer;
            private readonly Stopwatch clock;
            private readonly Action<double> setLevel;
            private readonly Func<bool> isMuted;
            private readonly object gate = new object();
            private readonly ManualResetEventSlim stopped = new ManualResetEventSlim(false);
            private readonly byte[] silence = new byte[16384];
            private long framesWritten;
            private Exception error;
            public long FirstBufferMilliseconds { get; private set; } = -1;
            public long BuffersReceived { get; private set; }
            public long SilenceFrames { get; private set; }
            public WaveFormat Format => writer.WaveFormat;
            public long DataOffset { get; }

            public CaptureFile(WasapiCapture capture, string path, Stopwatch clock,
                Action<double> setLevel, Func<bool> isMuted)
            {
                this.capture = capture;
                this.clock = clock;
                this.setLevel = setLevel;
                this.isMuted = isMuted;
                writer = new WaveFileWriter(path, capture.WaveFormat);
                writer.Flush();
                DataOffset = new FileInfo(path).Length;
                capture.DataAvailable += OnDataAvailable;
                capture.RecordingStopped += OnRecordingStopped;
            }

            private void WriteSilence(long frames)
            {
                int blockAlign = Format.BlockAlign;
                while (frames > 0)
                {
                    int count = (int)Math.Min(frames, silence.Length / blockAlign);
                    writer.Write(silence, 0, count * blockAlign);
                    framesWritten += count;
                    SilenceFrames += count;
                    frames -= count;
                }
            }

            private void OnDataAvailable(object sender, WaveInEventArgs args)
            {
                lock (gate)
                {
                    if (error != null || args.BytesRecorded == 0 || !clock.IsRunning) return;
                    try
                    {
                        long now = clock.ElapsedTicks;
                        int packetFrames = args.BytesRecorded / Format.BlockAlign;
                        if (FirstBufferMilliseconds < 0)
                            FirstBufferMilliseconds = (long)(now * 1000.0 / Stopwatch.Frequency);
                        // Callback time is approximately the end of this packet.
                        long endFrame = (long)(now * (double)Format.SampleRate / Stopwatch.Frequency);
                        WriteSilence(Math.Max(0, endFrame - packetFrames - framesWritten));
                        bool mutePacket = isMuted();
                        if (mutePacket) WriteSilence(packetFrames);
                        else
                        {
                            writer.Write(args.Buffer, 0, args.BytesRecorded);
                            framesWritten += packetFrames;
                        }
                        BuffersReceived++;
                        setLevel(mutePacket ? 0 : GetSoundLevel(args.Buffer, args.BytesRecorded, Format.BitsPerSample));
                    }
                    catch (Exception ex) { error = ex; }
                }
            }

            private void OnRecordingStopped(object sender, StoppedEventArgs args)
            {
                if (args.Exception != null) error = args.Exception;
                stopped.Set();
            }

            public void Start() => capture.StartRecording();

            public long FlushForTranscript(long elapsedTicks)
            {
                lock (gate)
                {
                    long safeTicks = Math.Max(0, elapsedTicks - Stopwatch.Frequency / 2);
                    long safeFrame = (long)(safeTicks * (double)Format.SampleRate / Stopwatch.Frequency);
                    WriteSilence(Math.Max(0, safeFrame - framesWritten));
                    writer.Flush();
                    return framesWritten;
                }
            }

            public void Stop(long finalElapsedTicks)
            {
                capture.StopRecording();
                if (!stopped.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("Audio capture did not stop within 10 seconds.");
                lock (gate)
                {
                    if (error != null) throw new IOException("Audio capture failed.", error);
                    long finalFrame = (long)(finalElapsedTicks * (double)Format.SampleRate / Stopwatch.Frequency);
                    WriteSilence(Math.Max(0, finalFrame - framesWritten));
                }
            }

            public void Dispose()
            {
                capture.DataAvailable -= OnDataAvailable;
                capture.RecordingStopped -= OnRecordingStopped;
                capture.Dispose();
                writer.Dispose();
                stopped.Dispose();
            }
        }


        private static double GetSoundLevel(byte[] buffer, int bytesRecorded, int bitsPerSample)
        {
            double level = 0.0;
            int bytesPerSample = bitsPerSample / 8;
            double maxLevel = Math.Pow(2, bitsPerSample - 1);
            switch (bitsPerSample)
            {
                case 8:
                    for (int i = 0; i < bytesRecorded / bytesPerSample; ++i)
                        level = Math.Max(level, Math.Abs((double)buffer[i] / maxLevel));
                    break;
                case 16:
                    for (int i = 0; i < bytesRecorded / bytesPerSample; ++i)
                        level = Math.Max(level, Math.Abs((double)BitConverter.ToInt16(buffer, i * bytesPerSample)) / maxLevel);
                    break;
                case 32:
                    for (int i = 0; i < bytesRecorded / bytesPerSample; ++i)
                        level = Math.Max(level, Math.Abs((double)BitConverter.ToInt32(buffer, i * bytesPerSample)) / maxLevel);
                    break;
            }
            return level;
        }

        public void StartRecording(string outputFilePath, int inputIndex, int loopbackIndex, bool encodeToMP3)
        {
            if (IsRecording) throw new InvalidOperationException("Already recording.");
            _outputFilePath = outputFilePath;
            _startTime = DateTime.Now;
            _encodeToMP3 = encodeToMP3;
            outputOnly = false;
            captureComplete = false;
            muted = false;
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            var input = new WasapiCapture(InputDevices[inputIndex]) { WaveFormat = _waveFormat };
            var loopback = new WasapiLoopbackCapture(LoopbackDevices[loopbackIndex]) { WaveFormat = _waveFormat };
            _clock = new Stopwatch();
            bool inputStarted = false;
            try
            {
                _inputCapture = new CaptureFile(input, outputFilePath + ".input.wav", _clock,
                    level => InputLevel = level, () => muted);
                _loopbackCapture = new CaptureFile(loopback, outputFilePath + ".loopback.wav", _clock,
                    level => LoopbackLevel = level, () => muted);
                inputDataOffset = _inputCapture.DataOffset;
                loopbackDataOffset = _loopbackCapture.DataOffset;
                _clock.Start();
                _inputCapture.Start();
                inputStarted = true;
                _loopbackCapture.Start();
                IsRecording = true;
            }
            catch
            {
                _clock.Stop();
                if (inputStarted)
                {
                    try { _inputCapture.Stop(_clock.ElapsedTicks); }
                    catch { /* Preserve the original startup failure. */ }
                }
                if (_inputCapture != null) { _inputCapture.Dispose(); _inputCapture = null; } else input.Dispose();
                if (_loopbackCapture != null) { _loopbackCapture.Dispose(); _loopbackCapture = null; } else loopback.Dispose();
                throw;
            }
        }

        public void StartLoopbackRecording(string outputFilePath, MMDevice device, bool encodeToMP3)
        {
            if (IsRecording) throw new InvalidOperationException("Already recording.");
            _outputFilePath = outputFilePath;
            _startTime = DateTime.Now;
            _encodeToMP3 = encodeToMP3;
            outputOnly = true;
            captureComplete = false;
            muted = false;
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            var loopback = new WasapiLoopbackCapture(device) { WaveFormat = _waveFormat };
            _clock = new Stopwatch();
            try
            {
                _loopbackCapture = new CaptureFile(loopback, outputFilePath + ".loopback.wav", _clock,
                    level => LoopbackLevel = level, () => muted);
                loopbackDataOffset = _loopbackCapture.DataOffset;
                _clock.Start();
                _loopbackCapture.Start();
                IsRecording = true;
            }
            catch
            {
                _clock.Stop();
                if (_loopbackCapture != null) { _loopbackCapture.Dispose(); _loopbackCapture = null; }
                else loopback.Dispose();
                throw;
            }
        }

        public void PauseCapture() { if (IsRecording) _clock.Stop(); }
        public void ResumeCapture() { if (IsRecording) _clock.Start(); }

        public void StopCapture()
        {
            if (!IsRecording) return;
            IsRecording = false;
            _clock.Stop();
            long endTicks = _clock.ElapsedTicks;
            Exception failure = null;
            try { _loopbackCapture.Stop(endTicks); } catch (Exception ex) { failure = ex; }
            if (_inputCapture != null)
                try { _inputCapture.Stop(endTicks); } catch (Exception ex) { if (failure == null) failure = ex; }
            try { if (DiagnosticLogging) WriteCaptureLog(endTicks, failure); }
            finally
            {
                lock (transcriptGate)
                {
                    _loopbackCapture.Dispose();
                    if (_inputCapture != null) _inputCapture.Dispose();
                    _loopbackCapture = null;
                    _inputCapture = null;
                    captureComplete = true;
                }
            }
            if (failure != null) throw new IOException("Recording failed; source WAV files were kept.", failure);
        }

        public void FinishRecording(IProgress<double> progress)
        {
            if (outputOnly)
                ConvertLoopbackFile(_outputFilePath + ".loopback.wav",
                    _outputFilePath + (_encodeToMP3 ? ".mp3" : ".wav"), progress);
            else
                MixAudioFiles(_outputFilePath + ".input.wav", _outputFilePath + ".loopback.wav",
                    _outputFilePath + (_encodeToMP3 ? ".mp3" : ".wav"), progress);
        }

        public void FeedLiveTranscript(Stream destination, CancellationToken cancellation)
        {
            if (outputOnly) throw new InvalidOperationException("A live transcript requires both recording sources.");
            string inputPath = _outputFilePath + ".input.wav";
            string loopbackPath = _outputFilePath + ".loopback.wav";
            using (var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var loopback = new FileStream(loopbackPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long nextSample = 0;
                while (!cancellation.IsCancellationRequested)
                {
                    long availableFrames;
                    bool done;
                    lock (transcriptGate)
                    {
                        done = captureComplete;
                        if (done)
                            availableFrames = Math.Min((input.Length - inputDataOffset) / 4,
                                (loopback.Length - loopbackDataOffset) / 4);
                        else
                        {
                            long ticks = _clock.ElapsedTicks;
                            availableFrames = Math.Min(_inputCapture.FlushForTranscript(ticks),
                                _loopbackCapture.FlushForTranscript(ticks));
                        }
                    }
                    long availableSamples = Math.Max(0, (availableFrames - 1) * 16000 / 44100);
                    if (nextSample < availableSamples)
                    {
                        int count = (int)Math.Min(1600, availableSamples - nextSample);
                        WriteTranscriptBlock(input, loopback, destination, nextSample, count,
                            inputDataOffset, loopbackDataOffset);
                        nextSample += count;
                    }
                    else if (done) break;
                    else Thread.Sleep(40);
                }
            }
        }

        private static void WriteTranscriptBlock(FileStream input, FileStream loopback, Stream destination,
            long firstSample, int sampleCount, long inputOffset, long loopbackOffset)
        {
            long firstFrame = firstSample * 44100 / 16000;
            long lastFrame = (firstSample + sampleCount - 1) * 44100 / 16000 + 1;
            int bytes = checked((int)(lastFrame - firstFrame + 1) * 4);
            byte[] microphone = new byte[bytes];
            byte[] computer = new byte[bytes];
            ReadExact(input, inputOffset + firstFrame * 4, microphone);
            ReadExact(loopback, loopbackOffset + firstFrame * 4, computer);
            byte[] mono = new byte[sampleCount * 2];
            for (int i = 0; i < sampleCount; i++)
            {
                double sourceFrame = (firstSample + i) * 44100.0 / 16000;
                long lower = (long)sourceFrame;
                double fraction = sourceFrame - lower;
                int offset = checked((int)(lower - firstFrame) * 4);
                double first = (StereoAverage(microphone, offset) + StereoAverage(computer, offset)) / 2.0;
                double second = (StereoAverage(microphone, offset + 4) + StereoAverage(computer, offset + 4)) / 2.0;
                int value = (int)Math.Round(first + (second - first) * fraction);
                value = Math.Max(short.MinValue, Math.Min(short.MaxValue, value));
                mono[i * 2] = (byte)value;
                mono[i * 2 + 1] = (byte)(value >> 8);
            }
            destination.Write(mono, 0, mono.Length);
        }

        private static int StereoAverage(byte[] bytes, int offset)
        {
            int left = (short)(bytes[offset] | bytes[offset + 1] << 8);
            int right = (short)(bytes[offset + 2] | bytes[offset + 3] << 8);
            return (left + right) / 2;
        }

        private static void ReadExact(FileStream stream, long position, byte[] buffer)
        {
            stream.Position = position;
            int count = 0;
            while (count < buffer.Length)
            {
                int read = stream.Read(buffer, count, buffer.Length - count);
                if (read == 0) throw new EndOfStreamException("Capture file ended while feeding the transcript.");
                count += read;
            }
        }

        private void WriteCaptureLog(long elapsedTicks, Exception failure)
        {
            File.WriteAllLines(_outputFilePath + ".capture.txt", new[] {
                "StartedLocal=" + _startTime.ToString("O"),
                "ElapsedMilliseconds=" + (elapsedTicks * 1000.0 / Stopwatch.Frequency).ToString("F0"),
                "InputFirstBufferMilliseconds=" + (_inputCapture == null ? -1 : _inputCapture.FirstBufferMilliseconds),
                "InputBuffers=" + (_inputCapture == null ? 0 : _inputCapture.BuffersReceived),
                "InputSilenceFrames=" + (_inputCapture == null ? 0 : _inputCapture.SilenceFrames),
                "LoopbackFirstBufferMilliseconds=" + _loopbackCapture.FirstBufferMilliseconds,
                "LoopbackBuffers=" + _loopbackCapture.BuffersReceived,
                "LoopbackSilenceFrames=" + _loopbackCapture.SilenceFrames,
                "Error=" + (failure == null ? "none" : failure.ToString())
            });
        }

        private static void MixBuffered(Stream output, IWaveProvider input, IWaveProvider loopback,
            WaveFormat format, long totalFrames, IProgress<double> progress)
        {
            if (format.BitsPerSample != 16 || format.Channels != 2)
                throw new NotSupportedException("The mixer requires 16-bit stereo PCM.");

            const int framesPerBlock = 4096;
            byte[] inputBytes = new byte[framesPerBlock * 4];
            byte[] loopbackBytes = new byte[framesPerBlock * 4];
            byte[] result = new byte[framesPerBlock * 4];
            long framesProcessed = 0;
            int lastPercent = -1;
            progress?.Report(0);
            while (true)
            {
                Array.Clear(inputBytes, 0, inputBytes.Length);
                Array.Clear(loopbackBytes, 0, loopbackBytes.Length);
                int inputCount = ReadBlock(input, inputBytes);
                int loopbackCount = ReadBlock(loopback, loopbackBytes);
                if (inputCount == 0 && loopbackCount == 0) break;
                if (inputCount % 4 != 0 || loopbackCount % 4 != 0)
                    throw new InvalidDataException("A source returned an incomplete stereo sample frame.");

                int frames = Math.Max(inputCount, loopbackCount) / 4;
                for (int frame = 0; frame < frames; frame++)
                {
                    int offset = frame * 4;
                    int microphone = ((short)(inputBytes[offset] | inputBytes[offset + 1] << 8) +
                                      (short)(inputBytes[offset + 2] | inputBytes[offset + 3] << 8)) / 2;
                    int computer = ((short)(loopbackBytes[offset] | loopbackBytes[offset + 1] << 8) +
                                    (short)(loopbackBytes[offset + 2] | loopbackBytes[offset + 3] << 8)) / 2;
                    result[offset] = (byte)microphone;
                    result[offset + 1] = (byte)(microphone >> 8);
                    result[offset + 2] = (byte)computer;
                    result[offset + 3] = (byte)(computer >> 8);
                }
                output.Write(result, 0, frames * 4);
                framesProcessed += frames;
                int percent = totalFrames == 0 ? 100 : (int)Math.Min(99, framesProcessed * 100 / totalFrames);
                if (percent != lastPercent)
                {
                    progress?.Report(percent);
                    lastPercent = percent;
                }
            }
            progress?.Report(100);
        }

        private static int ReadBlock(IWaveProvider source, byte[] buffer)
        {
            int count = 0;
            while (count < buffer.Length)
            {
                int read = source.Read(buffer, count, buffer.Length - count);
                if (read == 0) break;
                count += read;
            }
            return count;
        }

        private static void ConvertLoopbackFile(string sourcePath, string outputPath, IProgress<double> progress)
        {
            progress?.Report(0);
            if (outputPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourcePath, outputPath, false);
                progress?.Report(100);
                return;
            }
            using (var reader = new WaveFileReader(sourcePath))
            using (var writer = new LameMP3FileWriter(outputPath, reader.WaveFormat, LAMEPreset.VBR_90))
            {
                byte[] buffer = new byte[32768];
                int previousPercent = -1;
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, count);
                    int percent = (int)Math.Min(99, reader.Position * 100 / Math.Max(1, reader.Length));
                    if (percent != previousPercent) { progress?.Report(percent); previousPercent = percent; }
                }
            }
            progress?.Report(100);
        }

        private void MixAudioFiles(string inputFileName, string loopbackFileName, string outputFileName,
            IProgress<double> progress)
        {
            try
            {
                // Open input and loopback WAV files
                using (var inputReader = new WaveFileReader(inputFileName))
                using (var loopbackReader = new WaveFileReader(loopbackFileName))
                {
                    long totalFrames = Math.Max(inputReader.Length / inputReader.WaveFormat.BlockAlign,
                        loopbackReader.Length / loopbackReader.WaveFormat.BlockAlign);
                    int sampleRate = 44100;
                    int bitsPerSample = inputReader.WaveFormat.BitsPerSample;
                    int channels = inputReader.WaveFormat.Channels;

                    // Ensure both files have the same bit depth and number of channels, resample if needed
                    if (bitsPerSample != loopbackReader.WaveFormat.BitsPerSample || channels != loopbackReader.WaveFormat.Channels)
                    {
                        MessageBox.Show("Input and loopback files must have the same bit depth and number of channels.");
                        return;
                    }

                    // Create resampler for input and loopback files if necessary
                    using (var inputResampler = new MediaFoundationResampler(inputReader, new WaveFormat(sampleRate, bitsPerSample, channels)))
                    using (var loopbackResampler = new MediaFoundationResampler(loopbackReader, new WaveFormat(sampleRate, bitsPerSample, channels)))
                    {
                        inputResampler.ResamplerQuality = 60;
                        loopbackResampler.ResamplerQuality = 60;

                        if (outputFileName.EndsWith(".mp3"))
                        {
                            WaveFormat format = new WaveFormat(sampleRate, bitsPerSample, channels);
                            using (var writer = new LameMP3FileWriter(outputFileName, format, LAMEPreset.VBR_90))
                                MixBuffered(writer, inputResampler, loopbackResampler, format, totalFrames, progress);
                        }
                        else
                        {
                            // Create mixed WAV file
                            WaveFormat format = new WaveFormat(sampleRate, bitsPerSample, channels);
                            using (var writer = new WaveFileWriter(outputFileName, format))
                                MixBuffered(writer, inputResampler, loopbackResampler, format, totalFrames, progress);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IOException("Mixing failed; source WAV files were kept.", ex);
            }
        }
    }
}
