namespace ioRecord
{
	partial class ioRecorderForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) StopPlayback();
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.inputComboBox = new ioRecord.ThemedComboBox();
            this.loopbackComboBox = new ioRecord.ThemedComboBox();
            this.record_button = new ioRecord.ThemedButton();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.browseLocationTextBox = new ioRecord.ThemedTextBox();
            this.browseButton = new ioRecord.ThemedButton();
            this.elapsedText = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.inputBar = new ioRecord.AudioLevelBar();
            this.loopbackBar = new ioRecord.AudioLevelBar();
            this.openButton = new ioRecord.ThemedButton();
            this.compressToMP3 = new ioRecord.ThemedCheckBox();
            this.openAudioButton = new ioRecord.ThemedButton();
            this.playAudioButton = new ioRecord.ThemedButton();
            this.stopAudioButton = new ioRecord.ThemedButton();
            this.audioFileLabel = new System.Windows.Forms.Label();
            this.transcriptGroup = new ioRecord.ThemedGroupBox();
            this.transcriptText = new ioRecord.TranscriptHtmlView();
            this.transcriptStatus = new System.Windows.Forms.Label();
            this.enableTranscript = new ioRecord.ThemedCheckBox();
            this.transcriberPathLabel = new System.Windows.Forms.Label();
            this.transcriberPath = new ioRecord.ThemedTextBox();
            this.browseTranscriber = new ioRecord.ThemedButton();
            this.transcriptGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputComboBox
            // 
            this.inputComboBox.FormattingEnabled = true;
            this.inputComboBox.Location = new System.Drawing.Point(111, 12);
            this.inputComboBox.Name = "inputComboBox";
            this.inputComboBox.Size = new System.Drawing.Size(223, 21);
            this.inputComboBox.TabIndex = 0;
            // 
            // loopbackComboBox
            // 
            this.loopbackComboBox.FormattingEnabled = true;
            this.loopbackComboBox.Location = new System.Drawing.Point(111, 39);
            this.loopbackComboBox.Name = "loopbackComboBox";
            this.loopbackComboBox.Size = new System.Drawing.Size(223, 21);
            this.loopbackComboBox.TabIndex = 1;
            // 
            // record_button
            // 
            this.record_button.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.record_button.Location = new System.Drawing.Point(454, 117);
            this.record_button.Name = "record_button";
            this.record_button.Size = new System.Drawing.Size(75, 23);
            this.record_button.TabIndex = 3;
            this.record_button.Text = "Record";
            this.record_button.UseVisualStyleBackColor = true;
            this.record_button.Click += new System.EventHandler(this.record_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(37, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Input Device";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Output Device";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(21, 71);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Output Directory";
            // 
            // browseLocationTextBox
            // 
            this.browseLocationTextBox.Location = new System.Drawing.Point(111, 66);
            this.browseLocationTextBox.Name = "browseLocationTextBox";
            this.browseLocationTextBox.Size = new System.Drawing.Size(254, 20);
            this.browseLocationTextBox.TabIndex = 8;
            // 
            // browseButton
            // 
            this.browseButton.Location = new System.Drawing.Point(373, 66);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(75, 23);
            this.browseButton.TabIndex = 9;
            this.browseButton.Text = "Browse";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // elapsedText
            // 
            this.elapsedText.AutoSize = true;
            this.elapsedText.Location = new System.Drawing.Point(383, 122);
            this.elapsedText.Name = "elapsedText";
            this.elapsedText.Size = new System.Drawing.Size(65, 13);
            this.elapsedText.TabIndex = 10;
            this.elapsedText.Text = "elapsedText";
            // 
            // inputBar
            // 
            this.inputBar.Location = new System.Drawing.Point(111, 92);
            this.inputBar.Name = "inputBar";
            this.inputBar.Size = new System.Drawing.Size(223, 23);
            this.inputBar.TabIndex = 11;
            // 
            // loopbackBar
            // 
            this.loopbackBar.Location = new System.Drawing.Point(111, 121);
            this.loopbackBar.Name = "loopbackBar";
            this.loopbackBar.Size = new System.Drawing.Size(223, 23);
            this.loopbackBar.TabIndex = 12;
            // 
            // openButton
            // 
            this.openButton.Location = new System.Drawing.Point(454, 66);
            this.openButton.Name = "openButton";
            this.openButton.Size = new System.Drawing.Size(75, 23);
            this.openButton.TabIndex = 13;
            this.openButton.Text = "Open";
            this.openButton.UseVisualStyleBackColor = true;
            this.openButton.Click += new System.EventHandler(this.openButton_Click);
            // 
            // compressToMP3
            // 
            this.compressToMP3.AutoSize = true;
            this.compressToMP3.Checked = true;
            this.compressToMP3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.compressToMP3.Location = new System.Drawing.Point(373, 26);
            this.compressToMP3.Name = "compressToMP3";
            this.compressToMP3.Size = new System.Drawing.Size(109, 17);
            this.compressToMP3.TabIndex = 14;
            this.compressToMP3.Text = "Compress to MP3";
            this.compressToMP3.UseVisualStyleBackColor = true;
            // openAudioButton
            this.openAudioButton.Location = new System.Drawing.Point(24, 161);
            this.openAudioButton.Name = "openAudioButton";
            this.openAudioButton.Size = new System.Drawing.Size(85, 23);
            this.openAudioButton.TabIndex = 15;
            this.openAudioButton.Text = "Open Audio";
            this.openAudioButton.UseVisualStyleBackColor = true;
            this.openAudioButton.Click += new System.EventHandler(this.openAudioButton_Click);
            // playAudioButton
            this.playAudioButton.Enabled = false;
            this.playAudioButton.Location = new System.Drawing.Point(115, 161);
            this.playAudioButton.Name = "playAudioButton";
            this.playAudioButton.Size = new System.Drawing.Size(75, 23);
            this.playAudioButton.TabIndex = 16;
            this.playAudioButton.Text = "Play";
            this.playAudioButton.UseVisualStyleBackColor = true;
            this.playAudioButton.Click += new System.EventHandler(this.playAudioButton_Click);
            // stopAudioButton
            this.stopAudioButton.Enabled = false;
            this.stopAudioButton.Location = new System.Drawing.Point(196, 161);
            this.stopAudioButton.Name = "stopAudioButton";
            this.stopAudioButton.Size = new System.Drawing.Size(75, 23);
            this.stopAudioButton.TabIndex = 17;
            this.stopAudioButton.Text = "Stop";
            this.stopAudioButton.UseVisualStyleBackColor = true;
            this.stopAudioButton.Click += new System.EventHandler(this.stopAudioButton_Click);
            // audioFileLabel
            this.audioFileLabel.AutoEllipsis = true;
            this.audioFileLabel.Location = new System.Drawing.Point(277, 166);
            this.audioFileLabel.Name = "audioFileLabel";
            this.audioFileLabel.Size = new System.Drawing.Size(252, 16);
            this.audioFileLabel.TabIndex = 18;
            this.audioFileLabel.Text = "No audio file selected";
            // transcriptGroup
            this.transcriptGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.transcriptGroup.Controls.Add(this.transcriptText);
            this.transcriptGroup.Controls.Add(this.transcriptStatus);
            this.transcriptGroup.Location = new System.Drawing.Point(24, 235);
            this.transcriptGroup.Name = "transcriptGroup";
            this.transcriptGroup.Size = new System.Drawing.Size(712, 300);
            this.transcriptGroup.TabIndex = 19;
            this.transcriptGroup.TabStop = false;
            this.transcriptGroup.Text = "Transcript";
            // transcriptText
            this.transcriptText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.transcriptText.BackColor = System.Drawing.SystemColors.Window;
            this.transcriptText.Location = new System.Drawing.Point(12, 42);
            this.transcriptText.Name = "transcriptText";
            this.transcriptText.Size = new System.Drawing.Size(688, 246);
            this.transcriptText.TabIndex = 1;
            // transcriptStatus
            this.transcriptStatus.AutoEllipsis = true;
            this.transcriptStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.transcriptStatus.Location = new System.Drawing.Point(12, 21);
            this.transcriptStatus.Name = "transcriptStatus";
            this.transcriptStatus.Size = new System.Drawing.Size(688, 16);
            this.transcriptStatus.TabIndex = 0;
            this.transcriptStatus.Text = "Transcript is off.";
            this.enableTranscript.AutoSize = true;
            this.enableTranscript.Location = new System.Drawing.Point(24, 164);
            this.enableTranscript.Name = "enableTranscript";
            this.enableTranscript.Size = new System.Drawing.Size(116, 17);
            this.enableTranscript.TabIndex = 20;
            this.enableTranscript.Text = "Enable transcript";
            this.enableTranscript.UseVisualStyleBackColor = true;
            this.enableTranscript.CheckedChanged += new System.EventHandler(this.enableTranscript_CheckedChanged);
            this.transcriberPathLabel.AutoSize = true;
            this.transcriberPathLabel.Location = new System.Drawing.Point(151, 202);
            this.transcriberPathLabel.Name = "transcriberPathLabel";
            this.transcriberPathLabel.Size = new System.Drawing.Size(79, 13);
            this.transcriberPathLabel.TabIndex = 21;
            this.transcriberPathLabel.Text = "verbastream:";
            this.transcriberPath.Location = new System.Drawing.Point(235, 198);
            this.transcriberPath.Name = "transcriberPath";
            this.transcriberPath.Size = new System.Drawing.Size(419, 20);
            this.transcriberPath.TabIndex = 22;
            this.browseTranscriber.Location = new System.Drawing.Point(660, 197);
            this.browseTranscriber.Name = "browseTranscriber";
            this.browseTranscriber.Size = new System.Drawing.Size(76, 23);
            this.browseTranscriber.TabIndex = 23;
            this.browseTranscriber.Text = "Browse";
            this.browseTranscriber.UseVisualStyleBackColor = true;
            this.browseTranscriber.Click += new System.EventHandler(this.browseTranscriber_Click);
            // 
            // ioRecorder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(760, 558);
            this.Controls.Add(this.browseTranscriber);
            this.Controls.Add(this.transcriberPath);
            this.Controls.Add(this.transcriberPathLabel);
            this.Controls.Add(this.enableTranscript);
            this.Controls.Add(this.transcriptGroup);
            this.Controls.Add(this.audioFileLabel);
            this.Controls.Add(this.stopAudioButton);
            this.Controls.Add(this.playAudioButton);
            this.Controls.Add(this.openAudioButton);
            this.Controls.Add(this.compressToMP3);
            this.Controls.Add(this.openButton);
            this.Controls.Add(this.loopbackBar);
            this.Controls.Add(this.inputBar);
            this.Controls.Add(this.elapsedText);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.browseLocationTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.record_button);
            this.Controls.Add(this.loopbackComboBox);
            this.Controls.Add(this.inputComboBox);
            this.MinimumSize = new System.Drawing.Size(776, 400);
            this.Name = "ioRecorder";
            this.Text = "ioRecorder";
            this.Load += new System.EventHandler(this.AudioRecorder_Load);
            this.transcriptGroup.ResumeLayout(false);
            this.transcriptGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private ioRecord.ThemedComboBox inputComboBox;
		private ioRecord.ThemedComboBox loopbackComboBox;
		private System.Windows.Forms.Button record_button;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private ioRecord.ThemedTextBox browseLocationTextBox;
		private System.Windows.Forms.Button browseButton;
		private System.Windows.Forms.Label elapsedText;
		private System.Windows.Forms.Timer timer;
		private ioRecord.AudioLevelBar inputBar;
		private ioRecord.AudioLevelBar loopbackBar;
		private System.Windows.Forms.Button openButton;
        private System.Windows.Forms.CheckBox compressToMP3;
        private System.Windows.Forms.Button openAudioButton;
        private System.Windows.Forms.Button playAudioButton;
        private System.Windows.Forms.Button stopAudioButton;
        private System.Windows.Forms.Label audioFileLabel;
        private ioRecord.ThemedGroupBox transcriptGroup;
        private ioRecord.TranscriptHtmlView transcriptText;
        private System.Windows.Forms.Label transcriptStatus;
        private System.Windows.Forms.CheckBox enableTranscript;
        private System.Windows.Forms.Label transcriberPathLabel;
        private ioRecord.ThemedTextBox transcriberPath;
        private System.Windows.Forms.Button browseTranscriber;
    }
}
