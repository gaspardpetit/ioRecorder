using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace Record
{
	public partial class ioRecorder : Form
	{
		private List<MMDevice> inputDevices;
		private List<MMDevice> loopbackDevices;
		private bool isRecording;
		private Thread inputRecordingThread;
		private Thread loopbackRecordingThread;
		WasapiLoopbackCapture loopbackWaveProvider;
		private DateTime startTime;
		private double inputLevel = 0;
		private double loopbackLevel = 0;

		int inputIndex = 0;
		int loopbackIndex = 0;

		WaveFormat waveFormat;
		string outputFilePath;


		public ioRecorder()
		{
			InitializeComponent();
			isRecording = false;
			inputRecordingThread = null;
			loopbackRecordingThread = null;
			waveFormat = new WaveFormat(44100, 16, 2);
			browseLocationTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sound Recordings");

			elapsedText.Text = "";
			loopbackBar.Visible = false;
			inputBar.Visible = false;
		}

		private void AudioRecorder_Load(object sender, EventArgs e)
		{
			inputDevices = GetAudioDevices(DataFlow.Capture);
			loopbackDevices = GetAudioDevices(DataFlow.Render);

			foreach (var device in inputDevices)
			{
				inputComboBox.Items.Add(device.FriendlyName);
			}

			foreach (var device in loopbackDevices)
			{
				loopbackComboBox.Items.Add(device.FriendlyName);
			}

			if (inputDevices.Count > 0)
				inputComboBox.SelectedIndex = 0;

			if (loopbackDevices.Count > 0)
				loopbackComboBox.SelectedIndex = 0;
		}

		private List<MMDevice> GetAudioDevices(DataFlow dataFlow)
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

		private void Timer1_Tick(object sender, EventArgs e)
		{
			TimeSpan elapsedTime = DateTime.Now - startTime;
			elapsedText.Text = elapsedTime.ToString(@"hh\:mm\:ss");
			inputBar.Value = (int)(Math.Max(0, Math.Min(100, inputLevel * 100)));
			loopbackBar.Value = (int)(Math.Max(0, Math.Min(100, loopbackLevel * 100)));
		}


		private void record_Click(object sender, EventArgs e)
		{
			if (!isRecording)
			{
				startTime = DateTime.Now;
				timer.Tick += Timer1_Tick;
				timer.Enabled = true;
				loopbackBar.Visible = true;
				inputBar.Visible = true;


				outputFilePath = browseLocationTextBox.Text + "\\" + DateTime.Now.ToString("yyyyMMdd-HHmmss");


				// Start recording
				isRecording = true;
				record_button.Text = "Stop";

				// Start input device recording thread
				inputIndex = inputComboBox.SelectedIndex;
				inputRecordingThread = new Thread(InputRecordingThread);
				inputRecordingThread.Start();

				// Start loopback device recording thread
				loopbackIndex = loopbackComboBox.SelectedIndex;
				MMDevice loopbackDevice = loopbackDevices[loopbackIndex];
				loopbackWaveProvider = new WasapiLoopbackCapture(loopbackDevice)
				{
					WaveFormat = waveFormat
				};

				loopbackRecordingThread = new Thread(LoopbackRecordingThread);
				loopbackRecordingThread.Start();
			}
			else
			{

				timer.Tick -= Timer1_Tick;
				timer.Enabled = false;
				elapsedText.Text = "";
				loopbackBar.Visible = false;
				inputBar.Visible = false;


				// Stop recording
				isRecording = false;
				record_button.Text = "Record";
				loopbackRecordingThread.Join();
				inputRecordingThread.Join();

				// Mix input and loopback recordings
				MixAudioFiles(outputFilePath + ".input.wav", outputFilePath + ".loopback.wav", outputFilePath + ".wav");
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
					int sampleRate = inputReader.WaveFormat.SampleRate;
					int bitsPerSample = inputReader.WaveFormat.BitsPerSample;
					int channels = inputReader.WaveFormat.Channels;
					// Ensure both files have the same sample rate, bit depth, and number of channels
					if (sampleRate != loopbackReader.WaveFormat.SampleRate ||
						bitsPerSample != loopbackReader.WaveFormat.BitsPerSample ||
						channels != loopbackReader.WaveFormat.Channels)
					{
						MessageBox.Show("Input and loopback files must have the same sample rate, bit depth, and number of channels.");
						return;
					}

					// Create mixed WAV file
					WaveFormat outputFormat = new WaveFormat(sampleRate, bitsPerSample, 2);
					using (var mixedWriter = new WaveFileWriter(outputFileName, outputFormat))
					{
						int bytesPerSample = channels * bitsPerSample / 8;

						// Read and mix audio data sample by sample
						while (inputReader.Position < inputReader.Length && loopbackReader.Position < loopbackReader.Length)
						{
							// Read input and loopback samples
							byte[] inputBuffer = new byte[bytesPerSample];
							byte[] loopbackBuffer = new byte[bytesPerSample];
							inputReader.Read(inputBuffer, 0, bytesPerSample);
							loopbackReader.Read(loopbackBuffer, 0, bytesPerSample);

							double inputSample = 0;
							double loopbackSample = 0;
							for (int i = 0; i < channels; ++i)
							{
								switch (bitsPerSample)
								{
									case 8:
										inputSample += inputBuffer[i * bitsPerSample / 8];
										loopbackSample += loopbackBuffer[i * bitsPerSample / 8];
										break;
									case 16:
										inputSample += BitConverter.ToInt16(inputBuffer, i * bitsPerSample / 8);
										loopbackSample += BitConverter.ToInt16(loopbackBuffer, i * bitsPerSample / 8);
										break;
									case 32:
										inputSample += BitConverter.ToInt32(inputBuffer, i * bitsPerSample / 8);
										loopbackSample += BitConverter.ToInt32(loopbackBuffer, i * bitsPerSample / 8);
										break;
								}
							}

							inputSample /= channels;
							loopbackSample /= channels;

							// Convert mixed samples back to bytes
							byte[] mixedBuffer = new byte[2 * bitsPerSample / 8];
							switch (bitsPerSample)
							{
								case 8:
									Buffer.BlockCopy(BitConverter.GetBytes((byte)inputSample), 0, mixedBuffer, 0, bitsPerSample / 8);
									Buffer.BlockCopy(BitConverter.GetBytes((byte)loopbackSample), 0, mixedBuffer, bitsPerSample / 8, bitsPerSample / 8);
									break;
								case 16:
									Buffer.BlockCopy(BitConverter.GetBytes((short)inputSample), 0, mixedBuffer, 0, bitsPerSample / 8);
									Buffer.BlockCopy(BitConverter.GetBytes((short)loopbackSample), 0, mixedBuffer, bitsPerSample / 8, bitsPerSample / 8);
									break;
								case 32:
									Buffer.BlockCopy(BitConverter.GetBytes((int)inputSample), 0, mixedBuffer, 0, bitsPerSample / 8);
									Buffer.BlockCopy(BitConverter.GetBytes((int)loopbackSample), 0, mixedBuffer, bitsPerSample / 8, bitsPerSample / 8);
									break;
							}

							// Write mixed samples to output file
							mixedWriter.Write(mixedBuffer, 0, mixedBuffer.Length);
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



		private void InputRecordingThread()
		{
			var inputWaveProvider = new WaveInEvent
			{
				DeviceNumber = inputIndex,
				WaveFormat = waveFormat,
				BufferMilliseconds = 50
			};

			using (var waveFileWriter = new WaveFileWriter(outputFilePath + ".input.wav", waveFormat))
			{
				inputWaveProvider.DataAvailable += (s, args) =>
				{
					waveFileWriter.Write(args.Buffer, 0, args.BytesRecorded);
					switch (waveFileWriter.WaveFormat.BitsPerSample)
					{
						case 8:
							inputLevel = 0;
							for (int i = 0; i < args.Buffer.Length; ++i)
								inputLevel = Math.Max(inputLevel, Math.Abs(i / 128.0));
							break;
						case 16:
							inputLevel = 0;
							for (int i = 0; i < args.Buffer.Length/2; ++i)
								inputLevel = Math.Max(inputLevel, Math.Abs((double)BitConverter.ToInt16(args.Buffer, i * 2)) / 32768.0);
							break;
						case 32:
							inputLevel = 0;
							for (int i = 0; i < args.Buffer.Length / 4; ++i)
								inputLevel = Math.Max(inputLevel, Math.Abs((double)BitConverter.ToInt32(args.Buffer, i * 4)) / 2147483648.0);
							break;
					}
				};

				inputWaveProvider.StartRecording();

				while (isRecording)
				{
					Thread.Sleep(100);
				}

				inputWaveProvider.StopRecording();
			}
		}

		private void LoopbackRecordingThread()
		{
			using (var waveFileWriter = new WaveFileWriter(outputFilePath + ".loopback.wav", waveFormat))
			{
				loopbackWaveProvider.DataAvailable += (s, args) =>
				{
					waveFileWriter.Write(args.Buffer, 0, args.BytesRecorded);
					switch (waveFileWriter.WaveFormat.BitsPerSample)
					{
						case 8:
							loopbackLevel = 0;
							for (int i = 0; i < args.Buffer.Length; ++i)
								loopbackLevel = Math.Max(loopbackLevel, Math.Abs(i / 128.0));
							break;
						case 16:
							loopbackLevel = 0;
							for (int i = 0; i < args.Buffer.Length / 2; ++i)
								loopbackLevel = Math.Max(loopbackLevel, Math.Abs((double)BitConverter.ToInt16(args.Buffer, i * 2)) / 32768.0);
							break;
						case 32:
							loopbackLevel = 0;
							for (int i = 0; i < args.Buffer.Length / 4; ++i)
								loopbackLevel = Math.Max(loopbackLevel, Math.Abs((double)BitConverter.ToInt32(args.Buffer, i * 4)) / 2147483648.0);
							break;
					}
				};

				loopbackWaveProvider.StartRecording();

				while (isRecording)
				{
					Thread.Sleep(100);
				}

				loopbackWaveProvider.StopRecording();
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
