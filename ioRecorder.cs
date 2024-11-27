using NAudio.CoreAudioApi;
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Remoting.Channels;

namespace ioRecord
{
    public class ioRecorder
    {
        public bool IsRecording { get; private set; }
        public List<MMDevice> InputDevices { get; private set; }
        public List<MMDevice> LoopbackDevices { get; private set; }
        public DateTime _startTime { get; private set; }
        public double InputLevel { get; private set; }

        private Thread _inputRecordingThread;
        private Thread _loopbackRecordingThread;
        private WasapiLoopbackCapture _loopbackWaveProvider;
        public double LoopbackLevel { get; private set; }
        private int _inputIndex = 0;
        private WaveFormat _waveFormat;
        private string _outputFilePath;
        private bool _encodeToMP3 = false;
        public TimeSpan ElapsedTime => DateTime.Now - _startTime;

        public ioRecorder()
        {
            IsRecording = false;
            _inputRecordingThread = null;
            _loopbackRecordingThread = null;
            _waveFormat = new WaveFormat(44100, 16, 2);
        }

        public void LoadDevices()
        {
            InputDevices = GetAudioDevices(DataFlow.Capture);
            LoopbackDevices = GetAudioDevices(DataFlow.Render);
        }

        private static List<MMDevice> GetAudioDevices(DataFlow dataFlow)
        {
            var devices = new List<MMDevice>();
            var enumerator = new MMDeviceEnumerator();
            devices.Add(enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Communications));

