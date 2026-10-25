using System;
using System.Drawing;
using System.Windows.Forms;

namespace ioRecord
{
    internal sealed class ThemedTextBox : UserControl
    {
        private readonly TextBox editor;
        private Color borderColor = SystemColors.ControlDark;

        public bool Locked
        {
            get { return editor.ReadOnly; }
            set { editor.ReadOnly = value; }
        }

        public override string Text
        {
            get { return editor == null ? base.Text : editor.Text; }
            set
            {
                if (editor == null) base.Text = value;
                else editor.Text = value;
            }
        }

        public ThemedTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
            editor = new TextBox { BorderStyle = BorderStyle.None };
            editor.TextChanged += (s, e) => OnTextChanged(e);
            Controls.Add(editor);
            Size = new Size(250, 20);
            TabStop = true;
            LayoutEditor();
        }

        public void ApplyTheme(bool dark)
        {
            BackColor = dark ? Color.FromArgb(45, 48, 54) : SystemColors.Window;
            ForeColor = dark ? Color.FromArgb(232, 235, 240) : SystemColors.WindowText;
            borderColor = dark ? Color.FromArgb(67, 72, 80) : SystemColors.ControlDark;
            editor.BackColor = BackColor;
            editor.ForeColor = ForeColor;
            Invalidate();
        }

        private void LayoutEditor()
        {
            if (editor == null) return;
            editor.Location = new Point(5, Math.Max(1, (Height - editor.PreferredHeight) / 2));
            editor.Width = Math.Max(0, Width - 10);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutEditor();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            editor.Focus();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            editor.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            using (var pen = new Pen(borderColor))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
