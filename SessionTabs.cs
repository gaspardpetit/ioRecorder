using System;
using System.Drawing;
using System.Windows.Forms;

namespace ioRecord
{
    public partial class ioRecorderForm
    {
        private Panel sessionArea;
        private Panel recordingPage;
        private Panel playbackPage;
        private ThemedButton recordingTabButton;
        private ThemedButton playbackTabButton;
        private Label compressionStatus;
        private AudioLevelBar compressionBar;
        private Label playbackSaveStatus;
        private AudioLevelBar playbackSaveBar;
        private ThemedComboBox playbackOutputCombo;
        private ThemedButton muteButton;
        private ThemedButton recordingStopButton;
        private AudioLevelBar playbackLeftBar;
        private AudioLevelBar playbackRightBar;
        private System.Windows.Forms.Timer playbackMeterTimer;
        private bool sessionTabsLocked;
        private Label deviceStatus;

        private void InitializeSessionTabs()
        {
            sessionArea = new Panel {
                Location = new Point(12, 5),
                Size = new Size(736, 115),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            recordingTabButton = new ThemedButton {
                Text = "Recording", Location = new Point(0, 0), Size = new Size(122, 30),
                FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false
            };
            playbackTabButton = new ThemedButton {
                Text = "Playback", Location = new Point(122, 0), Size = new Size(122, 30),
                FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false
            };
            deviceStatus = new Label {
                Location = new Point(280, 7), Size = new Size(450, 18),
                AutoEllipsis = true, Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            recordingPage = new Panel {
                Location = new Point(0, 30), Size = new Size(736, 85),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            playbackPage = new Panel {
                Location = new Point(0, 30), Size = new Size(736, 85),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false
            };
            recordingTabButton.Click += (s, e) => SelectSessionTab(false);
            playbackTabButton.Click += (s, e) => SelectSessionTab(true);

            Control[] recordingControls = {
                label1, inputComboBox, label2, loopbackComboBox,
                inputBar, loopbackBar, elapsedText, record_button
            };
            foreach (Control control in recordingControls)
                recordingPage.Controls.Add(control);
            label1.Text = "Microphone";
            label1.Location = new Point(12, 12);
            inputComboBox.Location = new Point(110, 8);
            inputComboBox.Size = new Size(330, 21);
            label2.Text = "Output device";
            label2.Location = new Point(12, 46);
            loopbackComboBox.Location = new Point(110, 42);
            loopbackComboBox.Size = new Size(330, 21);
            inputBar.Vertical = true;
            loopbackBar.Vertical = true;
            inputBar.Location = new Point(475, 9);
            loopbackBar.Location = new Point(494, 9);
            inputBar.Size = new Size(10, 55);
            loopbackBar.Size = new Size(10, 55);
            record_button.Location = new Point(635, 27);
            muteButton = new ThemedButton { Text = "Mute", Location = new Point(554, 27),
                Size = new Size(75, 23), Visible = false };
            muteButton.Click += (s, e) => ToggleRecordingMute();
            recordingStopButton = new ThemedButton { Text = "Stop", Location = new Point(635, 27),
                Size = new Size(75, 23), Visible = false };
            recordingStopButton.Click += record_Click;
            recordingPage.Controls.Add(muteButton);
            recordingPage.Controls.Add(recordingStopButton);
            elapsedText.Location = new Point(555, 55);
            compressionStatus = new Label {
                Location = new Point(110, 68), Size = new Size(175, 17),
                Text = "Compressing 0%", Visible = false
            };
            compressionBar = new AudioLevelBar {
                Location = new Point(290, 71), Size = new Size(175, 12), Visible = false
            };
            recordingPage.Controls.Add(compressionStatus);
            recordingPage.Controls.Add(compressionBar);

            Control[] playbackControls = {
                openAudioButton, playAudioButton, stopAudioButton, audioFileLabel
            };
            foreach (Control control in playbackControls)
            {
                playbackPage.Controls.Add(control);
            }
            openAudioButton.Text = "Select Audio";
            openAudioButton.Location = new Point(24, 8);
            openAudioButton.Size = new Size(105, 23);
            audioFileLabel.Location = new Point(24, 45);
            audioFileLabel.Size = new Size(420, 20);
            playAudioButton.Location = new Point(635, 27);
            stopAudioButton.Location = new Point(635, 27);
            stopAudioButton.Visible = false;
            playbackOutputCombo = new ThemedComboBox {
                Size = new Size(223, 21)
            };
            playbackLeftBar = new AudioLevelBar { Vertical = true, Location = new Point(475, 9),
                Size = new Size(10, 55), Visible = false };
            playbackRightBar = new AudioLevelBar { Vertical = true, Location = new Point(494, 9),
                Size = new Size(10, 55), Visible = false };
            playbackPage.Controls.Add(playbackLeftBar);
            playbackPage.Controls.Add(playbackRightBar);
            playbackMeterTimer = new System.Windows.Forms.Timer(components) { Interval = 100 };
            playbackMeterTimer.Tick += (s, e) => {
                playbackLeftBar.Value = playbackLeftLevel;
                playbackRightBar.Value = playbackRightLevel;
            };
            playbackSaveStatus = new Label {
                Location = new Point(24, 68), Size = new Size(175, 17), Visible = false
            };
            playbackSaveBar = new AudioLevelBar {
                Location = new Point(205, 71), Size = new Size(220, 12), Visible = false
            };
            playbackPage.Controls.Add(playbackSaveStatus);
            playbackPage.Controls.Add(playbackSaveBar);

            sessionArea.Controls.Add(recordingPage);
            sessionArea.Controls.Add(playbackPage);
            sessionArea.Controls.Add(recordingTabButton);
            sessionArea.Controls.Add(playbackTabButton);
            sessionArea.Controls.Add(deviceStatus);
            Controls.Add(sessionArea);
            sessionArea.BringToFront();
            SelectSessionTab(false);
        }

        private void SelectSessionTab(bool playback)
        {
            if (sessionTabsLocked && playback != playbackPage.Visible) return;
            recordingPage.Visible = !playback;
            playbackPage.Visible = playback;
            Color selected = darkTheme ? Color.FromArgb(45, 48, 54) : SystemColors.Window;
            Color unselected = darkTheme ? Color.FromArgb(31, 33, 38) : SystemColors.Control;
            recordingTabButton.BackColor = playback ? unselected : selected;
            playbackTabButton.BackColor = playback ? selected : unselected;
            Color edge = darkTheme ? Color.FromArgb(67, 72, 80) : SystemColors.ControlDark;
            recordingTabButton.FlatAppearance.BorderColor = edge;
            playbackTabButton.FlatAppearance.BorderColor = edge;
        }

        private void SetTabsLocked(bool locked)
        {
            sessionTabsLocked = locked;
            recordingTabButton.Enabled = !locked || recordingPage.Visible;
            playbackTabButton.Enabled = !locked || playbackPage.Visible;
        }
    }
}
