using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace ioRecord
{
    // Sends a saved file to verbastream at the pace of the audible playback.
    internal sealed class TranscriptPlayback : IDisposable
    {
        private readonly Process process;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly Func<TimeSpan> playbackPosition;
        private readonly Action<string> onSnapshot;
        private readonly Action<string> onStatus;
        private readonly JavaScriptSerializer eventJson = new JavaScriptSerializer();
        private readonly object logGate = new object();
        private readonly StreamWriter responseLog;
        private readonly StreamWriter errorLog;
        private bool logsClosed;
        // -1 means playback is still supplying audio; a nonnegative value is
        // the final number of PCM bytes to send before closing stdin.
        private long completionBytes = -1;
        public string ResponseLogPath { get; }
        public string ErrorLogPath { get; }

        public TranscriptPlayback(string executable, string audioPath, string logPrefix, Func<TimeSpan> playbackPosition,
            string[] languages, bool quality, string backend, string model, bool diagnosticLogging,
            Action<string> onSnapshot, Action<string> onStatus)
            : this(executable, logPrefix, languages, quality, backend, model, diagnosticLogging, onSnapshot, onStatus)
        {
            this.playbackPosition = playbackPosition;
            Task.Run(() => Feed(audioPath));
        }

        public TranscriptPlayback(string executable, string logPrefix, string[] languages, bool quality, string backend, string model,
            Action<Stream, CancellationToken> feedLive, bool diagnosticLogging,
            Action<string> onSnapshot, Action<string> onStatus)
            : this(executable, logPrefix, languages, quality, backend, model, diagnosticLogging, onSnapshot, onStatus)
        {
            Task.Run(() => FeedLive(feedLive));
        }

        private TranscriptPlayback(string executable, string logPrefix, string[] languages, bool quality, string backend, string model,
            bool diagnosticLogging, Action<string> onSnapshot, Action<string> onStatus)
        {
            string arguments = BuildArguments(languages, quality, backend, model);
            this.onSnapshot = onSnapshot;
            this.onStatus = onStatus;
            if (diagnosticLogging)
            {
                ResponseLogPath = logPrefix + ".verbastream.stdout.log";
                ErrorLogPath = logPrefix + ".verbastream.stderr.log";
                responseLog = new StreamWriter(ResponseLogPath, false) { AutoFlush = true };
                try { errorLog = new StreamWriter(ErrorLogPath, false) { AutoFlush = true }; }
                catch { responseLog.Dispose(); throw; }
                string commandLine = "\"" + executable + "\" " + arguments;
                responseLog.WriteLine("Command: " + commandLine);
                errorLog.WriteLine("Command: " + commandLine);
            }
            process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = executable,
                    Arguments = arguments,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            process.StartInfo.EnvironmentVariables["CUDA_LAUNCH_BLOCKING"] = "1";
            process.OutputDataReceived += (s, e) => {
                if (e.Data == null) return;
                AppendLog(responseLog, e.Data);
                if (e.Data.StartsWith("data: ")) HandleEvent(e.Data.Substring(6));
            };
            process.ErrorDataReceived += (s, e) => {
                if (e.Data == null) return;
                AppendLog(errorLog, e.Data);
            };
            try
            {
                if (!process.Start()) throw new IOException("verbastream did not start.");
            }
            catch
            {
                CloseLogs();
                process.Dispose();
                throw;
            }
            onStatus("Starting transcription…" + LogDetails(" Logs: "));
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private void HandleEvent(string json)
        {
            try
            {
                var data = eventJson.DeserializeObject(json) as Dictionary<string, object>;
                if (data == null) throw new FormatException("Expected a JSON object.");
                object value;
                string type = data.TryGetValue("type", out value) ? Convert.ToString(value) : "snapshot";
                if (type == "snapshot") onSnapshot(json);
                else if (type == "status")
                {
                    string stage = data.TryGetValue("stage", out value) ? Convert.ToString(value) : "";
                    onStatus(StatusText(stage, data));
                }
                else if (type == "error")
                    onStatus("Transcription error: " + (data.TryGetValue("message", out value) ? Convert.ToString(value) : "Unknown error"));
            }
            catch (Exception ex)
            {
                onStatus("Could not read transcriber event: " + ex.Message);
            }
        }

        private static string StatusText(string stage, Dictionary<string, object> data)
        {
            object value;
            switch (stage)
            {
                case "initializing": return "Initializing transcription…";
                case "loading_diarizer": return "Loading speaker recognition model…";
                case "loading_transcriber":
                    return "Loading transcription model" +
                        (data.TryGetValue("model", out value) && value != null ? " (" + value + ")" : "") + "…";
                case "ready": return "Transcription models ready.";
                case "waiting_for_audio": return "Waiting for audio…";
                case "transcribing": return "Transcribing audio…";
                case "finalizing": return "Finalizing transcript…";
                case "complete": return "Transcript finalized.";
                default: return "Transcription: " + stage.Replace('_', ' ') + "…";
            }
        }

        private static string BuildArguments(string[] languages, bool quality, string backend, string model)
        {
            if (languages == null || languages.Length == 0)
                throw new ArgumentException("Select at least one transcript language.", nameof(languages));
            if (backend != "whisper" && backend != "qwen" && backend != "parakeet")
                throw new ArgumentException("Unsupported transcript model: " + backend, nameof(backend));
            string arguments = (quality ? "--mode quality" : "--mode low-latency") + " --backend " + backend;
            if (backend == "parakeet")
            {
                if (model != "nvidia/parakeet-tdt-0.6b-v2" &&
                    model != "Archime/parakeet-tdt-0.6b-v3-fr-tv-media" &&
                    model != "nvidia/parakeet-tdt-0.6b-v3")
                    throw new ArgumentException("Unsupported Parakeet model: " + model, nameof(model));
                arguments += " --model " + model;
            }
            foreach (string language in languages)
            {
                if (language != "en" && language != "fr")
                    throw new ArgumentException("Unsupported transcript language: " + language);
                arguments += " --language " + language;
            }
            return arguments;
        }

        public void PlaybackEnded() { Interlocked.Exchange(ref completionBytes, long.MaxValue); }

        public void CompleteAt(TimeSpan position)
        {
            long bytes = Math.Max(0, (long)(position.TotalSeconds * 32000)) & ~1L;
            Interlocked.CompareExchange(ref completionBytes, bytes, -1);
        }

        private void Feed(string audioPath)
        {
            try
            {
                using (var reader = new MediaFoundationReader(audioPath))
                using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(16000, 16, 1)))
                {
                    resampler.ResamplerQuality = 60;
                    byte[] buffer = new byte[3200]; // 100 ms of PCM16 mono.
                    long bytesSent = 0;
                    while (!cancellation.IsCancellationRequested)
                    {
                        long finalBytes = Interlocked.Read(ref completionBytes);
                        long allowed = finalBytes >= 0 ? finalBytes :
                            (long)(playbackPosition().TotalSeconds * 32000);
                        long available = allowed - bytesSent;
                        if (finalBytes >= 0 && available <= 0) break;
                        if (available < buffer.Length && finalBytes < 0)
                        {
                            Thread.Sleep(20);
                            continue;
                        }
                        int requested = finalBytes >= 0 ? (int)Math.Min(buffer.Length, available) : buffer.Length;
                        requested &= ~1;
                        if (requested == 0) break;
                        int read = resampler.Read(buffer, 0, requested);
                        if (read == 0) break;
                        process.StandardInput.BaseStream.Write(buffer, 0, read);
                        bytesSent += read;
                    }
                }
                if (cancellation.IsCancellationRequested) return;
                process.StandardInput.Close(); // EOF releases the final partial block.
                process.WaitForExit();
                onStatus(process.ExitCode == 0 ? "Transcription complete." + LogDetails(" Log: ") :
                    "Transcription exited with code " + process.ExitCode + "." + LogDetails(" Logs: "));
            }
            catch (Exception ex)
            {
                if (!cancellation.IsCancellationRequested) onStatus("Transcription failed: " + ex.Message + "." + LogDetails(" Logs: "));
            }
            finally { CloseLogs(); }
        }

        private void FeedLive(Action<Stream, CancellationToken> feed)
        {
            try
            {
                feed(process.StandardInput.BaseStream, cancellation.Token);
                if (cancellation.IsCancellationRequested) return;
                process.StandardInput.Close();
                process.WaitForExit();
                onStatus(process.ExitCode == 0 ? "Transcription complete." + LogDetails(" Log: ") :
                    "Transcription exited with code " + process.ExitCode + "." + LogDetails(" Logs: "));
            }
            catch (Exception ex)
            {
                if (!cancellation.IsCancellationRequested)
                    onStatus("Transcription failed: " + ex.Message + "." + LogDetails(" Logs: "));
            }
            finally { CloseLogs(); }
        }

        private void CloseLogs()
        {
            lock (logGate)
            {
                if (logsClosed) return;
                logsClosed = true;
                responseLog?.Dispose();
                errorLog?.Dispose();
            }
        }

        private void AppendLog(StreamWriter writer, string line)
        {
            try
            {
                lock (logGate) { if (!logsClosed) writer?.WriteLine(line); }
            }
            catch (Exception ex) { onStatus("Could not save transcriber log: " + ex.Message); }
        }

        private string LogDetails(string prefix)
        {
            return ResponseLogPath == null ? "" : prefix + ResponseLogPath;
        }

        public void Dispose()
        {
            cancellation.Cancel();
            if (!process.HasExited) process.Kill();
            process.WaitForExit(2000);
            CloseLogs();
            process.Dispose();
            cancellation.Dispose();
        }
    }
}
