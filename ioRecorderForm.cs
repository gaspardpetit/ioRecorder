using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ioRecord;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading.Tasks;

namespace ioRecord
{
	public partial class ioRecorderForm : Form
	{
		private ioRecorder _ioRecorder;
		private string _audioFilePath;
		private WaveStream _audioReader;
		private WasapiOut _audioPlayer;
        private TranscriptPlayback _transcriptPlayback;
        private ioRecorder _playbackCapture;
        private volatile int playbackLeftLevel;
        private volatile int playbackRightLevel;
        private bool _playbackCaptureFinishing;
        private bool _recordingTranscript;
        private bool _recordingStopped;
        private bool _recordingAudioFinishing;
        private string _sessionOutputBase;
        private string _transcriptFilePath;
        private string _recordingCleanupBase;
        private FileStream _tempCaptureLock;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        private string[] SelectedLanguages()
        {
            var languages = new List<string>();
            if (englishLanguage.Checked) languages.Add("en");
            if (frenchLanguage.Checked) languages.Add("fr");
            if (languages.Count == 0) throw new InvalidOperationException("Select EN, FR, or both.");
            return languages.ToArray();
        }

        private string NewOutputBase(string name)
        {
            string folder = browseLocationTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(folder)) throw new InvalidOperationException("Select an output folder.");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, name);
        }

        private static void DeleteSourceFiles(string path)
        {
            if (path == null) return;
            foreach (string suffix in new[] { ".input.wav", ".loopback.wav" })
            {
                string file = path + suffix;
                if (File.Exists(file)) File.Delete(file);
            }
            string temporaryRoot = Path.Combine(Path.GetTempPath(), "ioRecord");
            string directory = Path.GetDirectoryName(path);
            if (directory != null && directory.StartsWith(temporaryRoot + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            {
                string log = path + ".capture.txt";
                if (File.Exists(log)) File.Delete(log);
                string active = Path.Combine(directory, "active.lock");
                if (File.Exists(active)) File.Delete(active);
                if (Directory.Exists(directory)) Directory.Delete(directory);
            }
        }

        private static void CleanupAbandonedTemporaryCaptures()
        {
            string root = Path.Combine(Path.GetTempPath(), "ioRecord");
            if (!Directory.Exists(root)) return;
            foreach (string directory in Directory.GetDirectories(root))
            {
                Guid id;
                if (!Guid.TryParse(Path.GetFileName(directory), out id)) continue;
                try
                {
                    string active = Path.Combine(directory, "active.lock");
                    if (File.Exists(active))
                        using (new FileStream(active, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                    DeleteSourceFiles(Path.Combine(directory, "capture"));
                }
                catch (IOException) { } // Another process may still have this capture open.
                catch (UnauthorizedAccessException) { }
            }
        }

        private void TryCleanupRecordingSources()
        {
            if (!_recordingStopped || _recordingAudioFinishing || _transcriptPlayback != null ||
                _recordingCleanupBase == null) return;
            if (_tempCaptureLock != null) { _tempCaptureLock.Dispose(); _tempCaptureLock = null; }
            DeleteSourceFiles(_recordingCleanupBase);
            _recordingCleanupBase = null;
        }

		public ioRecorderForm()
		{
			InitializeComponent();
            CleanupAbandonedTemporaryCaptures();
            InitializeSessionTabs();
            InitializeSessionOptions();
            RestoreSessionOptions();
            ApplySystemTheme();
            UpdateTranscriptLayout();
			Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemThemeChanged;
			_json.MaxJsonLength = int.MaxValue;
			_ioRecorder = new ioRecorder();
			this.elapsedText.Text = "";
			this.loopbackBar.Visible = false;
			this.inputBar.Visible = false;
			this.transcriberPath.Text = Properties.Settings.Default.VerbastreamPath;
			if (transcriberPath.Text == "verbastream")
			{
				string bundledExecutable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
					"transcriber", "verbastream.exe");
				if (File.Exists(bundledExecutable)) transcriberPath.Text = bundledExecutable;
				else
				{
					string siblingExecutable = Path.GetFullPath(Path.Combine(
						AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "verbatim_bu",
						".venv", "Scripts", "verbastream.exe"));
					if (File.Exists(siblingExecutable)) transcriberPath.Text = siblingExecutable;
				}
			}
			this.FormClosing += (s, e) => {
				SaveSessionOptions();
				string inputId = SelectedDeviceId(_ioRecorder.InputDevices, inputComboBox.SelectedIndex);
				string recordingOutputId = SelectedDeviceId(_ioRecorder.LoopbackDevices, loopbackComboBox.SelectedIndex);
				string playbackOutputId = SelectedDeviceId(_ioRecorder.LoopbackDevices, playbackOutputCombo.SelectedIndex);
				if (inputId.Length > 0) Properties.Settings.Default.InputDeviceId = inputId;
				if (recordingOutputId.Length > 0) Properties.Settings.Default.RecordingOutputDeviceId = recordingOutputId;
				if (playbackOutputId.Length > 0) Properties.Settings.Default.PlaybackOutputDeviceId = playbackOutputId;
				string bundledExecutable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
					"transcriber", "verbastream.exe");
				Properties.Settings.Default.VerbastreamPath =
					string.Equals(transcriberPath.Text.Trim(), bundledExecutable, StringComparison.OrdinalIgnoreCase)
						? "verbastream" : transcriberPath.Text.Trim();
				Properties.Settings.Default.Save();
			};
		}

		private void AudioRecorder_Load(object sender, EventArgs e)
		{
			RefreshAudioDevices();
		}

		private void RefreshAudioDevices()
		{
			string inputId = _ioRecorder.InputDevices == null ? Properties.Settings.Default.InputDeviceId :
				SelectedDeviceId(_ioRecorder.InputDevices, inputComboBox.SelectedIndex);
			string recordingOutputId = _ioRecorder.LoopbackDevices == null ? Properties.Settings.Default.RecordingOutputDeviceId :
				SelectedDeviceId(_ioRecorder.LoopbackDevices, loopbackComboBox.SelectedIndex);
			string playbackOutputId = _ioRecorder.LoopbackDevices == null ? Properties.Settings.Default.PlaybackOutputDeviceId :
				SelectedDeviceId(_ioRecorder.LoopbackDevices, playbackOutputCombo.SelectedIndex);
			if (string.IsNullOrEmpty(inputId)) inputId = Properties.Settings.Default.InputDeviceId;
			if (string.IsNullOrEmpty(recordingOutputId)) recordingOutputId = Properties.Settings.Default.RecordingOutputDeviceId;
			if (string.IsNullOrEmpty(playbackOutputId)) playbackOutputId = Properties.Settings.Default.PlaybackOutputDeviceId;
			_ioRecorder.LoadDevices();
			inputComboBox.SelectedIndex = -1;
			loopbackComboBox.SelectedIndex = -1;
			playbackOutputCombo.SelectedIndex = -1;
			inputComboBox.Items.Clear();
			loopbackComboBox.Items.Clear();
			playbackOutputCombo.Items.Clear();

			foreach (var device in _ioRecorder.InputDevices)
			{
				inputComboBox.Items.Add(device.FriendlyName);
			}

			foreach (var device in _ioRecorder.LoopbackDevices)
			{
				loopbackComboBox.Items.Add(device.FriendlyName);
				playbackOutputCombo.Items.Add(device.FriendlyName);
			}

			if (_ioRecorder.InputDevices.Count > 0)
				inputComboBox.SelectedIndex = PreferredDeviceIndex(_ioRecorder.InputDevices, inputId);

			if (_ioRecorder.LoopbackDevices.Count > 0)
			{
				loopbackComboBox.SelectedIndex = PreferredDeviceIndex(_ioRecorder.LoopbackDevices, recordingOutputId);
				playbackOutputCombo.SelectedIndex = PreferredDeviceIndex(_ioRecorder.LoopbackDevices, playbackOutputId);
			}
			deviceStatus.Text = _ioRecorder.DeviceDiscoveryError ??
				(_ioRecorder.LoopbackDevices.Count == 0 ? "No audio output device in this session." :
				 _ioRecorder.InputDevices.Count == 0 ? "No microphone in this session." : "");
			deviceStatus.Visible = deviceStatus.Text.Length > 0;
			record_button.Enabled = _ioRecorder.InputDevices.Count > 0 && _ioRecorder.LoopbackDevices.Count > 0;
			playAudioButton.Enabled = _audioFilePath != null && _ioRecorder.LoopbackDevices.Count > 0;
		}

		private static string SelectedDeviceId(List<MMDevice> devices, int index)
		{
			return devices != null && index >= 0 && index < devices.Count ? devices[index].ID : "";
		}

		private static int PreferredDeviceIndex(List<MMDevice> devices, string id)
		{
			for (int i = 0; i < devices.Count; i++)
				if (devices[i].ID == id) return i;
			return 0;
		}

		private void updateStatusTimer_Tick(object sender, EventArgs e)
		{
			TimeSpan elapsedTime = _ioRecorder.ElapsedTime;
			this.elapsedText.Text = elapsedTime.ToString(@"hh\:mm\:ss");
			this.inputBar.Value = (int)(Math.Max(0, Math.Min(100, _ioRecorder.InputLevel * 100)));
			this.loopbackBar.Value = (int)(Math.Max(0, Math.Min(100, _ioRecorder.LoopbackLevel * 100)));
		}

		private void ToggleRecordingMute()
		{
			if (!_ioRecorder.IsRecording) return;
			_ioRecorder.IsMuted = !_ioRecorder.IsMuted;
			muteButton.Text = _ioRecorder.IsMuted ? "Unmute" : "Mute";
			if (_ioRecorder.IsMuted) { inputBar.Value = 0; loopbackBar.Value = 0; }
		}

		private async void record_Click(object sender, EventArgs e)
		{
			if (!_ioRecorder.IsRecording)
			{
				if (inputComboBox.SelectedIndex < 0 || loopbackComboBox.SelectedIndex < 0)
				{
					MessageBox.Show("Recording needs an active microphone and output device. Check Settings or refresh devices.");
					return;
				}
				if (!saveAudio.Checked && !enableTranscript.Checked)
				{
					MessageBox.Show("Enable Save audio, Enable transcript, or both before recording.");
					return;
				}
				string outputFilePath;
				string captureFilePath;
				string[] languages = null;
				try
				{
					if (enableTranscript.Checked) languages = SelectedLanguages();
					outputFilePath = NewOutputBase(DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
					captureFilePath = saveAudio.Checked ? outputFilePath :
						Path.Combine(Path.GetTempPath(), "ioRecord", Guid.NewGuid().ToString("N"), "capture");
					if (!saveAudio.Checked)
					{
						Directory.CreateDirectory(Path.GetDirectoryName(captureFilePath));
						_tempCaptureLock = new FileStream(Path.Combine(Path.GetDirectoryName(captureFilePath),
							"active.lock"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
					}
				}
				catch (Exception ex) { MessageBox.Show(ex.Message); return; }
				int inputIndex = inputComboBox.SelectedIndex;
				int loopbackIndex = loopbackComboBox.SelectedIndex;
				bool encodeToMP3 = this.compressToMP3.Checked;

				try
				{
					_ioRecorder.DiagnosticLogging = diagnosticLogging.Checked;
					_ioRecorder.StartRecording(captureFilePath, inputIndex, loopbackIndex, encodeToMP3);
					_sessionOutputBase = outputFilePath;
					_recordingCleanupBase = captureFilePath;
					_recordingStopped = false;
					if (enableTranscript.Checked)
					{
						_recordingTranscript = true;
						var selectedModel = SelectedTranscriptModel();
						_transcriptFilePath = outputFilePath + ".txt";
						transcriptText.Clear();
						_transcriptPlayback = new TranscriptPlayback(transcriberPath.Text.Trim(), outputFilePath,
							languages, transcriptMode.SelectedIndex == 1,
							selectedModel.Backend, selectedModel.Model, _ioRecorder.FeedLiveTranscript,
							diagnosticLogging.Checked,
							OnTranscriptSnapshot, OnTranscriptStatus);
					}
				}
				catch (Exception ex)
				{
					_recordingTranscript = false;
					if (_tempCaptureLock != null) { _tempCaptureLock.Dispose(); _tempCaptureLock = null; }
					try
					{
						if (_ioRecorder.IsRecording) await Task.Run(() => _ioRecorder.StopCapture());
						DeleteSourceFiles(captureFilePath);
					}
					catch { }
					MessageBox.Show("Could not start recording: " + ex.Message);
					return;
				}
				if (_ioRecorder.IsRecording)
				{
					record_button.Visible = false;
					muteButton.Text = "Mute";
					muteButton.Visible = true;
					recordingStopButton.Visible = true;
					SetTabsLocked(true);
                    this.compressToMP3.Enabled = false;
                    saveAudio.Enabled = false;
                    enableTranscript.Enabled = false;
                    englishLanguage.Enabled = false;
                    frenchLanguage.Enabled = false;
                    transcriptMode.Enabled = false;
                    transcriptModel.Enabled = false;
                    transcriberPath.Locked = true;
                    browseTranscriber.Enabled = false;
                    browseLocationTextBox.Locked = true;
                    browseButton.Enabled = false;
					this.timer.Tick += updateStatusTimer_Tick;
					this.timer.Enabled = true;
					this.loopbackBar.Visible = true;
					this.inputBar.Visible = true;
				}
			}
			else
			{
				recordingStopButton.Enabled = false;
				muteButton.Enabled = false;
				try
				{
                    await Task.Run(() => _ioRecorder.StopCapture());
					_recordingStopped = true;
					if (_transcriptPlayback != null) transcriptStatus.Text = "Finishing transcription…";
					timer.Tick -= updateStatusTimer_Tick;
					timer.Enabled = false;
					if (saveAudio.Checked)
					{
						_recordingAudioFinishing = true;
						compressionBar.Value = 0;
						compressionBar.Visible = true;
						string progressVerb = compressToMP3.Checked ? "Compressing" : "Mixing";
						compressionStatus.Text = progressVerb + " 0%";
						compressionStatus.Visible = true;
						var progress = new Progress<double>(value => {
							if (IsDisposed) return;
							int percent = (int)value;
							compressionBar.Value = percent;
							compressionStatus.Text = progressVerb + " " + percent + "%";
						});
						await Task.Run(() => _ioRecorder.FinishRecording(progress));
						_recordingAudioFinishing = false;
					}
					TryCleanupRecordingSources();
				}
                catch (Exception ex) {
                    if (saveAudio.Checked)
                        _recordingCleanupBase = null; // Preserve source WAVs if finalization failed.
                    else
                    {
                        if (_transcriptPlayback != null)
                        {
                            try { _transcriptPlayback.Dispose(); } catch { }
                            _transcriptPlayback = null;
                        }
                        _recordingTranscript = false;
                        _recordingStopped = true;
                        try { TryCleanupRecordingSources(); } catch { }
                    }
                    MessageBox.Show("Recording or mixing failed: " + ex.Message +
                        Environment.NewLine + ex.GetBaseException().Message);
                }
				finally
				{
				_recordingAudioFinishing = false;
					record_button.Visible = true;
					muteButton.Visible = false;
					recordingStopButton.Visible = false;
					recordingStopButton.Enabled = true;
					muteButton.Enabled = true;
					_ioRecorder.IsMuted = false;
					SetTabsLocked(_transcriptPlayback != null);
					record_button.Enabled = _transcriptPlayback == null &&
						inputComboBox.SelectedIndex >= 0 && loopbackComboBox.SelectedIndex >= 0;
					compressToMP3.Enabled = true;
					saveAudio.Enabled = true;
					enableTranscript.Enabled = _transcriptPlayback == null;
					englishLanguage.Enabled = true;
					frenchLanguage.Enabled = true;
					transcriptMode.Enabled = true;
					transcriptModel.Enabled = true;
					transcriberPath.Locked = false;
					browseTranscriber.Enabled = true;
					browseLocationTextBox.Locked = false;
					browseButton.Enabled = true;
					timer.Tick -= updateStatusTimer_Tick;
					timer.Enabled = false;
					elapsedText.Text = "";
					loopbackBar.Visible = false;
					inputBar.Visible = false;
					compressionBar.Visible = false;
					compressionStatus.Visible = false;
				}
			}
		}

		private void browseButton_Click(object sender, EventArgs e)
		{
			using (FolderBrowserDialog openFileDialog = new FolderBrowserDialog())
			{
				// Set initial directory to last location if available, otherwise default to current directory
				if (!string.IsNullOrEmpty(browseLocationTextBox.Text))
					openFileDialog.SelectedPath = browseLocationTextBox.Text;
				else
					openFileDialog.SelectedPath = Directory.GetCurrentDirectory();

				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					// Display the selected file path in the TextBox
					browseLocationTextBox.Text = openFileDialog.SelectedPath;
				}
			}
		}

		private void openButton_Click(object sender, EventArgs e)
		{
			string path = browseLocationTextBox.Text;
			if (!path.EndsWith("\\"))
				path += "\\";

			Process.Start("explorer.exe", browseLocationTextBox.Text);
		}

		private void openAudioButton_Click(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Filter = "Audio files|*.wav;*.mp3;*.m4a;*.wma;*.aac|All files|*.*";
				dialog.Title = "Open meeting audio";
				if (dialog.ShowDialog(this) != DialogResult.OK) return;
				StopPlayback();
				_audioFilePath = dialog.FileName;
				audioFileLabel.Text = Path.GetFileName(_audioFilePath);
				audioFileLabel.AccessibleDescription = _audioFilePath;
				playAudioButton.Enabled = !_playbackCaptureFinishing && playbackOutputCombo.SelectedIndex >= 0;
			}
		}

		private void browseTranscriber_Click(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Filter = "Executable files|*.exe|All files|*.*";
				dialog.Title = "Select verbastream executable";
				if (dialog.ShowDialog(this) == DialogResult.OK)
					transcriberPath.Text = dialog.FileName;
			}
		}

		private void enableTranscript_CheckedChanged(object sender, EventArgs e)
		{
			UpdateTranscriptLayout();
			transcriptStatus.Text = enableTranscript.Checked
				? "Ready to transcribe the next session."
				: "Transcript is off.";
			if (!enableTranscript.Checked && _transcriptPlayback != null)
			{
				_transcriptPlayback.Dispose();
				_transcriptPlayback = null;
			}
		}


		private void PrepareTranscript()
		{
			if (!enableTranscript.Checked || _audioFilePath == null || _transcriptPlayback != null) return;
			string[] languages = SelectedLanguages();
			transcriptText.Clear();
			string logDirectory = browseLocationTextBox.Text.Trim();
			Directory.CreateDirectory(logDirectory);
			string logPrefix = _sessionOutputBase ?? Path.Combine(logDirectory,
				Path.GetFileNameWithoutExtension(_audioFilePath) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
			_transcriptFilePath = Path.ChangeExtension(_sessionOutputBase ?? _audioFilePath, ".txt");
			_recordingTranscript = false;
			var selectedModel = SelectedTranscriptModel();
		_transcriptPlayback = new TranscriptPlayback(transcriberPath.Text.Trim(), _audioFilePath, logPrefix,
				() => _audioReader == null ? TimeSpan.Zero : _audioReader.CurrentTime,
				languages, transcriptMode.SelectedIndex == 1,
				selectedModel.Backend, selectedModel.Model, diagnosticLogging.Checked,
				OnTranscriptSnapshot, OnTranscriptStatus);
		}

		private void playAudioButton_Click(object sender, EventArgs e)
		{
			try
			{
				if (_playbackCaptureFinishing) return;
				if (_audioPlayer != null && _audioPlayer.PlaybackState == PlaybackState.Playing)
				{
					_audioPlayer.Pause();
					_playbackCapture?.PauseCapture();
					playbackMeterTimer.Stop();
					playbackLeftBar.Value = playbackRightBar.Value = 0;
					playAudioButton.Text = "Resume";
					return;
				}
				if (_audioPlayer == null)
				{
					if (playbackOutputCombo.SelectedIndex < 0)
						throw new InvalidOperationException("Select an output device first.");
					_audioReader = new MediaFoundationReader(_audioFilePath);
					MMDevice device = _ioRecorder.LoopbackDevices[playbackOutputCombo.SelectedIndex];
					_audioPlayer = new WasapiOut(device, AudioClientShareMode.Shared, true, 100);
					_audioPlayer.PlaybackStopped += AudioPlaybackStopped;
					var meter = new MeteringSampleProvider(_audioReader.ToSampleProvider());
					meter.StreamVolume += (source, volume) => {
						float[] peaks = volume.MaxSampleValues;
						if (peaks.Length == 0) return;
						playbackLeftLevel = (int)(Math.Min(1, peaks[0]) * 100);
						playbackRightLevel = (int)(Math.Min(1, peaks.Length > 1 ? peaks[1] : peaks[0]) * 100);
					};
					_audioPlayer.Init(new SampleToWaveProvider16(meter));
					_sessionOutputBase = saveAudio.Checked
						? NewOutputBase(Path.GetFileNameWithoutExtension(_audioFilePath) + "-playback-" +
							DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")) : null;
					if (saveAudio.Checked)
					{
						_playbackCapture = new ioRecorder { DiagnosticLogging = diagnosticLogging.Checked };
						_playbackCapture.StartLoopbackRecording(_sessionOutputBase, device, compressToMP3.Checked);
					}
				}
				PrepareTranscript();
				_playbackCapture?.ResumeCapture();
				_audioPlayer.Play();
				playbackLeftBar.Visible = true;
				playbackRightBar.Visible = true;
				playbackMeterTimer.Start();
				SetTabsLocked(true);
				playAudioButton.Text = "Pause";
				playAudioButton.Left = 554;
				stopAudioButton.Enabled = true;
				stopAudioButton.Visible = true;
				enableTranscript.Enabled = false;
				saveAudio.Enabled = false;
				compressToMP3.Enabled = false;
				englishLanguage.Enabled = false;
				frenchLanguage.Enabled = false;
				transcriptMode.Enabled = false;
				transcriptModel.Enabled = false;
				browseLocationTextBox.Locked = true;
				browseButton.Enabled = false;
				transcriberPath.Locked = true;
				browseTranscriber.Enabled = false;
			}
			catch (Exception ex)
			{
				StopPlayback();
				MessageBox.Show("Could not play audio: " + ex.Message);
			}
		}

		private void stopAudioButton_Click(object sender, EventArgs e)
		{
			if (_transcriptPlayback != null && _audioReader != null)
			{
				_transcriptPlayback.CompleteAt(_audioReader.CurrentTime);
				transcriptStatus.Text = "Finishing transcription…";
				StopPlayback(false);
			}
			else StopPlayback();
		}

		private void AudioPlaybackStopped(object sender, StoppedEventArgs e)
		{
			if (!IsDisposed && IsHandleCreated)
				BeginInvoke((Action)(() =>
				{
					if (_transcriptPlayback != null) _transcriptPlayback.PlaybackEnded();
					StopPlayback(false);
					if (e.Exception != null)
						MessageBox.Show("Audio playback failed: " + e.Exception.Message);
				}));
		}

		private void StopPlayback(bool stopTranscript = true)
		{
			if (stopTranscript && _transcriptPlayback != null)
			{
				_transcriptPlayback.Dispose();
				_transcriptPlayback = null;
				transcriptStatus.Text = "Transcription stopped.";
			}
			if (_audioPlayer != null)
			{
				_audioPlayer.PlaybackStopped -= AudioPlaybackStopped;
				_audioPlayer.Stop();
				_audioPlayer.Dispose();
				_audioPlayer = null;
			}
			if (_audioReader != null)
			{
				_audioReader.Dispose();
				_audioReader = null;
			}
			playbackMeterTimer.Stop();
			playbackLeftLevel = playbackRightLevel = 0;
			playbackLeftBar.Value = playbackRightBar.Value = 0;
			playbackLeftBar.Visible = playbackRightBar.Visible = false;
			if (playAudioButton != null)
			{
				playAudioButton.Text = "Play";
				playAudioButton.Left = 635;
				playAudioButton.Enabled = _audioFilePath != null && playbackOutputCombo.SelectedIndex >= 0 &&
					(stopTranscript || _transcriptPlayback == null);
			}
			if (stopAudioButton != null) stopAudioButton.Enabled = false;
			if (stopAudioButton != null) stopAudioButton.Visible = false;
			if (enableTranscript != null) enableTranscript.Enabled = true;
			if (saveAudio != null) saveAudio.Enabled = true;
			if (compressToMP3 != null) compressToMP3.Enabled = true;
			if (englishLanguage != null) englishLanguage.Enabled = true;
			if (frenchLanguage != null) frenchLanguage.Enabled = true;
			if (transcriptMode != null) transcriptMode.Enabled = true;
			if (transcriptModel != null) transcriptModel.Enabled = true;
			if (browseLocationTextBox != null) browseLocationTextBox.Locked = false;
			if (browseButton != null) browseButton.Enabled = true;
			if (transcriberPath != null) transcriberPath.Locked = false;
			if (browseTranscriber != null) browseTranscriber.Enabled = true;
			if (_playbackCapture != null)
			{
				ioRecorder capture = _playbackCapture;
				_playbackCapture = null;
				FinishPlaybackCaptureAsync(capture, _sessionOutputBase);
			}
			SetTabsLocked(_transcriptPlayback != null || _playbackCaptureFinishing);
		}

        private async void FinishPlaybackCaptureAsync(ioRecorder capture, string outputBase)
        {
            _playbackCaptureFinishing = true;
            playAudioButton.Enabled = false;
            openAudioButton.Enabled = false;
            saveAudio.Enabled = false;
            compressToMP3.Enabled = false;
            browseLocationTextBox.Locked = true;
            browseButton.Enabled = false;
            playbackSaveBar.Value = 0;
            playbackSaveBar.Visible = true;
            playbackSaveStatus.Text = "Saving playback audio 0%";
            playbackSaveStatus.Visible = true;
            try
            {
                await Task.Run(() => capture.StopCapture());
                var progress = new Progress<double>(value => {
                    if (IsDisposed) return;
                    int percent = (int)value;
                    playbackSaveBar.Value = percent;
                    playbackSaveStatus.Text = "Saving playback audio " + percent + "%";
                });
                await Task.Run(() => capture.FinishRecording(progress));
                DeleteSourceFiles(outputBase);
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show("Could not save playback audio: " + ex.GetBaseException().Message +
                    Environment.NewLine + (capture.DiagnosticLogging
                        ? "Source WAV and capture log were kept." : "Source WAV was kept."));
            }
            finally
            {
                if (!IsDisposed)
                {
                    _playbackCaptureFinishing = false;
                    SetTabsLocked(_transcriptPlayback != null);
                    playbackSaveBar.Visible = false;
                    playbackSaveStatus.Visible = false;
                    openAudioButton.Enabled = true;
                    saveAudio.Enabled = true;
                    compressToMP3.Enabled = true;
                    browseLocationTextBox.Locked = false;
                    browseButton.Enabled = true;
                    playAudioButton.Enabled = _audioFilePath != null && playbackOutputCombo.SelectedIndex >= 0 &&
                        _transcriptPlayback == null;
                }
            }
        }

		private void OnTranscriptStatus(string status)
		{
			if (IsDisposed || !IsHandleCreated) return;
			BeginInvoke((Action)(() => {
				if (IsDisposed) return;
				transcriptStatus.Text = status;
				int logDetails = status.IndexOf(" Log:", StringComparison.Ordinal);
				if (logDetails < 0) logDetails = status.IndexOf(" Logs:", StringComparison.Ordinal);
				transcriptText.SetStatus(logDetails < 0 ? status : status.Substring(0, logDetails));
				if ((status.StartsWith("Transcription complete.") ||
					 status.StartsWith("Transcription exited") ||
					 status.StartsWith("Transcription failed")) && _transcriptPlayback != null)
				{
					_transcriptPlayback.Dispose();
					_transcriptPlayback = null;
					if (_recordingTranscript)
					{
						_recordingTranscript = false;
						enableTranscript.Enabled = true;
						try { TryCleanupRecordingSources(); }
						catch (Exception ex) { transcriptStatus.Text = "Could not remove temporary audio: " + ex.Message; }
						record_button.Enabled = !_ioRecorder.IsRecording && !_recordingAudioFinishing &&
							inputComboBox.SelectedIndex >= 0 && loopbackComboBox.SelectedIndex >= 0;
					}
					else playAudioButton.Enabled = _audioFilePath != null && playbackOutputCombo.SelectedIndex >= 0 &&
						!_playbackCaptureFinishing;
					SetTabsLocked(_ioRecorder.IsRecording || _audioPlayer != null || _playbackCaptureFinishing);
				}
			}));
		}

		private void OnTranscriptSnapshot(string json)
		{
			if (IsDisposed || !IsHandleCreated) return;
			BeginInvoke((Action)(() =>
			{
				try
				{
					var snapshot = (Dictionary<string, object>)_json.DeserializeObject(json);
					var entries = new List<TranscriptItem>();
					foreach (object item in (object[])snapshot["utterances"])
					{
						var utterance = (Dictionary<string, object>)item;
						double seconds = Convert.ToDouble(utterance["start"]);
						string speaker = utterance["speaker"] == null ? "Speaker" : utterance["speaker"].ToString();
						bool pending = utterance["status"].ToString() != "confirmed";
						entries.Add(new TranscriptItem { Start = seconds, Speaker = speaker,
							Text = utterance["text"].ToString().TrimStart(), Tentative = pending });
					}
					var words = (object[])snapshot["unconfirmed_words"];
					StringBuilder partial = null;
					string partialSpeaker = null;
					double partialStart = 0;
					double previousEnd = 0;
					foreach (object item in words)
					{
						var word = (Dictionary<string, object>)item;
						double start = Convert.ToDouble(word["start"]);
						double end = Convert.ToDouble(word["end"]);
						string speaker = word["speaker"] == null ? "Speaker" : word["speaker"].ToString();
						if (partial != null && (speaker != partialSpeaker || start - previousEnd > 1.5))
						{
							entries.Add(new TranscriptItem { Start = partialStart, Speaker = partialSpeaker,
								Text = partial.ToString().Trim(), Tentative = true });
							partial = null;
						}
						if (partial == null)
						{
							partial = new StringBuilder();
							partialSpeaker = speaker;
							partialStart = start;
						}
						partial.Append(word["text"]);
						previousEnd = end;
					}
					if (partial != null)
						entries.Add(new TranscriptItem { Start = partialStart, Speaker = partialSpeaker,
							Text = partial.ToString().Trim(), Tentative = true });
					entries.Sort((a, b) => a.Start.CompareTo(b.Start));
					SaveTranscriptText(entries);
					transcriptText.SetSnapshot(json);
					transcriptStatus.Text = Convert.ToBoolean(snapshot["final"]) ? "Transcription complete." :
						"Transcribing — " + Convert.ToDouble(snapshot["audio_seconds"]).ToString("F0") + " seconds processed";
				}
				catch (Exception ex) { transcriptStatus.Text = "Could not read transcript snapshot: " + ex.Message; }
			}));
		}

        private void SaveTranscriptText(List<TranscriptItem> entries)
        {
            if (string.IsNullOrEmpty(_transcriptFilePath)) return;
            var text = new StringBuilder();
            foreach (var entry in entries)
                text.Append('[').Append(TimeSpan.FromSeconds(entry.Start).ToString(@"hh\:mm\:ss"))
                    .Append("] ").Append(entry.Speaker).Append(": ")
                    .Append(entry.Text).AppendLine();
            string temporary = _transcriptFilePath + ".tmp";
            File.WriteAllText(temporary, text.ToString(), new System.Text.UTF8Encoding(false));
            if (File.Exists(_transcriptFilePath)) File.Replace(temporary, _transcriptFilePath, null);
            else File.Move(temporary, _transcriptFilePath);
        }
	}
}
