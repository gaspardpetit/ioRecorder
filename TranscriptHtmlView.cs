using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ioRecord
{
    internal sealed class TranscriptHtmlView : UserControl
    {
        private readonly WebView2 browser;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        private string latestSnapshot;
        private string latestStatus;
        private bool darkTheme;
        private bool pageReady;

        public TranscriptHtmlView()
        {
            browser = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(browser);
            browser.NavigationCompleted += (sender, args) => {
                if (!args.IsSuccess) return;
                pageReady = true;
                SendTheme();
                SendStatus();
                SendSnapshot();
            };
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                await browser.EnsureCoreWebView2Async();
                browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                string page = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TranscriptPage.html");
                browser.NavigateToString(File.ReadAllText(page));
            }
            catch (Exception ex)
            {
                Controls.Clear();
                Controls.Add(new Label {
                    Dock = DockStyle.Fill,
                    Text = "Transcript view unavailable: " + ex.Message,
                    TextAlign = ContentAlignment.MiddleCenter
                });
            }
        }

        public void Clear()
        {
            latestSnapshot = null;
            latestStatus = null;
            Send(new { type = "clear" });
        }

        public void SetSnapshot(string json)
        {
            latestSnapshot = json;
            SendSnapshot();
        }

        public void SetStatus(string status)
        {
            latestStatus = status;
            SendStatus();
        }

        public void ApplyTheme(bool dark)
        {
            darkTheme = dark;
            BackColor = dark ? Color.FromArgb(24, 26, 30) : Color.White;
            SendTheme();
        }

        private void SendSnapshot()
        {
            if (latestSnapshot != null && pageReady)
                browser.CoreWebView2.PostWebMessageAsJson(latestSnapshot);
        }

        private void SendStatus()
        {
            Send(new { type = "status", text = latestStatus ?? "" });
        }

        private void SendTheme()
        {
            Send(new { type = "theme", dark = darkTheme });
        }

        private void Send(object message)
        {
            if (pageReady && browser.CoreWebView2 != null)
                browser.CoreWebView2.PostWebMessageAsJson(serializer.Serialize(message));
        }
    }
}
