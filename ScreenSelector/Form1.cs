using System.Diagnostics;

namespace ScreenSelector
{
    public partial class Form1 : Form
    {
        private const int HotkeyId = 0x5343;
        private readonly bool _launchMinimized;
        private AppSettings _settings = new();
        private bool _loadingSettings;
        private bool _capturingShortcut;
        private bool _selectionOpen;
        private bool _allowExit;
        private bool _settingsLoaded;
        private bool _isInTrayMode;

        private static readonly LanguageOption[] Languages =
        {
            new("Otomatik algıla", "auto", string.Empty),
            new("Türkçe", "tr", "tr-TR"), new("İngilizce", "en", "en-US"),
            new("Almanca", "de", "de-DE"), new("Fransızca", "fr", "fr-FR"),
            new("İspanyolca", "es", "es-ES"), new("İtalyanca", "it", "it-IT"),
            new("Portekizce", "pt", "pt-BR"), new("Rusça", "ru", "ru-RU"),
            new("Arapça", "ar", "ar-SA"), new("Japonca", "ja", "ja-JP"),
            new("Korece", "ko", "ko-KR"), new("Çince (Basitleştirilmiş)", "zh", "zh-CN")
        };

        public Form1() : this(false)
        {
        }

        public Form1(bool launchMinimized)
        {
            _launchMinimized = launchMinimized;
            InitializeComponent();
            notifyIcon.Icon = SystemIcons.Application;
            toolTip.SetToolTip(btnSelectNow, "Atanmış kısayol ile aynı seçim ekranını açar.");
            toolTip.SetToolTip(btnSwapLanguages, "Kaynak ve hedef dili değiştir");
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            _loadingSettings = true;
            _settings = AppSettings.Load();
            cmbSourceLanguage.DataSource = Languages.ToArray();
            cmbSourceLanguage.DisplayMember = nameof(LanguageOption.Name);
            cmbTargetLanguage.DataSource = Languages.Where(language => language.Code != "auto").ToArray();
            cmbTargetLanguage.DisplayMember = nameof(LanguageOption.Name);
            SelectLanguage(cmbSourceLanguage, _settings.SourceLanguage);
            SelectLanguage(cmbTargetLanguage, _settings.TargetLanguage);
            chkStartWithWindows.Checked = _settings.StartWithWindows;
            chkStartMinimized.Checked = _settings.StartMinimized;
            txtAudDToken.Text = _settings.AudDToken;
            txtShortcut.Text = FormatShortcut(_settings.HotkeyModifiers, _settings.HotkeyKey);
            _loadingSettings = false;
            _settingsLoaded = true;

            RegisterCurrentHotkey(showError: true);
            if (_launchMinimized)
            {
                BeginInvoke(() => MinimizeToTray(showBalloon: false));
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // RegisterHotKey is tied to the native window handle. If WinForms ever
            // recreates that handle, immediately bind the configured shortcut again.
            if (_settingsLoaded && !_allowExit)
            {
                RegisterCurrentHotkey(showError: false);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WmHotkey && m.WParam.ToInt32() == HotkeyId)
            {
                BeginInvoke(StartSelection);
                return;
            }
            base.WndProc(ref m);
        }

        private bool RegisterCurrentHotkey(bool showError)
        {
            if (IsHandleCreated) NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            var registered = NativeMethods.RegisterHotKey(Handle, HotkeyId,
                _settings.HotkeyModifiers | HotkeyModifiers.NoRepeat, _settings.HotkeyKey);
            lblReady.Text = registered ? "Kısayol dinleniyor" : "Kısayol kullanılamıyor";
            lblReadyDot.ForeColor = registered ? Color.FromArgb(75, 220, 160) : Color.FromArgb(248, 104, 116);
            if (!registered && showError)
            {
                MessageBox.Show("Seçtiğiniz kısayol başka bir uygulama tarafından kullanılıyor. Lütfen farklı bir kısayol seçin.",
                    "Kısayol kaydedilemedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return registered;
        }

        private void StartSelection()
        {
            if (_selectionOpen) return;
            _selectionOpen = true;
            // Starting a capture also enters tray mode. Completing or cancelling
            // the selection must never bring the main window to the foreground.
            MinimizeToTray(showBalloon: false);
            try
            {
                using var selectionForm = new SelectionForm(_settings);
                selectionForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Alan seçme ekranı açılamadı.\n\n{ex.Message}", "ScreenSelector",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _selectionOpen = false;
            }
        }

        private void ShowMainWindow()
        {
            _isInTrayMode = false;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void SaveSettings()
        {
            if (!_loadingSettings) _settings.Save();
        }

        private static void SelectLanguage(ComboBox combo, string code)
        {
            for (var i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is LanguageOption option && option.Code == code)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private void btnChangeShortcut_Click(object? sender, EventArgs e)
        {
            _capturingShortcut = true;
            txtShortcut.Text = "Yeni kısayola basın…";
            txtShortcut.BackColor = Color.FromArgb(255, 250, 224);
            lblHotkeyState.Text = "Tek bir tuşa veya istediğiniz tuş birleşimine basın. Esc: iptal";
            btnChangeShortcut.Text = "Dinliyor…";
            Focus();
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_capturingShortcut) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (e.KeyCode == Keys.Escape) { EndShortcutCapture(); return; }
            if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin) return;

            var modifiers = HotkeyModifiers.None;
            if (e.Control) modifiers |= HotkeyModifiers.Control;
            if (e.Shift) modifiers |= HotkeyModifiers.Shift;
            if (e.Alt) modifiers |= HotkeyModifiers.Alt;
            var previousKey = _settings.HotkeyKey;
            var previousModifiers = _settings.HotkeyModifiers;
            _settings.HotkeyKey = e.KeyCode;
            _settings.HotkeyModifiers = modifiers;
            if (!RegisterCurrentHotkey(showError: false))
            {
                _settings.HotkeyKey = previousKey;
                _settings.HotkeyModifiers = previousModifiers;
                RegisterCurrentHotkey(showError: false);
                lblHotkeyState.Text = "Bu kısayol kullanımda; başka bir tuş birleşimi deneyin.";
                return;
            }
            SaveSettings();
            EndShortcutCapture();
        }

        private void EndShortcutCapture()
        {
            _capturingShortcut = false;
            txtShortcut.Text = FormatShortcut(_settings.HotkeyModifiers, _settings.HotkeyKey);
            txtShortcut.BackColor = Color.FromArgb(247, 248, 252);
            lblHotkeyState.Text = "Tek tuş veya tuş birleşimi atayabilirsiniz. Örnek: Pause";
            btnChangeShortcut.Text = "Kısayolu değiştir";
        }

        private static string FormatShortcut(HotkeyModifiers modifiers, Keys key)
        {
            var parts = new List<string>();
            if (modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
            if (modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
            if (modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
            if (modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
            parts.Add(key == Keys.Space ? "Space" : key.ToString());
            return string.Join(" + ", parts);
        }

        private void language_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_loadingSettings) return;
            if (cmbSourceLanguage.SelectedItem is LanguageOption source) _settings.SourceLanguage = source.Code;
            if (cmbTargetLanguage.SelectedItem is LanguageOption target) _settings.TargetLanguage = target.Code;
            SaveSettings();
        }

        private void btnSwapLanguages_Click(object? sender, EventArgs e)
        {
            if (cmbSourceLanguage.SelectedItem is not LanguageOption source ||
                cmbTargetLanguage.SelectedItem is not LanguageOption target)
                return;

            if (source.Code == "auto")
            {
                lblTranslationHint.Text = "Dilleri değiştirmek için önce kaynak dili açıkça seçin.";
                return;
            }

            SelectLanguage(cmbSourceLanguage, target.Code);
            SelectLanguage(cmbTargetLanguage, source.Code);
            lblTranslationHint.Text = "Google Translate kaynak dili otomatik olarak da algılayabilir.";
        }

        private void startup_CheckedChanged(object? sender, EventArgs e)
        {
            if (_loadingSettings) return;
            _settings.StartWithWindows = chkStartWithWindows.Checked;
            _settings.StartMinimized = chkStartMinimized.Checked;
            try
            {
                StartupManager.SetEnabled(_settings.StartWithWindows, _settings.StartMinimized);
                SaveSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Windows başlangıç ayarı değiştirilemedi.\n\n{ex.Message}",
                    "Başlangıç ayarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void txtAudDToken_TextChanged(object? sender, EventArgs e)
        {
            if (_loadingSettings) return;
            _settings.AudDToken = txtAudDToken.Text.Trim();
            SaveSettings();
        }

        private void linkAudD_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
            Process.Start(new ProcessStartInfo("https://audd.io/") { UseShellExecute = true });

        private void ScrollTo(Control control, string title)
        {
            contentFlow.ScrollControlIntoView(control);
            lblHeaderTitle.Text = title;
        }

        private void btnNavHome_Click(object? sender, EventArgs e) => ScrollTo(cardIntro, "Genel bakış");
        private void btnNavSettings_Click(object? sender, EventArgs e) => ScrollTo(cardHotkey, "Ayarlar");
        private void btnNavTranslation_Click(object? sender, EventArgs e) => ScrollTo(cardTranslation, "Çeviri");
        private void btnNavMusic_Click(object? sender, EventArgs e) => ScrollTo(cardMusic, "Şarkı tanıma");
        private void btnSelectNow_Click(object? sender, EventArgs e) => BeginInvoke(StartSelection);
        private void menuSelect_Click(object? sender, EventArgs e) => BeginInvoke(StartSelection);
        private void btnHeaderMinimize_Click(object? sender, EventArgs e) => MinimizeToTray();
        private void notifyIcon_DoubleClick(object? sender, EventArgs e) => ShowMainWindow();
        private void menuOpen_Click(object? sender, EventArgs e) => ShowMainWindow();

        private void MinimizeToTray(bool showBalloon = true)
        {
            var wasAlreadyInTray = _isInTrayMode;
            _isInTrayMode = true;
            Hide();
            // Hide() already removes the window from the taskbar. Keeping
            // ShowInTaskbar unchanged prevents handle recreation, which would
            // invalidate the global hotkey registration.
            if (showBalloon && !wasAlreadyInTray)
            {
                notifyIcon.ShowBalloonTip(1500, "ScreenSelector hazır",
                    $"{FormatShortcut(_settings.HotkeyModifiers, _settings.HotkeyKey)} ile alan seçebilirsiniz.",
                    ToolTipIcon.Info);
            }
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) MinimizeToTray();
        }

        private void menuExit_Click(object? sender, EventArgs e) { _allowExit = true; Close(); }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToTray();
                return;
            }
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            notifyIcon.Visible = false;
        }
    }

    public sealed record LanguageOption(string Name, string Code, string OcrTag)
    {
        public override string ToString() => Name;

        public static string GetOcrTag(string code) => LanguageLookup.TryGetValue(code, out var tag) ? tag : string.Empty;

        private static readonly IReadOnlyDictionary<string, string> LanguageLookup =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tr"] = "tr-TR", ["en"] = "en-US", ["de"] = "de-DE", ["fr"] = "fr-FR",
                ["es"] = "es-ES", ["it"] = "it-IT", ["pt"] = "pt-BR", ["ru"] = "ru-RU",
                ["ar"] = "ar-SA", ["ja"] = "ja-JP", ["ko"] = "ko-KR", ["zh"] = "zh-CN"
            };
    }
}
