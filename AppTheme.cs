using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ioRecord
{
    public partial class ioRecorderForm
    {
        private const int WmSettingChange = 0x001A;
        private bool darkTheme;
        private bool themeApplied;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute,
            ref int value, int size);

        private static bool SystemUsesDarkApps()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object value = key == null ? null : key.GetValue("AppsUseLightTheme");
                return value is int && (int)value == 0;
            }
        }

        private void SystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke((Action)ApplySystemTheme);
        }

        private void ApplySystemTheme()
        {
            bool currentTheme = SystemUsesDarkApps();
            if (themeApplied && currentTheme == darkTheme) return;
            darkTheme = currentTheme;
            themeApplied = true;
            Color surface = darkTheme ? Color.FromArgb(31, 33, 38) : SystemColors.Control;
            Color field = darkTheme ? Color.FromArgb(45, 48, 54) : SystemColors.Window;
            Color foreground = darkTheme ? Color.FromArgb(232, 235, 240) : SystemColors.ControlText;
            Color border = darkTheme ? Color.FromArgb(57, 61, 68) : SystemColors.Control;
            BackColor = surface;
            ForeColor = foreground;
            foreach (Control control in Controls)
                ApplyControlTheme(control, surface, field, foreground, border);
            transcriptText.ApplyTheme(darkTheme);
            transcriptGroup.BorderColor = darkTheme ? Color.FromArgb(67, 72, 80) : SystemColors.ControlDark;
            SelectSessionTab(playbackPage.Visible);
            ApplyTitleBarTheme();
        }

        private void ApplyControlTheme(Control control, Color surface, Color field,
            Color foreground, Color border)
        {
            if (control == transcriptText) return;
            control.ForeColor = foreground;
            if (control is ThemedTextBox)
            {
                ((ThemedTextBox)control).ApplyTheme(darkTheme);
            }
            else if (control is ThemedComboBox)
            {
                ((ThemedComboBox)control).ApplyTheme(darkTheme);
            }
            else if (control is TextBox || control is ComboBox)
            {
                control.BackColor = field;
                if (control is ComboBox)
                    ((ComboBox)control).FlatStyle = darkTheme ? FlatStyle.Flat : FlatStyle.Standard;
            }
            else if (control is ThemedButton)
            {
                ((ThemedButton)control).ApplyTheme(darkTheme);
            }
            else if (control is ThemedCheckBox)
            {
                ((ThemedCheckBox)control).ApplyTheme(darkTheme);
            }
            else if (control is Button)
            {
                var button = (Button)control;
                button.UseVisualStyleBackColor = !darkTheme;
                button.FlatStyle = darkTheme ? FlatStyle.Flat : FlatStyle.Standard;
                button.BackColor = darkTheme ? field : surface;
                button.FlatAppearance.BorderColor = border;
                button.FlatAppearance.MouseOverBackColor = darkTheme
                    ? Color.FromArgb(64, 68, 76) : SystemColors.Control;
            }
            else if (control is AudioLevelBar)
            {
                control.BackColor = field;
                control.ForeColor = darkTheme ? Color.FromArgb(94, 170, 210) : SystemColors.Highlight;
            }
            else
            {
                control.BackColor = surface;
            }
            foreach (Control child in control.Controls)
                ApplyControlTheme(child, surface, field, foreground, border);
        }

        private void ApplyTitleBarTheme()
        {
            if (!IsHandleCreated) return;
            int enabled = darkTheme ? 1 : 0;
            try
            {
                if (DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int)) != 0)
                    DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyTitleBarTheme();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemThemeChanged;
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == WmSettingChange && IsHandleCreated)
                BeginInvoke((Action)ApplySystemTheme);
        }
    }
}
