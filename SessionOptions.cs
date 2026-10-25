using System;
using System.Drawing;
using System.Windows.Forms;

namespace ioRecord
{
    public partial class ioRecorderForm
    {
        private sealed class TranscriptModelOption
        {
            public string Label { get; set; }
            public string Backend { get; set; }
            public string Model { get; set; }
            public override string ToString() { return Label; }
        }

        private CheckBox saveAudio;
        private CheckBox diagnosticLogging;
        private CheckBox englishLanguage;
        private CheckBox frenchLanguage;
        private Label languageLabel;
        private Label modeLabel;
        private ThemedComboBox transcriptMode;
        private Label modelLabel;
        private Label playbackOutputLabel;
        private ThemedComboBox transcriptModel;
        private ThemedButton settingsButton;
        private ThemedGroupBox settingsGroup;
        private ThemedGroupBox generalSettingsGroup;
        private ThemedGroupBox transcriptSettingsGroup;
        private ThemedGroupBox playbackSettingsGroup;
        private ThemedButton refreshDevicesButton;
        private int expandedTranscriptHeight = 300;
        private bool transcriptLayoutShown;

        private void InitializeSessionOptions()
        {
            saveAudio = new ThemedCheckBox { Text = "Save audio", AutoSize = true, Location = new Point(160, 127) };
            saveAudio.CheckedChanged += (s, e) => UpdateTranscriptLayout();
            enableTranscript.Location = new Point(24, 127);
            settingsButton = new ThemedButton {
                Text = "⚙ Settings", Location = new Point(630, 123), Size = new Size(106, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            settingsButton.Click += (s, e) => {
                settingsGroup.Visible = !settingsGroup.Visible;
                settingsButton.Text = settingsGroup.Visible ? "⚙ Close settings" : "⚙ Settings";
                settingsButton.Width = settingsGroup.Visible ? 130 : 106;
                settingsButton.Left = ClientSize.Width - settingsButton.Width - 24;
                UpdateTranscriptLayout();
            };
            settingsGroup = new ThemedGroupBox {
                Text = "Settings", Location = new Point(24, 158), Size = new Size(712, 310),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Visible = false
            };
            generalSettingsGroup = new ThemedGroupBox {
                Text = "General", Location = new Point(12, 23), Size = new Size(688, 83),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            transcriptSettingsGroup = new ThemedGroupBox {
                Text = "Transcript", Location = new Point(12, 113), Size = new Size(688, 111),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            playbackSettingsGroup = new ThemedGroupBox {
                Text = "Playback", Location = new Point(12, 231), Size = new Size(688, 66),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            settingsGroup.Controls.Add(generalSettingsGroup);
            settingsGroup.Controls.Add(transcriptSettingsGroup);
            settingsGroup.Controls.Add(playbackSettingsGroup);
            languageLabel = new Label { Text = "Languages:", AutoSize = true, Location = new Point(12, 27) };
            englishLanguage = new ThemedCheckBox { Text = "EN", AutoSize = true, Checked = true, Location = new Point(92, 24) };
            frenchLanguage = new ThemedCheckBox { Text = "FR", AutoSize = true, Checked = true, Location = new Point(146, 24) };
            modeLabel = new Label { Text = "Mode:", AutoSize = true, Location = new Point(215, 27) };
            transcriptMode = new ThemedComboBox { Size = new Size(178, 21), Location = new Point(258, 23) };
            transcriptMode.Items.Add("Low latency (3 s)");
            transcriptMode.Items.Add("Quality (30 s context)");
            transcriptMode.SelectedIndex = 0;
            modelLabel = new Label { Text = "Model:", AutoSize = true, Location = new Point(12, 56) };
            transcriptModel = new ThemedComboBox { Size = new Size(250, 21), Location = new Point(104, 52) };
            transcriptModel.Items.Add(new TranscriptModelOption { Label = "Whisper large-v3", Backend = "whisper", Model = "large-v3" });
            transcriptModel.Items.Add(new TranscriptModelOption { Label = "Qwen3-ASR 1.7B", Backend = "qwen" });
            transcriptModel.Items.Add(new TranscriptModelOption { Label = "Parakeet v2 (NVIDIA)", Backend = "parakeet",
                Model = "nvidia/parakeet-tdt-0.6b-v2" });
            transcriptModel.Items.Add(new TranscriptModelOption { Label = "Parakeet v3 (FR media)", Backend = "parakeet",
                Model = "Archime/parakeet-tdt-0.6b-v3-fr-tv-media" });
            transcriptModel.Items.Add(new TranscriptModelOption { Label = "Parakeet v3 (NVIDIA)", Backend = "parakeet",
                Model = "nvidia/parakeet-tdt-0.6b-v3" });
            transcriptModel.SelectedIndex = 0;
            foreach (Control control in new Control[] { languageLabel, englishLanguage, frenchLanguage,
                modeLabel, transcriptMode, modelLabel, transcriptModel }) transcriptSettingsGroup.Controls.Add(control);

            playbackOutputLabel = new Label { Text = "Playback output", AutoSize = true,
                Location = new Point(12, 28) };
            playbackOutputCombo.Location = new Point(104, 24);
            playbackOutputCombo.Size = new Size(330, 21);
            playbackSettingsGroup.Controls.Add(playbackOutputLabel);
            playbackSettingsGroup.Controls.Add(playbackOutputCombo);

            transcriberPathLabel.Location = new Point(12, 85);
            transcriberPath.Location = new Point(104, 81);
            transcriberPath.Size = new Size(477, 20);
            transcriberPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            browseTranscriber.Location = new Point(589, 80);
            browseTranscriber.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            foreach (Control control in new Control[] { transcriberPathLabel, transcriberPath,
                browseTranscriber }) transcriptSettingsGroup.Controls.Add(control);

            label3.Text = "Output folder";
            label3.Location = new Point(12, 29);
            browseLocationTextBox.Location = new Point(104, 25);
            browseLocationTextBox.Size = new Size(477, 20);
            browseLocationTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            browseButton.Location = new Point(589, 24);
            browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openButton.Location = new Point(104, 53);
            compressToMP3.Location = new Point(195, 56);
            diagnosticLogging = new ThemedCheckBox { Text = "Diagnostic logging", AutoSize = true,
                Location = new Point(350, 56) };
            refreshDevicesButton = new ThemedButton {
                Text = "Refresh devices", Location = new Point(445, 23), Size = new Size(118, 23)
            };
            refreshDevicesButton.Click += (s, e) => RefreshAudioDevices();
            foreach (Control control in new Control[] { label3, browseLocationTextBox,
                browseButton, openButton, compressToMP3, diagnosticLogging }) generalSettingsGroup.Controls.Add(control);
            playbackSettingsGroup.Controls.Add(refreshDevicesButton);

            Controls.Add(saveAudio);
            Controls.Add(settingsButton);
            Controls.Add(settingsGroup);
            settingsGroup.BringToFront();
            settingsButton.BringToFront();
        }

        private TranscriptModelOption SelectedTranscriptModel()
        {
            if (transcriptModel.SelectedIndex < 0)
                throw new InvalidOperationException("Select a transcription model.");
            return (TranscriptModelOption)transcriptModel.Items[transcriptModel.SelectedIndex];
        }

        private void RestoreSessionOptions()
        {
            var settings = Properties.Settings.Default;
            browseLocationTextBox.Text = string.IsNullOrWhiteSpace(settings.OutputFolder)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sound Recordings")
                : settings.OutputFolder;
            saveAudio.Checked = settings.SaveAudio;
            compressToMP3.Checked = settings.CompressMp3;
            diagnosticLogging.Checked = settings.DiagnosticLogging;
            englishLanguage.Checked = settings.EnglishLanguage;
            frenchLanguage.Checked = settings.FrenchLanguage;
            transcriptMode.SelectedIndex = settings.TranscriptMode >= 0 && settings.TranscriptMode < transcriptMode.Items.Count
                ? settings.TranscriptMode : 0;
            for (int i = 0; i < transcriptModel.Items.Count; i++)
            {
                var option = (TranscriptModelOption)transcriptModel.Items[i];
                if (option.Backend + ":" + option.Model == settings.TranscriptModel)
                {
                    transcriptModel.SelectedIndex = i;
                    break;
                }
            }
            enableTranscript.Checked = settings.EnableTranscript;
        }

        private void SaveSessionOptions()
        {
            var settings = Properties.Settings.Default;
            settings.OutputFolder = browseLocationTextBox.Text.Trim();
            settings.SaveAudio = saveAudio.Checked;
            settings.CompressMp3 = compressToMP3.Checked;
            settings.DiagnosticLogging = diagnosticLogging.Checked;
            settings.EnableTranscript = enableTranscript.Checked;
            settings.EnglishLanguage = englishLanguage.Checked;
            settings.FrenchLanguage = frenchLanguage.Checked;
            settings.TranscriptMode = transcriptMode.SelectedIndex;
            var option = SelectedTranscriptModel();
            settings.TranscriptModel = option.Backend + ":" + option.Model;
        }

        private void UpdateTranscriptLayout()
        {
            bool transcript = enableTranscript.Checked;
            if (transcriptLayoutShown)
                expandedTranscriptHeight = Math.Max(140, transcriptGroup.Height);
            int contentTop = settingsGroup.Visible ? settingsGroup.Bottom + 8 : 158;
            transcriptGroup.Visible = false;
            transcriptGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            if (transcript)
            {
                transcriptGroup.Top = contentTop;
                MinimumSize = new Size(776, 400);
                if (WindowState == FormWindowState.Normal)
                    ClientSize = new Size(ClientSize.Width, transcriptGroup.Top + expandedTranscriptHeight + 23);
                transcriptGroup.Height = Math.Max(140, ClientSize.Height - transcriptGroup.Top - 23);
                transcriptGroup.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                    AnchorStyles.Left | AnchorStyles.Right;
                transcriptGroup.Visible = true;
            }
            else
            {
                MinimumSize = new Size(776, settingsGroup.Visible ? settingsGroup.Bottom + 62 : 200);
                if (WindowState == FormWindowState.Normal)
                    ClientSize = new Size(ClientSize.Width, settingsGroup.Visible ? settingsGroup.Bottom + 23 : 166);
            }
            transcriptLayoutShown = transcript;
        }
    }
}
