using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ioRecord
{
    internal sealed class ThemedComboBox : Control
    {
        private readonly List<object> items = new List<object>();
        private int selectedIndex = -1;
        private bool darkTheme;

        public List<object> Items { get { return items; } }
        public bool FormattingEnabled { get; set; }
        public event EventHandler SelectedIndexChanged;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                if (value < -1 || value >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (selectedIndex == value) return;
                selectedIndex = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ThemedComboBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(223, 21);
        }

        public void ApplyTheme(bool dark)
        {
            darkTheme = dark;
            BackColor = dark ? Color.FromArgb(45, 48, 54) : SystemColors.Window;
            ForeColor = dark ? Color.FromArgb(232, 235, 240) : SystemColors.WindowText;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            Color edge = darkTheme ? Color.FromArgb(67, 72, 80) : SystemColors.ControlDark;
            using (var pen = new Pen(edge))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            string selectedText = selectedIndex < 0 ? "" : Convert.ToString(items[selectedIndex]);
            TextRenderer.DrawText(e.Graphics, selectedText, Font,
                new Rectangle(5, 1, Math.Max(0, Width - 29), Height - 2), ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            Point[] arrow = {
                new Point(Width - 17, Height / 2 - 2),
                new Point(Width - 9, Height / 2 - 2),
                new Point(Width - 13, Height / 2 + 2)
            };
            using (var brush = new SolidBrush(ForeColor))
                e.Graphics.FillPolygon(brush, arrow);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ShowChoices();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && e.Alt || e.KeyCode == Keys.F4 ||
                e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                ShowChoices();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
            {
                if (items.Count == 0) return;
                int step = e.KeyCode == Keys.Down ? 1 : -1;
                SelectedIndex = Math.Max(0, Math.Min(items.Count - 1, selectedIndex + step));
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void ShowChoices()
        {
            if (!Enabled || items.Count == 0) return;
            var list = new ListBox {
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22,
                BorderStyle = BorderStyle.None,
                BackColor = BackColor,
                ForeColor = ForeColor,
                IntegralHeight = false
            };
            foreach (object item in items) list.Items.Add(item);
            list.SelectedIndex = selectedIndex;
            list.DrawItem += (s, e) => {
                if (e.Index < 0) return;
                bool selected = (e.State & DrawItemState.Selected) != 0;
                Color background = selected
                    ? (darkTheme ? Color.FromArgb(70, 91, 115) : SystemColors.Highlight)
                    : BackColor;
                Color foreground = selected && !darkTheme ? SystemColors.HighlightText : ForeColor;
                using (var brush = new SolidBrush(background))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, Convert.ToString(list.Items[e.Index]), Font,
                    new Rectangle(e.Bounds.X + 5, e.Bounds.Y, e.Bounds.Width - 9, e.Bounds.Height),
                    foreground, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix);
            };
            int height = Math.Min(items.Count * list.ItemHeight, 220);
            list.Size = new Size(Width, height);
            var host = new ToolStripControlHost(list) {
                AutoSize = false, Margin = Padding.Empty, Padding = Padding.Empty,
                Size = list.Size
            };
            var popup = new ToolStripDropDown {
                AutoSize = false, Padding = Padding.Empty, Margin = Padding.Empty,
                BackColor = BackColor, Size = list.Size, DropShadowEnabled = false
            };
            popup.Items.Add(host);
            list.Click += (s, e) => {
                if (list.SelectedIndex >= 0) SelectedIndex = list.SelectedIndex;
                if (!popup.IsDisposed) popup.Close();
                Focus();
            };
            list.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter && list.SelectedIndex >= 0)
                {
                    SelectedIndex = list.SelectedIndex;
                    if (!popup.IsDisposed) popup.Close();
                    Focus();
                }
                else if (e.KeyCode == Keys.Escape && !popup.IsDisposed) popup.Close();
            };
            // Closing can occur while a ListBox click is still being dispatched.
            // Dispose after that event has returned, so its handler can safely finish.
            popup.Closed += (s, e) => {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke((Action)(() => popup.Dispose()));
                else popup.Dispose();
            };
            popup.Show(this, 0, Height);
            list.Focus();
        }
    }
}
