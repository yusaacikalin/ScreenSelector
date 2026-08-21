namespace ScreenSelector
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panelSidebar = new Panel();
            lblVersion = new Label();
            panelReady = new Panel();
            lblReady = new Label();
            lblReadyDot = new Label();
            btnNavMusic = new Button();
            btnNavTranslation = new Button();
            btnNavSettings = new Button();
            btnNavHome = new Button();
            lblBrandSubtitle = new Label();
            lblBrand = new Label();
            panelMain = new Panel();
            contentFlow = new FlowLayoutPanel();
            cardIntro = new Panel();
            btnSelectNow = new Button();
            lblIntroText = new Label();
            lblIntroTitle = new Label();
            cardHotkey = new Panel();
            lblHotkeyState = new Label();
            btnChangeShortcut = new Button();
            txtShortcut = new TextBox();
            lblShortcutLabel = new Label();
            lblHotkeyDescription = new Label();
            lblHotkeyTitle = new Label();
            cardFeatures = new Panel();
            featureMusic = new Panel();
            featureTranslate = new Panel();
            featureText = new Panel();
            lblFeaturesDescription = new Label();
            lblFeaturesTitle = new Label();
            cardTranslation = new Panel();
            lblTranslationHint = new Label();
            btnSwapLanguages = new Button();
            cmbTargetLanguage = new ComboBox();
            lblTargetLanguage = new Label();
            cmbSourceLanguage = new ComboBox();
            lblSourceLanguage = new Label();
            lblTranslationDescription = new Label();
            lblTranslationTitle = new Label();
            cardStartup = new Panel();
            chkStartMinimized = new CheckBox();
            chkStartWithWindows = new CheckBox();
            lblStartupDescription = new Label();
            lblStartupTitle = new Label();
            cardMusic = new Panel();
            linkAudD = new LinkLabel();
            lblTokenHint = new Label();
            txtAudDToken = new TextBox();
            lblToken = new Label();
            lblMusicDescription = new Label();
            lblMusicTitle = new Label();
            lblFooter = new Label();
            panelHeader = new Panel();
            btnHeaderMinimize = new Button();
            lblHeaderHint = new Label();
            lblHeaderTitle = new Label();
            lblFeatureMusicDescription = new Label();
            lblFeatureMusicTitle = new Label();
            lblFeatureMusicIcon = new Label();
            lblFeatureTranslateDescription = new Label();
            lblFeatureTranslateTitle = new Label();
            lblFeatureTranslateIcon = new Label();
            lblFeatureTextDescription = new Label();
            lblFeatureTextTitle = new Label();
            lblFeatureTextIcon = new Label();
            notifyIcon = new NotifyIcon(components);
            trayMenu = new ContextMenuStrip(components);
            menuOpen = new ToolStripMenuItem();
            menuSelect = new ToolStripMenuItem();
            menuSeparator = new ToolStripSeparator();
            menuExit = new ToolStripMenuItem();
            toolTip = new ToolTip(components);
            panelSidebar.SuspendLayout();
            panelReady.SuspendLayout();
            panelMain.SuspendLayout();
            contentFlow.SuspendLayout();
            cardIntro.SuspendLayout();
            cardHotkey.SuspendLayout();
            cardFeatures.SuspendLayout();
            cardTranslation.SuspendLayout();
            cardStartup.SuspendLayout();
            cardMusic.SuspendLayout();
            panelHeader.SuspendLayout();
            trayMenu.SuspendLayout();
            SuspendLayout();
            // 
            // panelSidebar
            // 
            panelSidebar.BackColor = Color.FromArgb(20, 24, 38);
            panelSidebar.Controls.Add(lblVersion);
            panelSidebar.Controls.Add(panelReady);
            panelSidebar.Controls.Add(btnNavMusic);
            panelSidebar.Controls.Add(btnNavTranslation);
            panelSidebar.Controls.Add(btnNavSettings);
            panelSidebar.Controls.Add(btnNavHome);
            panelSidebar.Controls.Add(lblBrandSubtitle);
            panelSidebar.Controls.Add(lblBrand);
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Location = new Point(0, 0);
            panelSidebar.Name = "panelSidebar";
            panelSidebar.Padding = new Padding(18);
            panelSidebar.Size = new Size(226, 741);
            panelSidebar.TabIndex = 2;
            // 
            // lblVersion
            // 
            lblVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Segoe UI", 8.5F);
            lblVersion.ForeColor = Color.FromArgb(112, 119, 143);
            lblVersion.Location = new Point(25, 703);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(102, 15);
            lblVersion.TabIndex = 0;
            lblVersion.Text = "ScreenSelector 1.0";
            // 
            // panelReady
            // 
            panelReady.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelReady.BackColor = Color.FromArgb(28, 34, 51);
            panelReady.Controls.Add(lblReady);
            panelReady.Controls.Add(lblReadyDot);
            panelReady.Location = new Point(18, 644);
            panelReady.Name = "panelReady";
            panelReady.Size = new Size(190, 43);
            panelReady.TabIndex = 1;
            // 
            // lblReady
            // 
            lblReady.AutoSize = true;
            lblReady.Font = new Font("Segoe UI Semibold", 9F);
            lblReady.ForeColor = Color.FromArgb(216, 221, 235);
            lblReady.Location = new Point(36, 14);
            lblReady.Name = "lblReady";
            lblReady.Size = new Size(100, 15);
            lblReady.TabIndex = 0;
            lblReady.Text = "Kısayol dinleniyor";
            // 
            // lblReadyDot
            // 
            lblReadyDot.AutoSize = true;
            lblReadyDot.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblReadyDot.ForeColor = Color.FromArgb(75, 220, 160);
            lblReadyDot.Location = new Point(14, 9);
            lblReadyDot.Name = "lblReadyDot";
            lblReadyDot.Size = new Size(17, 21);
            lblReadyDot.TabIndex = 1;
            lblReadyDot.Text = "•";
            // 
            // btnNavMusic
            // 
            btnNavMusic.Location = new Point(0, 0);
            btnNavMusic.Name = "btnNavMusic";
            btnNavMusic.Size = new Size(75, 23);
            btnNavMusic.TabIndex = 2;
            btnNavMusic.Click += btnNavMusic_Click;
            // 
            // btnNavTranslation
            // 
            btnNavTranslation.Location = new Point(0, 0);
            btnNavTranslation.Name = "btnNavTranslation";
            btnNavTranslation.Size = new Size(75, 23);
            btnNavTranslation.TabIndex = 3;
            btnNavTranslation.Click += btnNavTranslation_Click;
            // 
            // btnNavSettings
            // 
            btnNavSettings.Location = new Point(0, 0);
            btnNavSettings.Name = "btnNavSettings";
            btnNavSettings.Size = new Size(75, 23);
            btnNavSettings.TabIndex = 4;
            btnNavSettings.Click += btnNavSettings_Click;
            // 
            // btnNavHome
            // 
            btnNavHome.Location = new Point(0, 0);
            btnNavHome.Name = "btnNavHome";
            btnNavHome.Size = new Size(75, 23);
            btnNavHome.TabIndex = 5;
            btnNavHome.Click += btnNavHome_Click;
            // 
            // lblBrandSubtitle
            // 
            lblBrandSubtitle.AutoSize = true;
            lblBrandSubtitle.Font = new Font("Segoe UI", 8.5F);
            lblBrandSubtitle.ForeColor = Color.FromArgb(119, 127, 151);
            lblBrandSubtitle.Location = new Point(26, 79);
            lblBrandSubtitle.Name = "lblBrandSubtitle";
            lblBrandSubtitle.Size = new Size(121, 15);
            lblBrandSubtitle.TabIndex = 6;
            lblBrandSubtitle.Text = "Ekrandaki her şeyi seç";
            // 
            // lblBrand
            // 
            lblBrand.AutoSize = true;
            lblBrand.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            lblBrand.ForeColor = Color.White;
            lblBrand.Location = new Point(22, 36);
            lblBrand.Name = "lblBrand";
            lblBrand.Size = new Size(198, 37);
            lblBrand.TabIndex = 7;
            lblBrand.Text = "ScreenSelector";
            // 
            // panelMain
            // 
            panelMain.BackColor = Color.FromArgb(244, 246, 251);
            panelMain.Controls.Add(contentFlow);
            panelMain.Controls.Add(panelHeader);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(226, 0);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(758, 741);
            panelMain.TabIndex = 1;
            // 
            // contentFlow
            // 
            contentFlow.AutoScroll = true;
            contentFlow.BackColor = Color.FromArgb(244, 246, 251);
            contentFlow.Controls.Add(cardIntro);
            contentFlow.Controls.Add(cardHotkey);
            contentFlow.Controls.Add(cardFeatures);
            contentFlow.Controls.Add(cardTranslation);
            contentFlow.Controls.Add(cardStartup);
            contentFlow.Controls.Add(cardMusic);
            contentFlow.Controls.Add(lblFooter);
            contentFlow.Dock = DockStyle.Fill;
            contentFlow.FlowDirection = FlowDirection.TopDown;
            contentFlow.Location = new Point(0, 91);
            contentFlow.Name = "contentFlow";
            contentFlow.Padding = new Padding(24, 14, 24, 28);
            contentFlow.Size = new Size(758, 650);
            contentFlow.TabIndex = 0;
            contentFlow.WrapContents = false;
            // 
            // cardIntro
            // 
            cardIntro.BackColor = Color.FromArgb(106, 92, 255);
            cardIntro.Controls.Add(btnSelectNow);
            cardIntro.Controls.Add(lblIntroText);
            cardIntro.Controls.Add(lblIntroTitle);
            cardIntro.Location = new Point(27, 17);
            cardIntro.Margin = new Padding(3, 3, 3, 14);
            cardIntro.Name = "cardIntro";
            cardIntro.Size = new Size(682, 142);
            cardIntro.TabIndex = 0;
            // 
            // btnSelectNow
            // 
            btnSelectNow.BackColor = Color.White;
            btnSelectNow.Cursor = Cursors.Hand;
            btnSelectNow.FlatAppearance.BorderSize = 0;
            btnSelectNow.FlatStyle = FlatStyle.Flat;
            btnSelectNow.Font = new Font("Segoe UI Semibold", 10F);
            btnSelectNow.ForeColor = Color.FromArgb(77, 63, 220);
            btnSelectNow.Location = new Point(490, 49);
            btnSelectNow.Name = "btnSelectNow";
            btnSelectNow.Size = new Size(160, 46);
            btnSelectNow.TabIndex = 0;
            btnSelectNow.Text = "Alan seçmeye başla";
            btnSelectNow.UseVisualStyleBackColor = false;
            btnSelectNow.Click += btnSelectNow_Click;
            // 
            // lblIntroText
            // 
            lblIntroText.Font = new Font("Segoe UI", 10F);
            lblIntroText.ForeColor = Color.FromArgb(226, 223, 255);
            lblIntroText.Location = new Point(28, 68);
            lblIntroText.Name = "lblIntroText";
            lblIntroText.Size = new Size(426, 48);
            lblIntroText.TabIndex = 1;
            lblIntroText.Text = "Ekrandan bir alan seç; metni kopyala, anında çevir veya bilgisayarında çalan şarkıyı bul.";
            // 
            // lblIntroTitle
            // 
            lblIntroTitle.AutoSize = true;
            lblIntroTitle.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            lblIntroTitle.ForeColor = Color.White;
            lblIntroTitle.Location = new Point(26, 25);
            lblIntroTitle.Name = "lblIntroTitle";
            lblIntroTitle.Size = new Size(264, 32);
            lblIntroTitle.TabIndex = 2;
            lblIntroTitle.Text = "Ekranın artık seçilebilir.";
            // 
            // cardHotkey
            // 
            cardHotkey.BackColor = Color.White;
            cardHotkey.Controls.Add(lblHotkeyState);
            cardHotkey.Controls.Add(btnChangeShortcut);
            cardHotkey.Controls.Add(txtShortcut);
            cardHotkey.Controls.Add(lblShortcutLabel);
            cardHotkey.Controls.Add(lblHotkeyDescription);
            cardHotkey.Controls.Add(lblHotkeyTitle);
            cardHotkey.Location = new Point(27, 176);
            cardHotkey.Margin = new Padding(3, 3, 3, 14);
            cardHotkey.Name = "cardHotkey";
            cardHotkey.Size = new Size(682, 168);
            cardHotkey.TabIndex = 1;
            // 
            // lblHotkeyState
            // 
            lblHotkeyState.AutoSize = true;
            lblHotkeyState.Font = new Font("Segoe UI", 8.5F);
            lblHotkeyState.ForeColor = Color.FromArgb(90, 102, 126);
            lblHotkeyState.Location = new Point(28, 137);
            lblHotkeyState.Name = "lblHotkeyState";
            lblHotkeyState.Size = new Size(287, 15);
            lblHotkeyState.TabIndex = 0;
            lblHotkeyState.Text = "Tek tuş veya tuş birleşimi atayabilirsiniz. Örnek: Pause";
            // 
            // btnChangeShortcut
            // 
            btnChangeShortcut.BackColor = Color.FromArgb(238, 236, 255);
            btnChangeShortcut.Cursor = Cursors.Hand;
            btnChangeShortcut.FlatAppearance.BorderSize = 0;
            btnChangeShortcut.FlatStyle = FlatStyle.Flat;
            btnChangeShortcut.Font = new Font("Segoe UI Semibold", 9.5F);
            btnChangeShortcut.ForeColor = Color.FromArgb(82, 68, 221);
            btnChangeShortcut.Location = new Point(515, 90);
            btnChangeShortcut.Name = "btnChangeShortcut";
            btnChangeShortcut.Size = new Size(135, 36);
            btnChangeShortcut.TabIndex = 1;
            btnChangeShortcut.Text = "Kısayolu değiştir";
            btnChangeShortcut.UseVisualStyleBackColor = false;
            btnChangeShortcut.Click += btnChangeShortcut_Click;
            // 
            // txtShortcut
            // 
            txtShortcut.BackColor = Color.FromArgb(247, 248, 252);
            txtShortcut.BorderStyle = BorderStyle.FixedSingle;
            txtShortcut.Font = new Font("Segoe UI Semibold", 11F);
            txtShortcut.ForeColor = Color.FromArgb(31, 37, 56);
            txtShortcut.Location = new Point(345, 94);
            txtShortcut.Name = "txtShortcut";
            txtShortcut.ReadOnly = true;
            txtShortcut.Size = new Size(154, 27);
            txtShortcut.TabIndex = 2;
            txtShortcut.Text = "Ctrl + Shift + Space";
            txtShortcut.TextAlign = HorizontalAlignment.Center;
            // 
            // lblShortcutLabel
            // 
            lblShortcutLabel.Location = new Point(0, 0);
            lblShortcutLabel.Name = "lblShortcutLabel";
            lblShortcutLabel.Size = new Size(100, 23);
            lblShortcutLabel.TabIndex = 3;
            // 
            // lblHotkeyDescription
            // 
            lblHotkeyDescription.Font = new Font("Segoe UI", 9.5F);
            lblHotkeyDescription.ForeColor = Color.FromArgb(94, 103, 124);
            lblHotkeyDescription.Location = new Point(28, 65);
            lblHotkeyDescription.Name = "lblHotkeyDescription";
            lblHotkeyDescription.Size = new Size(281, 61);
            lblHotkeyDescription.TabIndex = 4;
            lblHotkeyDescription.Text = "Uygulama arka plandayken bile bu kısayol alan seçme ekranını açar.";
            // 
            // lblHotkeyTitle
            // 
            lblHotkeyTitle.Location = new Point(0, 0);
            lblHotkeyTitle.Name = "lblHotkeyTitle";
            lblHotkeyTitle.Size = new Size(100, 23);
            lblHotkeyTitle.TabIndex = 5;
            // 
            // cardFeatures
            // 
            cardFeatures.BackColor = Color.White;
            cardFeatures.Controls.Add(featureMusic);
            cardFeatures.Controls.Add(featureTranslate);
            cardFeatures.Controls.Add(featureText);
            cardFeatures.Controls.Add(lblFeaturesDescription);
            cardFeatures.Controls.Add(lblFeaturesTitle);
            cardFeatures.Location = new Point(27, 361);
            cardFeatures.Margin = new Padding(3, 3, 3, 14);
            cardFeatures.Name = "cardFeatures";
            cardFeatures.Size = new Size(682, 225);
            cardFeatures.TabIndex = 2;
            // 
            // featureMusic
            // 
            featureMusic.Location = new Point(0, 0);
            featureMusic.Name = "featureMusic";
            featureMusic.Size = new Size(200, 100);
            featureMusic.TabIndex = 0;
            // 
            // featureTranslate
            // 
            featureTranslate.Location = new Point(0, 0);
            featureTranslate.Name = "featureTranslate";
            featureTranslate.Size = new Size(200, 100);
            featureTranslate.TabIndex = 1;
            // 
            // featureText
            // 
            featureText.Location = new Point(0, 0);
            featureText.Name = "featureText";
            featureText.Size = new Size(200, 100);
            featureText.TabIndex = 2;
            // 
            // lblFeaturesDescription
            // 
            lblFeaturesDescription.Location = new Point(0, 0);
            lblFeaturesDescription.Name = "lblFeaturesDescription";
            lblFeaturesDescription.Size = new Size(100, 23);
            lblFeaturesDescription.TabIndex = 3;
            // 
            // lblFeaturesTitle
            // 
            lblFeaturesTitle.Location = new Point(0, 0);
            lblFeaturesTitle.Name = "lblFeaturesTitle";
            lblFeaturesTitle.Size = new Size(100, 23);
            lblFeaturesTitle.TabIndex = 4;
            // 
            // cardTranslation
            // 
            cardTranslation.BackColor = Color.White;
            cardTranslation.Controls.Add(lblTranslationHint);
            cardTranslation.Controls.Add(btnSwapLanguages);
            cardTranslation.Controls.Add(cmbTargetLanguage);
            cardTranslation.Controls.Add(lblTargetLanguage);
            cardTranslation.Controls.Add(cmbSourceLanguage);
            cardTranslation.Controls.Add(lblSourceLanguage);
            cardTranslation.Controls.Add(lblTranslationDescription);
            cardTranslation.Controls.Add(lblTranslationTitle);
            cardTranslation.Location = new Point(27, 603);
            cardTranslation.Margin = new Padding(3, 3, 3, 14);
            cardTranslation.Name = "cardTranslation";
            cardTranslation.Size = new Size(682, 190);
            cardTranslation.TabIndex = 3;
            // 
            // lblTranslationHint
            // 
            lblTranslationHint.AutoSize = true;
            lblTranslationHint.Font = new Font("Segoe UI", 8.5F);
            lblTranslationHint.ForeColor = Color.FromArgb(116, 125, 145);
            lblTranslationHint.Location = new Point(28, 158);
            lblTranslationHint.Name = "lblTranslationHint";
            lblTranslationHint.Size = new Size(322, 15);
            lblTranslationHint.TabIndex = 0;
            lblTranslationHint.Text = "Google Translate kaynak dili otomatik olarak da algılayabilir.";
            // 
            // btnSwapLanguages
            // 
            btnSwapLanguages.BackColor = Color.FromArgb(238, 236, 255);
            btnSwapLanguages.Cursor = Cursors.Hand;
            btnSwapLanguages.FlatAppearance.BorderSize = 0;
            btnSwapLanguages.FlatStyle = FlatStyle.Flat;
            btnSwapLanguages.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            btnSwapLanguages.ForeColor = Color.FromArgb(83, 70, 222);
            btnSwapLanguages.Location = new Point(321, 104);
            btnSwapLanguages.Name = "btnSwapLanguages";
            btnSwapLanguages.Size = new Size(42, 36);
            btnSwapLanguages.TabIndex = 1;
            btnSwapLanguages.Text = "⇄";
            btnSwapLanguages.UseVisualStyleBackColor = false;
            btnSwapLanguages.Click += btnSwapLanguages_Click;
            // 
            // cmbTargetLanguage
            // 
            cmbTargetLanguage.Location = new Point(0, 0);
            cmbTargetLanguage.Name = "cmbTargetLanguage";
            cmbTargetLanguage.Size = new Size(121, 23);
            cmbTargetLanguage.TabIndex = 2;
            // 
            // lblTargetLanguage
            // 
            lblTargetLanguage.Location = new Point(0, 0);
            lblTargetLanguage.Name = "lblTargetLanguage";
            lblTargetLanguage.Size = new Size(100, 23);
            lblTargetLanguage.TabIndex = 3;
            // 
            // cmbSourceLanguage
            // 
            cmbSourceLanguage.Location = new Point(0, 0);
            cmbSourceLanguage.Name = "cmbSourceLanguage";
            cmbSourceLanguage.Size = new Size(121, 23);
            cmbSourceLanguage.TabIndex = 4;
            // 
            // lblSourceLanguage
            // 
            lblSourceLanguage.Location = new Point(0, 0);
            lblSourceLanguage.Name = "lblSourceLanguage";
            lblSourceLanguage.Size = new Size(100, 23);
            lblSourceLanguage.TabIndex = 5;
            // 
            // lblTranslationDescription
            // 
            lblTranslationDescription.Location = new Point(0, 0);
            lblTranslationDescription.Name = "lblTranslationDescription";
            lblTranslationDescription.Size = new Size(100, 23);
            lblTranslationDescription.TabIndex = 6;
            // 
            // lblTranslationTitle
            // 
            lblTranslationTitle.Location = new Point(0, 0);
            lblTranslationTitle.Name = "lblTranslationTitle";
            lblTranslationTitle.Size = new Size(100, 23);
            lblTranslationTitle.TabIndex = 7;
            // 
            // cardStartup
            // 
            cardStartup.BackColor = Color.White;
            cardStartup.Controls.Add(chkStartMinimized);
            cardStartup.Controls.Add(chkStartWithWindows);
            cardStartup.Controls.Add(lblStartupDescription);
            cardStartup.Controls.Add(lblStartupTitle);
            cardStartup.Location = new Point(27, 810);
            cardStartup.Margin = new Padding(3, 3, 3, 14);
            cardStartup.Name = "cardStartup";
            cardStartup.Size = new Size(682, 152);
            cardStartup.TabIndex = 4;
            // 
            // chkStartMinimized
            // 
            chkStartMinimized.Location = new Point(0, 0);
            chkStartMinimized.Name = "chkStartMinimized";
            chkStartMinimized.Size = new Size(104, 24);
            chkStartMinimized.TabIndex = 0;
            // 
            // chkStartWithWindows
            // 
            chkStartWithWindows.Location = new Point(0, 0);
            chkStartWithWindows.Name = "chkStartWithWindows";
            chkStartWithWindows.Size = new Size(104, 24);
            chkStartWithWindows.TabIndex = 1;
            // 
            // lblStartupDescription
            // 
            lblStartupDescription.Location = new Point(0, 0);
            lblStartupDescription.Name = "lblStartupDescription";
            lblStartupDescription.Size = new Size(100, 23);
            lblStartupDescription.TabIndex = 2;
            // 
            // lblStartupTitle
            // 
            lblStartupTitle.Location = new Point(0, 0);
            lblStartupTitle.Name = "lblStartupTitle";
            lblStartupTitle.Size = new Size(100, 23);
            lblStartupTitle.TabIndex = 3;
            // 
            // cardMusic
            // 
            cardMusic.BackColor = Color.White;
            cardMusic.Controls.Add(linkAudD);
            cardMusic.Controls.Add(lblTokenHint);
            cardMusic.Controls.Add(txtAudDToken);
            cardMusic.Controls.Add(lblToken);
            cardMusic.Controls.Add(lblMusicDescription);
            cardMusic.Controls.Add(lblMusicTitle);
            cardMusic.Location = new Point(27, 979);
            cardMusic.Margin = new Padding(3, 3, 3, 14);
            cardMusic.Name = "cardMusic";
            cardMusic.Size = new Size(682, 192);
            cardMusic.TabIndex = 5;
            // 
            // linkAudD
            // 
            linkAudD.AutoSize = true;
            linkAudD.Font = new Font("Segoe UI Semibold", 8.5F);
            linkAudD.LinkColor = Color.FromArgb(91, 76, 230);
            linkAudD.Location = new Point(532, 159);
            linkAudD.Name = "linkAudD";
            linkAudD.Size = new Size(118, 15);
            linkAudD.TabIndex = 0;
            linkAudD.TabStop = true;
            linkAudD.Text = "AudD anahtarı alın ↗";
            linkAudD.LinkClicked += linkAudD_LinkClicked;
            // 
            // lblTokenHint
            // 
            lblTokenHint.AutoSize = true;
            lblTokenHint.Font = new Font("Segoe UI", 8.5F);
            lblTokenHint.ForeColor = Color.FromArgb(116, 125, 145);
            lblTokenHint.Location = new Point(28, 158);
            lblTokenHint.Name = "lblTokenHint";
            lblTokenHint.Size = new Size(283, 15);
            lblTokenHint.TabIndex = 1;
            lblTokenHint.Text = "Anahtar yalnızca bu bilgisayardaki ayarlarda saklanır.";
            // 
            // txtAudDToken
            // 
            txtAudDToken.BackColor = Color.FromArgb(247, 248, 252);
            txtAudDToken.BorderStyle = BorderStyle.FixedSingle;
            txtAudDToken.Font = new Font("Segoe UI", 10F);
            txtAudDToken.Location = new Point(28, 119);
            txtAudDToken.Name = "txtAudDToken";
            txtAudDToken.PlaceholderText = "API anahtarını buraya yapıştırın";
            txtAudDToken.Size = new Size(622, 25);
            txtAudDToken.TabIndex = 2;
            txtAudDToken.UseSystemPasswordChar = true;
            txtAudDToken.TextChanged += txtAudDToken_TextChanged;
            // 
            // lblToken
            // 
            lblToken.Location = new Point(0, 0);
            lblToken.Name = "lblToken";
            lblToken.Size = new Size(100, 23);
            lblToken.TabIndex = 3;
            // 
            // lblMusicDescription
            // 
            lblMusicDescription.Location = new Point(0, 0);
            lblMusicDescription.Name = "lblMusicDescription";
            lblMusicDescription.Size = new Size(100, 23);
            lblMusicDescription.TabIndex = 4;
            // 
            // lblMusicTitle
            // 
            lblMusicTitle.Location = new Point(0, 0);
            lblMusicTitle.Name = "lblMusicTitle";
            lblMusicTitle.Size = new Size(100, 23);
            lblMusicTitle.TabIndex = 5;
            // 
            // lblFooter
            // 
            lblFooter.Font = new Font("Segoe UI", 8.5F);
            lblFooter.ForeColor = Color.FromArgb(120, 128, 148);
            lblFooter.Location = new Point(27, 1188);
            lblFooter.Margin = new Padding(3, 3, 3, 20);
            lblFooter.Name = "lblFooter";
            lblFooter.Size = new Size(682, 38);
            lblFooter.TabIndex = 6;
            lblFooter.Text = "İpucu: Alan seçimini Esc tuşuyla iptal edebilirsiniz.\r\nScreenSelector görüntüleri bilgisayarınızda kalıcı olarak saklamaz.";
            lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(btnHeaderMinimize);
            panelHeader.Controls.Add(lblHeaderHint);
            panelHeader.Controls.Add(lblHeaderTitle);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(758, 91);
            panelHeader.TabIndex = 1;
            // 
            // btnHeaderMinimize
            // 
            btnHeaderMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnHeaderMinimize.BackColor = Color.FromArgb(246, 247, 251);
            btnHeaderMinimize.Cursor = Cursors.Hand;
            btnHeaderMinimize.FlatAppearance.BorderSize = 0;
            btnHeaderMinimize.FlatStyle = FlatStyle.Flat;
            btnHeaderMinimize.Font = new Font("Segoe UI Semibold", 9F);
            btnHeaderMinimize.ForeColor = Color.FromArgb(69, 78, 99);
            btnHeaderMinimize.Location = new Point(604, 29);
            btnHeaderMinimize.Name = "btnHeaderMinimize";
            btnHeaderMinimize.Size = new Size(125, 34);
            btnHeaderMinimize.TabIndex = 0;
            btnHeaderMinimize.Text = "Bildirim alanına al";
            btnHeaderMinimize.UseVisualStyleBackColor = false;
            btnHeaderMinimize.Click += btnHeaderMinimize_Click;
            // 
            // lblHeaderHint
            // 
            lblHeaderHint.AutoSize = true;
            lblHeaderHint.Font = new Font("Segoe UI", 9F);
            lblHeaderHint.ForeColor = Color.FromArgb(112, 121, 142);
            lblHeaderHint.Location = new Point(29, 57);
            lblHeaderHint.Name = "lblHeaderHint";
            lblHeaderHint.Size = new Size(271, 15);
            lblHeaderHint.TabIndex = 1;
            lblHeaderHint.Text = "Seçim kısayolunuzu ve özellikleri buradan yönetin.";
            // 
            // lblHeaderTitle
            // 
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            lblHeaderTitle.ForeColor = Color.FromArgb(27, 33, 51);
            lblHeaderTitle.Location = new Point(27, 22);
            lblHeaderTitle.Name = "lblHeaderTitle";
            lblHeaderTitle.Size = new Size(140, 32);
            lblHeaderTitle.TabIndex = 2;
            lblHeaderTitle.Text = "Genel bakış";
            // 
            // lblFeatureMusicDescription
            // 
            lblFeatureMusicDescription.Location = new Point(0, 0);
            lblFeatureMusicDescription.Name = "lblFeatureMusicDescription";
            lblFeatureMusicDescription.Size = new Size(100, 23);
            lblFeatureMusicDescription.TabIndex = 0;
            // 
            // lblFeatureMusicTitle
            // 
            lblFeatureMusicTitle.Location = new Point(0, 0);
            lblFeatureMusicTitle.Name = "lblFeatureMusicTitle";
            lblFeatureMusicTitle.Size = new Size(100, 23);
            lblFeatureMusicTitle.TabIndex = 0;
            // 
            // lblFeatureMusicIcon
            // 
            lblFeatureMusicIcon.Location = new Point(0, 0);
            lblFeatureMusicIcon.Name = "lblFeatureMusicIcon";
            lblFeatureMusicIcon.Size = new Size(100, 23);
            lblFeatureMusicIcon.TabIndex = 0;
            // 
            // lblFeatureTranslateDescription
            // 
            lblFeatureTranslateDescription.Location = new Point(0, 0);
            lblFeatureTranslateDescription.Name = "lblFeatureTranslateDescription";
            lblFeatureTranslateDescription.Size = new Size(100, 23);
            lblFeatureTranslateDescription.TabIndex = 0;
            // 
            // lblFeatureTranslateTitle
            // 
            lblFeatureTranslateTitle.Location = new Point(0, 0);
            lblFeatureTranslateTitle.Name = "lblFeatureTranslateTitle";
            lblFeatureTranslateTitle.Size = new Size(100, 23);
            lblFeatureTranslateTitle.TabIndex = 0;
            // 
            // lblFeatureTranslateIcon
            // 
            lblFeatureTranslateIcon.Location = new Point(0, 0);
            lblFeatureTranslateIcon.Name = "lblFeatureTranslateIcon";
            lblFeatureTranslateIcon.Size = new Size(100, 23);
            lblFeatureTranslateIcon.TabIndex = 0;
            // 
            // lblFeatureTextDescription
            // 
            lblFeatureTextDescription.Location = new Point(0, 0);
            lblFeatureTextDescription.Name = "lblFeatureTextDescription";
            lblFeatureTextDescription.Size = new Size(100, 23);
            lblFeatureTextDescription.TabIndex = 0;
            // 
            // lblFeatureTextTitle
            // 
            lblFeatureTextTitle.Location = new Point(0, 0);
            lblFeatureTextTitle.Name = "lblFeatureTextTitle";
            lblFeatureTextTitle.Size = new Size(100, 23);
            lblFeatureTextTitle.TabIndex = 0;
            // 
            // lblFeatureTextIcon
            // 
            lblFeatureTextIcon.Location = new Point(0, 0);
            lblFeatureTextIcon.Name = "lblFeatureTextIcon";
            lblFeatureTextIcon.Size = new Size(100, 23);
            lblFeatureTextIcon.TabIndex = 0;
            // 
            // notifyIcon
            // 
            notifyIcon.ContextMenuStrip = trayMenu;
            notifyIcon.Text = "ScreenSelector — kısayol hazır";
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            // 
            // trayMenu
            // 
            trayMenu.Items.AddRange(new ToolStripItem[] { menuOpen, menuSelect, menuSeparator, menuExit });
            trayMenu.Name = "trayMenu";
            trayMenu.Size = new Size(178, 76);
            // 
            // menuOpen
            // 
            menuOpen.Name = "menuOpen";
            menuOpen.Size = new Size(177, 22);
            menuOpen.Text = "ScreenSelector'ı aç";
            menuOpen.Click += menuOpen_Click;
            // 
            // menuSelect
            // 
            menuSelect.Name = "menuSelect";
            menuSelect.Size = new Size(177, 22);
            menuSelect.Text = "Alan seçmeye başla";
            menuSelect.Click += menuSelect_Click;
            // 
            // menuSeparator
            // 
            menuSeparator.Name = "menuSeparator";
            menuSeparator.Size = new Size(174, 6);
            // 
            // menuExit
            // 
            menuExit.Name = "menuExit";
            menuExit.Size = new Size(177, 22);
            menuExit.Text = "Çıkış";
            menuExit.Click += menuExit_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(244, 246, 251);
            ClientSize = new Size(984, 741);
            Controls.Add(panelMain);
            Controls.Add(panelSidebar);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            MinimumSize = new Size(1000, 720);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = " ";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            KeyDown += Form1_KeyDown;
            Resize += Form1_Resize;
            panelSidebar.ResumeLayout(false);
            panelSidebar.PerformLayout();
            panelReady.ResumeLayout(false);
            panelReady.PerformLayout();
            panelMain.ResumeLayout(false);
            contentFlow.ResumeLayout(false);
            cardIntro.ResumeLayout(false);
            cardIntro.PerformLayout();
            cardHotkey.ResumeLayout(false);
            cardHotkey.PerformLayout();
            cardFeatures.ResumeLayout(false);
            cardTranslation.ResumeLayout(false);
            cardTranslation.PerformLayout();
            cardStartup.ResumeLayout(false);
            cardMusic.ResumeLayout(false);
            cardMusic.PerformLayout();
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            trayMenu.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panelSidebar, panelReady, panelMain, panelHeader, cardIntro, cardHotkey, cardFeatures;
        private Panel featureText, featureTranslate, featureMusic, cardTranslation, cardStartup, cardMusic;
        private Label lblVersion, lblReady, lblReadyDot, lblBrandSubtitle, lblBrand, lblHeaderTitle, lblHeaderHint;
        private Label lblIntroText, lblIntroTitle, lblHotkeyState, lblShortcutLabel, lblHotkeyDescription, lblHotkeyTitle;
        private Label lblFeatureMusicDescription, lblFeatureMusicTitle, lblFeatureMusicIcon;
        private Label lblFeatureTranslateDescription, lblFeatureTranslateTitle, lblFeatureTranslateIcon;
        private Label lblFeatureTextDescription, lblFeatureTextTitle, lblFeatureTextIcon, lblFeaturesDescription, lblFeaturesTitle;
        private Label lblTranslationHint, lblTargetLanguage, lblSourceLanguage, lblTranslationDescription, lblTranslationTitle;
        private Label lblStartupDescription, lblStartupTitle, lblTokenHint, lblToken, lblMusicDescription, lblMusicTitle, lblFooter;
        private Button btnNavMusic, btnNavTranslation, btnNavSettings, btnNavHome, btnHeaderMinimize, btnSelectNow;
        private Button btnChangeShortcut, btnSwapLanguages;
        private TextBox txtShortcut, txtAudDToken;
        private ComboBox cmbTargetLanguage, cmbSourceLanguage;
        private CheckBox chkStartMinimized, chkStartWithWindows;
        private LinkLabel linkAudD;
        private FlowLayoutPanel contentFlow;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem menuOpen, menuSelect, menuExit;
        private ToolStripSeparator menuSeparator;
        private ToolTip toolTip;
    }
}
