using System.Drawing;
using System.Windows.Forms;

namespace ioRecord
{
    internal sealed class ThemedGroupBox : GroupBox
    {
        public Color BorderColor { get; set; } = SystemColors.ControlDark;

        public ThemedGroupBox()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            Size textSize = TextRenderer.MeasureText(Text, Font);
            int borderY = textSize.Height / 2;
            using (var pen = new Pen(BorderColor))
            {
                e.Graphics.DrawLine(pen, 0, borderY, 7, borderY);
                e.Graphics.DrawLine(pen, 11 + textSize.Width, borderY, Width - 1, borderY);
                e.Graphics.DrawLine(pen, 0, borderY, 0, Height - 1);
                e.Graphics.DrawLine(pen, Width - 1, borderY, Width - 1, Height - 1);
                e.Graphics.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, new Point(8, 0), ForeColor);
        }
    }
}
