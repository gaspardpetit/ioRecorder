namespace Record
{
	partial class ioRecorder
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
			this.inputComboBox = new System.Windows.Forms.ComboBox();
			this.loopbackComboBox = new System.Windows.Forms.ComboBox();
			this.record_button = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.browseLocationTextBox = new System.Windows.Forms.TextBox();
			this.browseButton = new System.Windows.Forms.Button();
			this.elapsedText = new System.Windows.Forms.Label();
			this.timer = new System.Windows.Forms.Timer(this.components);
			this.inputBar = new System.Windows.Forms.ProgressBar();
			this.loopbackBar = new System.Windows.Forms.ProgressBar();
			this.openButton = new System.Windows.Forms.Button();
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
			this.inputBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.inputBar.TabIndex = 11;
			// 
			// loopbackBar
			// 
			this.loopbackBar.Location = new System.Drawing.Point(111, 121);
			this.loopbackBar.Name = "loopbackBar";
			this.loopbackBar.Size = new System.Drawing.Size(223, 23);
			this.loopbackBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
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
			// ioRecorder
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(555, 149);
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
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "ioRecorder";
			this.Text = "ioRecorder";
			this.Load += new System.EventHandler(this.AudioRecorder_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox inputComboBox;
		private System.Windows.Forms.ComboBox loopbackComboBox;
		private System.Windows.Forms.Button record_button;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox browseLocationTextBox;
		private System.Windows.Forms.Button browseButton;
		private System.Windows.Forms.Label elapsedText;
		private System.Windows.Forms.Timer timer;
		private System.Windows.Forms.ProgressBar inputBar;
		private System.Windows.Forms.ProgressBar loopbackBar;
		private System.Windows.Forms.Button openButton;
	}
}