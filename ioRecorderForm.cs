using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ioRecord;

namespace ioRecord
{
    public partial class ioRecorderForm : Form
	{
        private ioRecorder _ioRecorder;

		public ioRecorderForm()
		{
			InitializeComponent();
            _ioRecorder = new ioRecorder();
            this.browseLocationTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sound Recordings");
            this.elapsedText.Text = "";
            this.loopbackBar.Visible = false;
            this.inputBar.Visible = false;
			this.compressToMP3.Checked = true;
		}

		private void AudioRecorder_Load(object sender, EventArgs e)
		{
			_ioRecorder.LoadDevices();

			foreach (var device in _ioRecorder.InputDevices)
			{
				inputComboBox.Items.Add(device.FriendlyName);
			}

			foreach (var device in _ioRecorder.LoopbackDevices)
			{
				loopbackComboBox.Items.Add(device.FriendlyName);
			}

			if (_ioRecorder.InputDevices.Count > 0)
				inputComboBox.SelectedIndex = 0;

			if (_ioRecorder.LoopbackDevices.Count > 0)
				loopbackComboBox.SelectedIndex = 0;
		}

		private void updateStatusTimer_Tick(object sender, EventArgs e)
		{
			TimeSpan elapsedTime = _ioRecorder.ElapsedTime;
            this.elapsedText.Text = elapsedTime.ToString(@"hh\:mm\:ss");
			this.inputBar.Value = (int)(Math.Max(0, Math.Min(100, _ioRecorder.InputLevel * 100)));
            this.loopbackBar.Value = (int)(Math.Max(0, Math.Min(100, _ioRecorder.LoopbackLevel * 100)));
		}

		private void record_Click(object sender, EventArgs e)
		{
			if (!_ioRecorder.IsRecording)
			{
                string outputFilePath = browseLocationTextBox.Text + "\\" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
				int inputIndex = inputComboBox.SelectedIndex;
                int loopbackIndex = loopbackComboBox.SelectedIndex;
				bool encodeToMP3 = this.compressToMP3.Checked;

                _ioRecorder.StartRecording(outputFilePath, inputIndex, loopbackIndex, encodeToMP3);
				if (_ioRecorder.IsRecording)
				{
                    this.record_button.Text = "Stop";
                    this.timer.Tick += updateStatusTimer_Tick;
                    this.timer.Enabled = true;
                    this.loopbackBar.Visible = true;
                    this.inputBar.Visible = true;
                }
            }
            else
			{
				_ioRecorder.StopRecording();
                if (_ioRecorder.IsRecording == false)
				{
                    record_button.Text = "Record";

                    timer.Tick -= updateStatusTimer_Tick;
                    timer.Enabled = false;
                    elapsedText.Text = "";
                    loopbackBar.Visible = false;
                    inputBar.Visible = false;
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
    }
}
