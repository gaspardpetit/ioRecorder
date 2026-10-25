using System;
using System.Drawing;
using System.Windows.Forms;

namespace ioRecord
{
    internal sealed class ThemedButton : Button
    {
        private bool hovered;
        private bool pressed;
        private bool darkTheme;

        public Color BorderColor { get; set; } = SystemColors.ControlDark;

        public ThemedButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            UseVisualStyleBackColor = false;
            FlatStyle = FlatStyle.Flat;
        }

        public void ApplyTheme(bool dark)
        {
            darkTheme = dark;
            UseVisualStyleBackColor = false;
            FlatStyle = FlatStyle.Flat;
            BackColor = dark ? Color.FromArgb(45, 48, 54) : SystemColors.Control;
            ForeColor = dark ? Color.FromArgb(232, 235, 240) : SystemColors.ControlText;
            BorderColor = dark ? Color.FromArgb(67, 72, 80) : SystemColors.ControlDark;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color fill = BackColor;
            if (Enabled && pressed)
                fill = darkTheme ? Color.FromArgb(70, 75, 84) : SystemColors.ControlDark;
            else if (Enabled && hovered)
                fill = darkTheme ? Color.FromArgb(59, 63, 71) : Color.FromArgb(235, 238, 242);
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
            using (var brush = new SolidBrush(fill))
                e.Graphics.FillRectangle(brush, 0, 0, Width - 1, Height - 1);
            using (var pen = new Pen(BorderColor))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            Color textColor = Enabled ? ForeColor : (darkTheme
                ? Color.FromArgb(130, 136, 145) : SystemColors.GrayText);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }
    }
}
