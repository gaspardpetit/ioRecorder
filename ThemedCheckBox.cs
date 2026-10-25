using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ioRecord
{
    internal sealed class ThemedCheckBox : CheckBox
    {
        private bool darkTheme;

        public ThemedCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void ApplyTheme(bool dark)
        {
            darkTheme = dark;
            BackColor = dark ? Color.FromArgb(31, 33, 38) : SystemColors.Control;
            ForeColor = dark ? Color.FromArgb(232, 235, 240) : SystemColors.ControlText;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int top = (Height - 13) / 2;
            var box = new Rectangle(1, top, 13, 13);
            Color edge = darkTheme ? Color.FromArgb(121, 128, 139) : SystemColors.ControlDark;
            Color fill = Checked
                ? (darkTheme ? Color.FromArgb(102, 164, 197) : SystemColors.Highlight)
                : (darkTheme ? Color.FromArgb(45, 48, 54) : SystemColors.Window);
            if (!Enabled)
            {
                edge = darkTheme ? Color.FromArgb(94, 101, 112) : SystemColors.GrayText;
                fill = Checked ? (darkTheme ? Color.FromArgb(78, 112, 129) : SystemColors.ControlDark)
                    : (darkTheme ? Color.FromArgb(45, 48, 54) : SystemColors.Control);
            }
            using (var brush = new SolidBrush(fill)) e.Graphics.FillRectangle(brush, box);
            using (var pen = new Pen(edge)) e.Graphics.DrawRectangle(pen, box);
            if (Checked)
            {
                Color tick = darkTheme ? Color.White : SystemColors.HighlightText;
                using (var pen = new Pen(tick, 1.8f))
                {
                    e.Graphics.DrawLine(pen, 4, top + 7, 7, top + 10);
                    e.Graphics.DrawLine(pen, 7, top + 10, 12, top + 3);
                }
            }
            Color text = Enabled ? ForeColor : (darkTheme
                ? Color.FromArgb(166, 173, 183) : SystemColors.GrayText);
            TextRenderer.DrawText(e.Graphics, Text, Font,
                new Rectangle(19, 0, Math.Max(0, Width - 19), Height), text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