            foreach (var device in enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active))
            {
                if (device != devices[0])
                    devices.Add(device);
            }
            return devices;
        }
        private void InputRecordingThread()
        {
            WaveInEvent inputWaveProvider = new WaveInEvent {
                DeviceNumber = _inputIndex,
                WaveFormat = _waveFormat,
                BufferMilliseconds = 50
            };

            using (WaveFileWriter waveFileWriter = new WaveFileWriter(_outputFilePath + ".input.wav", _waveFormat)) {
                EventHandler<WaveInEventArgs> recordHandler = (object s, WaveInEventArgs args) => {
                    waveFileWriter.Write(args.Buffer, 0, args.BytesRecorded);
                    InputLevel = GetSoundLevel(args.Buffer, args.BytesRecorded, waveFileWriter.WaveFormat.BitsPerSample);
                };
                inputWaveProvider.DataAvailable += recordHandler;

                inputWaveProvider.StartRecording();

                while (IsRecording)
                {
                    Thread.Sleep(100);
                }

                inputWaveProvider.StopRecording();
                inputWaveProvider.DataAvailable -= recordHandler;
                inputWaveProvider.Dispose();
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

        internal void LoopbackRecordingThread()
        {
            using (var waveFileWriter = new WaveFileWriter(_outputFilePath + ".loopback.wav", _waveFormat)) {
                EventHandler<WaveInEventArgs> recordHandler = (object s, WaveInEventArgs args) => {
                    waveFileWriter.Write(args.Buffer, 0, args.BytesRecorded);
                    LoopbackLevel = GetSoundLevel(args.Buffer, args.BytesRecorded, waveFileWriter.WaveFormat.BitsPerSample);
                };

                _loopbackWaveProvider.DataAvailable += recordHandler;
                _loopbackWaveProvider.StartRecording();

                while (IsRecording)
                {
                    Thread.Sleep(100);
                }

                _loopbackWaveProvider.StopRecording();
                _loopbackWaveProvider.DataAvailable -= recordHandler;
                _loopbackWaveProvider.Dispose();
            }
        }
        public void StartRecording(string outputFilePath, int inputIndex, int loopbackIndex, bool encodeToMP3)
        {
            _outputFilePath = outputFilePath;
            _inputIndex = inputIndex;
            _startTime = DateTime.Now;
            _encodeToMP3 = encodeToMP3;

            MMDevice loopbackDevice = LoopbackDevices[loopbackIndex];
            _loopbackWaveProvider = new WasapiLoopbackCapture(loopbackDevice) {
                WaveFormat = _waveFormat
            };

            // Start recording
            IsRecording = true;

            // Start input device recording thread
            _inputRecordingThread = new Thread(InputRecordingThread);
            _inputRecordingThread.Start();

            // Start loopback device recording thread
            _loopbackRecordingThread = new Thread(LoopbackRecordingThread);
            _loopbackRecordingThread.Start();
        }

        public void StopRecording()
        {
            // Stop recording
            IsRecording = false;
            _loopbackRecordingThread.Join();
            _inputRecordingThread.Join();

            // Mix input and loopback recordings and optionally encode to MP3
            MixAudioFiles(
                _outputFilePath + ".input.wav", 
                _outputFilePath + ".loopback.wav", 
                _outputFilePath + (_encodeToMP3 ? ".mp3" : ".wav"));
        }
        
        private static void MixToMP3(string outputFileName, WaveFormat format, IWaveProvider inputResampler, IWaveProvider loopbackResampler)
        {
            int bitsPerSample = format.BitsPerSample;
            int bytesPerSample = bitsPerSample / 8;
            int channels = format.Channels;

            // Create MP3 file
            using (var mixedWriter = new LameMP3FileWriter(outputFileName, format, LAMEPreset.VBR_90))
            {
                // Read and mix audio data sample by sample
                byte[] inputBuffer = new byte[channels * bytesPerSample];
                byte[] loopbackBuffer = new byte[channels * bytesPerSample];
                while (inputResampler.Read(inputBuffer, 0, inputBuffer.Length) > 0 &&
                       loopbackResampler.Read(loopbackBuffer, 0, loopbackBuffer.Length) > 0)
                {
                    double inputSample = 0;
                    double loopbackSample = 0;
                    for (int i = 0; i < channels; ++i)
                    {
                        switch (bitsPerSample)
                        {
                            case 8:
                                inputSample += inputBuffer[i * bytesPerSample];
                                loopbackSample += loopbackBuffer[i * bytesPerSample];
                                break;
                            case 16:
                                inputSample += BitConverter.ToInt16(inputBuffer, i * bytesPerSample);
                                loopbackSample += BitConverter.ToInt16(loopbackBuffer, i * bytesPerSample);
                                break;
                            case 32:
                                inputSample += BitConverter.ToInt32(inputBuffer, i * bytesPerSample);
                                loopbackSample += BitConverter.ToInt32(loopbackBuffer, i * bytesPerSample);
                                break;
                        }
                    }

                    inputSample /= channels;
                    loopbackSample /= channels;

                    // Convert mixed samples back to bytes
                    byte[] mixedBuffer = new byte[2 * bytesPerSample];
                    switch (bitsPerSample)
                    {
                        case 8:
                            Buffer.BlockCopy(BitConverter.GetBytes((byte)inputSample), 0, mixedBuffer, 0, bytesPerSample);
                            Buffer.BlockCopy(BitConverter.GetBytes((byte)loopbackSample), 0, mixedBuffer, bytesPerSample, bytesPerSample);
                            break;
                        case 16:
                            Buffer.BlockCopy(BitConverter.GetBytes((short)inputSample), 0, mixedBuffer, 0, bytesPerSample);
                            Buffer.BlockCopy(BitConverter.GetBytes((short)loopbackSample), 0, mixedBuffer, bytesPerSample, bytesPerSample);
                            break;
                        case 32:
                            Buffer.BlockCopy(BitConverter.GetBytes((int)inputSample), 0, mixedBuffer, 0, bytesPerSample);
                            Buffer.BlockCopy(BitConverter.GetBytes((int)loopbackSample), 0, mixedBuffer, bytesPerSample, bytesPerSample);
                            break;
                    }

                    // Write mixed samples to output file
                    mixedWriter.Write(mixedBuffer, 0, mixedBuffer.Length);
                }
            }
        }

        private static void MixToWav(string outputFileName, WaveFormat format, IWaveProvider inputResampler, IWaveProvider loopbackResampler)
        {
            int bitsPerSample = format.BitsPerSample;
            int bytesPerSample = bitsPerSample / 8;
            int channels = format.Channels;

            WaveFormat outputFormat = new WaveFormat(format.SampleRate, bitsPerSample, channels);
            using (var mixedWriter = new WaveFileWriter(outputFileName, outputFormat))
            {
                // Read and mix audio data sample by sample
                byte[] inputBuffer = new byte[channels * bytesPerSample];
                byte[] loopbackBuffer = new byte[channels * bytesPerSample];
                while (inputResampler.Read(inputBuffer, 0, inputBuffer.Length) > 0 &&
                       loopbackResampler.Read(loopbackBuffer, 0, loopbackBuffer.Length) > 0)
                {
                    double inputSample = 0;
                    double loopbackSample = 0;
                    for (int i = 0; i < channels; ++i)
                    {
                        switch (bitsPerSample)
                        {
                            case 8:
                                inputSample += inputBuffer[i * bytesPerSample];
                                loopbackSample += loopbackBuffer[i * bytesPerSample];
                                break;
                            case 16:
                                inputSample += BitConverter.ToInt16(inputBuffer, i * bytesPerSample);
                                loopbackSample += BitConverter.ToInt16(loopbackBuffer, i * bytesPerSample);
                                break;
                            case 32:
                                inputSample += BitConverter.ToInt32(inputBuffer, i * bytesPerSample);
                                loopbackSample += BitConverter.ToInt32(loopbackBuffer, i * bytesPerSample);
                                break;
                        }
                    }

                    inputSample /= channels;
                    loopbackSample /= channels;

                    // Convert mixed samples back to bytes
                    byte[] mixedBuffer = new byte[2 * bytesPerSample];
                    switch (bitsPerSample)
                    {
                        case 8:
                            Buffer.BlockCopy(BitConverter.GetBytes((byte)inputSample), 0, mixedBuffer, 0, bytesPerSample);
                            Buffer.BlockCopy(BitConverter.GetBytes((byte)loopbackSample), 0, mixedBuffer, bytesPerSample, bytesPerSample);
                            break;
                        case 16:
                            Buffer.BlockCopy(BitConverter.GetBytes((short)inputSample), 0, mixedBuffer, 0, bytesPerSample);
                            Buffer.BlockCopy(BitConverter.GetBytes((short)loopbackSample), 0, mixedBuffer, bytesPerSample, bytesPerSample);
                            break;
                        case 32:
                            Buffer.BlockCopy(BitConverter.GetBytes((int)inputSample), 0, mixedBuffer, 0, bytesPerSample);
                            Buffer.BlockCopy(BitConverter.GetBytes((int)loopbackSample), 0, mixedBuffer, bytesPerSample, bytesPerSample);
                            break;
                    }

                    // Write mixed samples to output file
                    mixedWriter.Write(mixedBuffer, 0, mixedBuffer.Length);
                }
            }
        }

        private void MixAudioFiles(string inputFileName, string loopbackFileName, string outputFileName)
        {
            try
            {
                // Open input and loopback WAV files
                using (var inputReader = new WaveFileReader(inputFileName))
                using (var loopbackReader = new WaveFileReader(loopbackFileName))
                {
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
                            MixToMP3(outputFileName, format, inputResampler, loopbackResampler);
                        }
                        else
                        {
                            // Create mixed WAV file
                            WaveFormat format = new WaveFormat(sampleRate, bitsPerSample, channels);
                            MixToWav(outputFileName, format, inputResampler, loopbackResampler);
                        }
                    }
                }
                File.Delete(inputFileName);
                File.Delete(loopbackFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error mixing audio files: {ex.Message}");
            }
        }
    }
}
