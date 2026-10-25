using System;
using System.Drawing;
using System.Windows.Forms;

namespace ioRecord
{
    internal sealed class AudioLevelBar : Control
    {
        private int level;
        public bool Vertical { get; set; }

        public int Value
        {
            get { return level; }
            set
            {
                int clamped = Math.Max(0, Math.Min(100, value));
                if (level == clamped) return;
                level = clamped;
                Invalidate();
            }
        }

        public AudioLevelBar()
        {
            DoubleBuffered = true;
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.Highlight;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            int fillSize = (int)Math.Round((Vertical ? Height : Width) * level / 100.0);
            if (fillSize > 0)
            {
                using (var brush = new SolidBrush(ForeColor))
                    if (Vertical)
                        e.Graphics.FillRectangle(brush, 0, Height - fillSize, Width, fillSize);
                    else
                        e.Graphics.FillRectangle(brush, 0, 0, fillSize, Height);
            }
        }
    }
}
