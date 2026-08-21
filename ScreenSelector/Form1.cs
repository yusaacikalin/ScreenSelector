using System.Diagnostics;

namespace ScreenSelector
{
    public partial class Form1 : Form
    {
        private const int HotkeyId = 0x5343;
        private readonly bool _launchMinimized;
        private readonly Icon _applicationIcon;
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
            _applicationIcon = LoadApplicationIcon();
            Icon = _applicationIcon;
            notifyIcon.Icon = _applicationIcon;
            ModernWindowBehavior.EnableDragging(this, panelHeader);
            ApplyInterfaceLayout();
            toolTip.SetToolTip(btnSelectNow, "Atanmış kısayol ile aynı seçim ekranını açar.");
            toolTip.SetToolTip(btnSwapLanguages, "Kaynak ve hedef dili değiştir");
        }

        private static Icon LoadApplicationIcon()
        {
            try
            {
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                    ?? (Icon)SystemIcons.Application.Clone();
            }
            catch
            {
                return (Icon)SystemIcons.Application.Clone();
            }
        }

        private void ApplyInterfaceLayout()
        {
            var sidebarText = Color.FromArgb(174, 181, 201);
            var sidebarBackground = Color.FromArgb(20, 24, 38);

            ConfigureNavButton(btnNavHome, "⌂  Genel bakış", 126, Color.White, Color.FromArgb(45, 51, 77));
            ConfigureNavButton(btnNavSettings, "⚙  Ayarlar", 178, sidebarText, sidebarBackground);
            ConfigureNavButton(btnNavTranslation, "文  Çeviri", 230, sidebarText, sidebarBackground);
            ConfigureNavButton(btnNavMusic, "♫  Şarkı tanıma", 282, sidebarText, sidebarBackground);

            StyleCardTitle(lblHotkeyTitle, "Seçim kısayolu", 28, 20);
            StyleFieldLabel(lblShortcutLabel, "Etkin kısayol", 345, 65);

            StyleCardTitle(lblFeaturesTitle, "Tek seçim, üç işlem", 28, 20);
            StyleDescription(lblFeaturesDescription,
                "Seçtiğiniz alanı ihtiyacınıza göre anında işleyin.", 28, 53);
            ConfigureFeature(featureText, lblFeatureTextIcon, lblFeatureTextTitle,
                lblFeatureTextDescription, 28, "T", "Metni çıkar",
                "Ekrandaki yazıyı seçip panoya kopyalayın.", Color.FromArgb(91, 76, 230));
            ConfigureFeature(featureTranslate, lblFeatureTranslateIcon, lblFeatureTranslateTitle,
                lblFeatureTranslateDescription, 241, "文", "Çevir",
                "Seçili metni tercih ettiğiniz dile çevirin.", Color.FromArgb(31, 154, 132));
            ConfigureFeature(featureMusic, lblFeatureMusicIcon, lblFeatureMusicTitle,
                lblFeatureMusicDescription, 454, "♫", "Şarkıyı bul",
                "Bilgisayarınızda çalan parçayı tanıyın.", Color.FromArgb(232, 117, 64));

            StyleCardTitle(lblTranslationTitle, "Çeviri ayarları", 28, 20);
            StyleDescription(lblTranslationDescription,
                "Metin çevirilerinde kullanılacak dilleri belirleyin.", 28, 53);
            StyleFieldLabel(lblSourceLanguage, "Kaynak dil", 28, 87);
            StyleFieldLabel(lblTargetLanguage, "Hedef dil", 387, 87);
            ConfigureLanguageCombo(cmbSourceLanguage, 28);
            ConfigureLanguageCombo(cmbTargetLanguage, 387);

            StyleCardTitle(lblStartupTitle, "Başlangıç davranışı", 28, 20);
            StyleDescription(lblStartupDescription,
                "ScreenSelector'ın Windows ile nasıl başlayacağını seçin.", 28, 53);
            ConfigureCheck(chkStartWithWindows, "Windows ile başlat", 28);
            ConfigureCheck(chkStartMinimized, "Bildirim alanında başlat", 340);

            StyleCardTitle(lblMusicTitle, "Şarkı tanıma", 28, 20);
            StyleDescription(lblMusicDescription,
                "AudD erişim anahtarınızı girerek bilgisayarınızda çalan parçayı bulun.", 28, 53);
            StyleFieldLabel(lblToken, "AudD API anahtarı", 28, 91);
        }

        private static void ConfigureNavButton(Button button, string text, int top, Color foreColor, Color backColor)
        {
            button.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            button.BackColor = backColor;
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(48, 54, 76);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 41, 61);
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Segoe UI Semibold", 10F);
            button.ForeColor = foreColor;
            button.Location = new Point(18, top);
            button.Padding = new Padding(9, 0, 0, 0);
            button.Size = new Size(190, 44);
            button.Text = text;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.UseVisualStyleBackColor = false;
        }

        private static void StyleCardTitle(Label label, string text, int left, int top)
        {
            label.AutoSize = true;
            label.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(29, 35, 54);
            label.Location = new Point(left, top);
            label.Text = text;
        }

        private static void StyleDescription(Label label, string text, int left, int top)
        {
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9.5F);
            label.ForeColor = Color.FromArgb(94, 103, 124);
            label.Location = new Point(left, top);
            label.Text = text;
        }

        private static void StyleFieldLabel(Label label, string text, int left, int top)
        {
            label.AutoSize = true;
            label.Font = new Font("Segoe UI Semibold", 9F);
            label.ForeColor = Color.FromArgb(75, 84, 106);
            label.Location = new Point(left, top);
            label.Text = text;
        }

        private static void ConfigureFeature(Panel panel, Label icon, Label title, Label description,
            int left, string iconText, string titleText, string descriptionText, Color accent)
        {
            panel.BackColor = Color.FromArgb(246, 248, 252);
            panel.Controls.Add(description);
            panel.Controls.Add(title);
            panel.Controls.Add(icon);
            panel.Location = new Point(left, 86);
            panel.Size = new Size(199, 112);

            icon.AutoSize = true;
            icon.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            icon.ForeColor = accent;
            icon.Location = new Point(14, 17);
            icon.Text = iconText;

            title.AutoSize = true;
            title.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(35, 42, 62);
            title.Location = new Point(52, 27);
            title.Text = titleText;

            description.Font = new Font("Segoe UI", 8.5F);
            description.ForeColor = Color.FromArgb(101, 111, 133);
            description.Location = new Point(16, 68);
            description.Size = new Size(169, 34);
            description.Text = descriptionText;
        }

        private void ConfigureLanguageCombo(ComboBox combo, int left)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Font = new Font("Segoe UI", 10F);
            combo.Location = new Point(left, 110);
            combo.Size = new Size(267, 25);
            combo.SelectedIndexChanged += language_SelectedIndexChanged;
        }

        private void ConfigureCheck(CheckBox check, string text, int left)
        {
            check.AutoSize = true;
            check.Font = new Font("Segoe UI", 9.5F);
            check.ForeColor = Color.FromArgb(56, 64, 84);
            check.Location = new Point(left, 102);
            check.Text = text;
            check.UseVisualStyleBackColor = true;
            check.CheckedChanged += startup_CheckedChanged;
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _applicationIcon.Dispose();
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
