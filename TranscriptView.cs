using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace ioRecord
{
    internal sealed class TranscriptItem
    {
        public double Start;
        public string Speaker;
        public string Text;
        public bool Tentative;
    }

    internal sealed class TranscriptView : ScrollableControl
    {
        private sealed class Row
        {
            public Rectangle Bounds;
            public Rectangle TextBounds;
            public string Text;
            public Block Block;
            public bool Timestamp;
            public bool Status;
            public bool Tentative;
        }

        private sealed class Block
        {
            public Rectangle Bounds;
            public Rectangle Badge;
            public string Speaker;
            public Color Accent;
            public bool AllTentative = true;
        }

        private readonly List<Row> rows = new List<Row>();
        private readonly List<Block> blocks = new List<Block>();
        private List<TranscriptItem> items = new List<TranscriptItem>();
        private readonly Font textFont;
        private readonly Font tentativeFont;
        private readonly Font badgeFont;
        private readonly Font timestampFont;
        private readonly Color[] speakerColors = {
            Color.FromArgb(29, 107, 127), Color.FromArgb(116, 72, 151),
            Color.FromArgb(166, 83, 47), Color.FromArgb(47, 120, 86),
            Color.FromArgb(156, 65, 111), Color.FromArgb(64, 92, 165)
        };
        private bool darkTheme;
        private Color mutedColor = Color.Gray;
        private Color timestampColor = Color.FromArgb(104, 112, 122);
        private int selectionAnchor = -1;
        private int selectionEnd = -1;
        private bool selecting;
        private string statusText;

        public TranscriptView()
        {
            AutoScroll = true;
            DoubleBuffered = true;
            TabStop = true;
            BackColor = Color.White;
            textFont = new Font("Segoe UI", 10f);
            tentativeFont = new Font(textFont, FontStyle.Italic);
            badgeFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            timestampFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            var menu = new ContextMenuStrip();
            menu.Items.Add("Copy selection", null, (s, e) => CopySelection());
            menu.Items.Add("Copy transcript", null, (s, e) => CopyTranscript());
            ContextMenuStrip = menu;
        }

        public void SetItems(List<TranscriptItem> updated)
        {
            items = updated ?? new List<TranscriptItem>();
            LayoutRows();
            if (selectionAnchor >= rows.Count || selectionEnd >= rows.Count)
                selectionAnchor = selectionEnd = -1;
        }

        public void SetStatus(string status)
        {
            if (statusText == status) return;
            statusText = status;
            LayoutRows();
        }

        public void ApplyTheme(bool dark)
        {
            darkTheme = dark;
            BackColor = dark ? Color.FromArgb(24, 26, 30) : Color.White;
            ForeColor = dark ? Color.FromArgb(232, 235, 240) : Color.FromArgb(35, 39, 44);
            mutedColor = dark ? Color.FromArgb(152, 159, 170) : Color.Gray;
            timestampColor = dark ? Color.FromArgb(166, 174, 185) : Color.FromArgb(104, 112, 122);
            if (ContextMenuStrip != null)
            {
                ContextMenuStrip.BackColor = dark ? Color.FromArgb(42, 45, 51) : SystemColors.Menu;
                ContextMenuStrip.ForeColor = ForeColor;
            }
            Invalidate();
        }

        private void CopyTranscript()
        {
            var text = new StringBuilder();
            foreach (var item in items)
                text.AppendFormat("[{0:hh\\:mm\\:ss}] {1}: {2}\r\n", TimeSpan.FromSeconds(item.Start),
                    item.Speaker, item.Text);
            if (text.Length > 0) Clipboard.SetText(text.ToString());
        }

        private void CopySelection()
        {
            if (selectionAnchor < 0 || selectionEnd < 0) return;
            var text = new StringBuilder();
            int first = Math.Min(selectionAnchor, selectionEnd);
            int last = Math.Max(selectionAnchor, selectionEnd);
            for (int i = first; i <= last; i++)
            {
                if (rows[i].Timestamp || rows[i].Status) continue;
                if (text.Length > 0) text.AppendLine();
                text.Append(rows[i].Text);
            }
            if (text.Length > 0) Clipboard.SetText(text.ToString());
        }

        private int RowAt(Point point)
        {
            int y = point.Y - AutoScrollPosition.Y;
            for (int i = 0; i < rows.Count; i++)
                if (!rows[i].Timestamp && !rows[i].Status && rows[i].Bounds.Contains(point.X, y)) return i;
            return -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            selectionAnchor = selectionEnd = RowAt(e.Location);
            selecting = selectionAnchor >= 0;
            if (selecting) Capture = true;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!selecting) return;
            int row = RowAt(e.Location);
            if (row >= 0 && row != selectionEnd)
            {
                selectionEnd = row;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) selecting = false;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.A) || keyData == (Keys.Control | Keys.C)) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.C)
            {
                if (selectionAnchor >= 0) CopySelection();
                else CopyTranscript();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                selectionAnchor = 0;
                selectionEnd = rows.Count - 1;
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                selectionAnchor = selectionEnd = -1;
                Invalidate();
            }
        }

        private Color SpeakerColor(string speaker)
        {
            int hash = 0;
            foreach (char c in speaker ?? "") hash = (hash * 31 + c) & 0x7fffffff;
            return speakerColors[hash % speakerColors.Length];
        }

        private static Color Tint(Color color, double white)
        {
            return Color.FromArgb((int)(color.R * (1 - white) + 255 * white),
                (int)(color.G * (1 - white) + 255 * white),
                (int)(color.B * (1 - white) + 255 * white));
        }

        private void LayoutRows()
        {
            int previousY = -AutoScrollPosition.Y;
            int previousMaximum = Math.Max(0, AutoScrollMinSize.Height - ClientSize.Height);
            bool followEnd = previousY >= previousMaximum - 24;
            rows.Clear();
            blocks.Clear();
            int width = Math.Max(180, ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
            int y = 4;
            double lastTimestamp = double.NegativeInfinity;
            Block block = null;
            using (Graphics graphics = CreateGraphics())
            {
                foreach (var item in items)
                {
                    if (item.Start - lastTimestamp >= 60)
                    {
                        if (block != null) y += 10;
                        block = null;
                        rows.Add(new Row { Timestamp = true, Text = TimeSpan.FromSeconds(item.Start).ToString(@"hh\:mm\:ss"),
                            Bounds = new Rectangle(6, y, width - 12, 22) });
                        y += 24;
                        lastTimestamp = item.Start;
                    }
                    string speaker = string.IsNullOrEmpty(item.Speaker) ? "Speaker" : item.Speaker;
                    if (block == null || block.Speaker != speaker)
                    {
                        if (block != null) y += 10;
                        int badgeWidth = Math.Min(150, (int)Math.Ceiling(graphics.MeasureString(speaker, badgeFont).Width) + 14);
                        block = new Block {
                            Bounds = new Rectangle(6, y, width - 8, 0),
                            Badge = new Rectangle(12, y + 3, badgeWidth, 20),
                            Speaker = speaker, Accent = SpeakerColor(speaker)
                        };
                        blocks.Add(block);
                    }
                    int textX = 12 + block.Badge.Width + 10;
                    int textWidth = Math.Max(80, width - textX - 10);
                    Font font = item.Tentative ? tentativeFont : textFont;
                    int textHeight = (int)Math.Ceiling(graphics.MeasureString(item.Text ?? "", font, textWidth).Height);
                    int height = Math.Max(18, textHeight);
                    rows.Add(new Row {
                        Bounds = new Rectangle(6, y, width - 8, height),
                        TextBounds = new Rectangle(textX, y, textWidth, height),
                        Block = block, Text = item.Text ?? "", Tentative = item.Tentative
                    });
                    block.AllTentative &= item.Tentative;
                    y += height;
                    block.Bounds = new Rectangle(block.Bounds.X, block.Bounds.Y, block.Bounds.Width,
                        y - block.Bounds.Y);
                }
                if (!string.IsNullOrEmpty(statusText))
                {
                    y += items.Count > 0 ? 10 : 8;
                    int capsuleWidth = Math.Min(width - 24,
                        (int)Math.Ceiling(graphics.MeasureString(statusText, badgeFont).Width) + 28);
                    rows.Add(new Row { Status = true, Text = statusText,
                        Bounds = new Rectangle(12, y, capsuleWidth, 26) });
                    y += 34;
                }
            }
            AutoScrollMinSize = new Size(0, y + 4);
            int maximum = Math.Max(0, AutoScrollMinSize.Height - ClientSize.Height);
            AutoScrollPosition = new Point(0, Math.Min(followEnd ? maximum : previousY, maximum));
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (textFont != null) LayoutRows();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TranslateTransform(0, AutoScrollPosition.Y);
            if (rows.Count == 0)
            {
                using (var brush = new SolidBrush(mutedColor))
                    e.Graphics.DrawString("Transcript will appear here.", textFont, brush, 20, 20);
                return;
            }
            int top = -AutoScrollPosition.Y;
            int bottom = top + ClientSize.Height;
            foreach (var block in blocks)
            {
                if (block.Bounds.Bottom < top || block.Bounds.Top > bottom) continue;
                using (var background = new SolidBrush(darkTheme
                    ? Color.FromArgb(block.AllTentative ? 40 : 53, block.Accent)
                    : Tint(block.Accent, block.AllTentative ? 0.95 : 0.91)))
                    e.Graphics.FillRoundedRectangle(background, block.Bounds, 6);
                using (var badge = new SolidBrush(block.Accent))
                    e.Graphics.FillRoundedRectangle(badge, block.Badge, 10);
                using (var brush = new SolidBrush(Color.White))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                    e.Graphics.DrawString(block.Speaker, badgeFont, brush, block.Badge, format);
            }
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Bounds.Bottom < top || row.Bounds.Top > bottom) continue;
                if (row.Timestamp)
                {
                    using (var brush = new SolidBrush(timestampColor))
                    using (var format = new StringFormat { Alignment = StringAlignment.Center })
                        e.Graphics.DrawString(row.Text, timestampFont, brush, row.Bounds, format);
                    continue;
                }
                if (row.Status)
                {
                    using (var background = new SolidBrush(darkTheme
                        ? Color.FromArgb(53, 70, 88) : Color.FromArgb(226, 236, 246)))
                        e.Graphics.FillRoundedRectangle(background, row.Bounds, 12);
                    using (var brush = new SolidBrush(darkTheme
                        ? Color.FromArgb(203, 221, 241) : Color.FromArgb(49, 77, 106)))
                    using (var format = new StringFormat { Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter,
                        FormatFlags = StringFormatFlags.NoWrap })
                        e.Graphics.DrawString(row.Text, badgeFont, brush, row.Bounds, format);
                    continue;
                }
                if (selectionAnchor >= 0 && selectionEnd >= 0 &&
                    i >= Math.Min(selectionAnchor, selectionEnd) && i <= Math.Max(selectionAnchor, selectionEnd))
                {
                    using (var selection = new SolidBrush(darkTheme
                        ? Color.FromArgb(80, 90, 145, 220) : Color.FromArgb(95, 91, 155, 225)))
                        e.Graphics.FillRectangle(selection, row.TextBounds);
                }
                using (var brush = new SolidBrush(darkTheme
                    ? (row.Tentative ? Tint(row.Block.Accent, 0.52) : Tint(row.Block.Accent, 0.72))
                    : (row.Tentative ? Tint(row.Block.Accent, 0.35) : row.Block.Accent)))
                    e.Graphics.DrawString(row.Text, row.Tentative ? tentativeFont : textFont, brush, row.TextBounds);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                textFont.Dispose();
                tentativeFont.Dispose();
                badgeFont.Dispose();
                timestampFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal static class RoundedGraphics
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            using (var path = new GraphicsPath())
            {
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                graphics.FillPath(brush, path);
            }
        }
    }
}
