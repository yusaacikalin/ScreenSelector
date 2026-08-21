namespace ScreenSelector
{
    partial class ActionToolbarForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            if (disposing)
            {
                _capture.Dispose();
                _cancellation.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panelToolbar = new Panel();
            progressBusy = new ProgressBar();
            lblStatus = new Label();
            btnClose = new Button();
            panelSeparator = new Panel();
            btnMusic = new Button();
            btnTranslate = new Button();
            btnExtractText = new Button();
            panelToast = new Panel();
            panelToastAccent = new Panel();
            lblToastIcon = new Label();
            lblToastTitle = new Label();
            lblToastMessage = new Label();
            toastTimer = new System.Windows.Forms.Timer(components);
            panelToolbar.SuspendLayout();
            panelToast.SuspendLayout();
            SuspendLayout();
            // panelToolbar
            panelToolbar.BackColor = Color.FromArgb(25, 29, 43);
            panelToolbar.Controls.Add(progressBusy);
            panelToolbar.Controls.Add(lblStatus);
            panelToolbar.Controls.Add(btnClose);
            panelToolbar.Controls.Add(panelSeparator);
            panelToolbar.Controls.Add(btnMusic);
            panelToolbar.Controls.Add(btnTranslate);
            panelToolbar.Controls.Add(btnExtractText);
            panelToolbar.Dock = DockStyle.Top;
            panelToolbar.Location = new Point(0, 0);
            panelToolbar.Name = "panelToolbar";
            panelToolbar.Padding = new Padding(10);
            panelToolbar.Size = new Size(610, 86);
            // progressBusy
            progressBusy.Location = new Point(18, 62);
            progressBusy.MarqueeAnimationSpeed = 24;
            progressBusy.Name = "progressBusy";
            progressBusy.Size = new Size(526, 3);
            progressBusy.Style = ProgressBarStyle.Marquee;
            progressBusy.TabIndex = 6;
            progressBusy.Visible = false;
            // lblStatus
            lblStatus.Font = new Font("Segoe UI", 8F);
            lblStatus.ForeColor = Color.FromArgb(172, 180, 201);
            lblStatus.Location = new Point(18, 67);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(526, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Bir işlem seçin";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // btnClose
            btnClose.BackColor = Color.FromArgb(44, 49, 66);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI Semibold", 11F);
            btnClose.ForeColor = Color.FromArgb(214, 219, 232);
            btnClose.Location = new Point(552, 12);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(46, 46);
            btnClose.TabIndex = 4;
            btnClose.Text = "×";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            // panelSeparator
            panelSeparator.BackColor = Color.FromArgb(60, 65, 83);
            panelSeparator.Location = new Point(536, 18);
            panelSeparator.Name = "panelSeparator";
            panelSeparator.Size = new Size(1, 34);
            panelSeparator.TabIndex = 3;
            // btnMusic
            btnMusic.BackColor = Color.FromArgb(47, 51, 69);
            btnMusic.Cursor = Cursors.Hand;
            btnMusic.FlatAppearance.BorderSize = 0;
            btnMusic.FlatStyle = FlatStyle.Flat;
            btnMusic.Font = new Font("Segoe UI Semibold", 9.5F);
            btnMusic.ForeColor = Color.White;
            btnMusic.Location = new Point(364, 12);
            btnMusic.Name = "btnMusic";
            btnMusic.Size = new Size(154, 46);
            btnMusic.TabIndex = 2;
            btnMusic.Text = "♫  Şarkıyı bul";
            btnMusic.UseVisualStyleBackColor = false;
            btnMusic.Click += btnMusic_Click;
            // btnTranslate
            btnTranslate.BackColor = Color.FromArgb(47, 51, 69);
            btnTranslate.Cursor = Cursors.Hand;
            btnTranslate.FlatAppearance.BorderSize = 0;
            btnTranslate.FlatStyle = FlatStyle.Flat;
            btnTranslate.Font = new Font("Segoe UI Semibold", 9.5F);
            btnTranslate.ForeColor = Color.White;
            btnTranslate.Location = new Point(191, 12);
            btnTranslate.Name = "btnTranslate";
            btnTranslate.Size = new Size(154, 46);
            btnTranslate.TabIndex = 1;
            btnTranslate.Text = "文  Çevir";
            btnTranslate.UseVisualStyleBackColor = false;
            btnTranslate.Click += btnTranslate_Click;
            // btnExtractText
            btnExtractText.BackColor = Color.FromArgb(106, 92, 255);
            btnExtractText.Cursor = Cursors.Hand;
            btnExtractText.FlatAppearance.BorderSize = 0;
            btnExtractText.FlatStyle = FlatStyle.Flat;
            btnExtractText.Font = new Font("Segoe UI Semibold", 9.5F);
            btnExtractText.ForeColor = Color.White;
            btnExtractText.Location = new Point(18, 12);
            btnExtractText.Name = "btnExtractText";
            btnExtractText.Size = new Size(154, 46);
            btnExtractText.TabIndex = 0;
            btnExtractText.Text = "T  Metni çıkar";
            btnExtractText.UseVisualStyleBackColor = false;
            btnExtractText.Click += btnExtractText_Click;
            // panelToast
            panelToast.BackColor = Color.FromArgb(255, 244, 245);
            panelToast.Controls.Add(lblToastMessage);
            panelToast.Controls.Add(lblToastTitle);
            panelToast.Controls.Add(lblToastIcon);
            panelToast.Controls.Add(panelToastAccent);
            panelToast.Location = new Point(0, 86);
            panelToast.Name = "panelToast";
            panelToast.Size = new Size(610, 84);
            panelToast.TabIndex = 1;
            // panelToastAccent
            panelToastAccent.BackColor = Color.FromArgb(237, 84, 99);
            panelToastAccent.Dock = DockStyle.Left;
            panelToastAccent.Location = new Point(0, 0);
            panelToastAccent.Name = "panelToastAccent";
            panelToastAccent.Size = new Size(4, 84);
            panelToastAccent.TabIndex = 0;
            // lblToastIcon
            lblToastIcon.BackColor = Color.FromArgb(237, 84, 99);
            lblToastIcon.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblToastIcon.ForeColor = Color.White;
            lblToastIcon.Location = new Point(18, 18);
            lblToastIcon.Name = "lblToastIcon";
            lblToastIcon.Size = new Size(30, 30);
            lblToastIcon.TabIndex = 1;
            lblToastIcon.Text = "!";
            lblToastIcon.TextAlign = ContentAlignment.MiddleCenter;
            // lblToastTitle
            lblToastTitle.AutoSize = true;
            lblToastTitle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblToastTitle.ForeColor = Color.FromArgb(105, 35, 47);
            lblToastTitle.Location = new Point(61, 13);
            lblToastTitle.Name = "lblToastTitle";
            lblToastTitle.Size = new Size(112, 17);
            lblToastTitle.TabIndex = 2;
            lblToastTitle.Text = "İşlem tamamlanamadı";
            // lblToastMessage
            lblToastMessage.AutoEllipsis = true;
            lblToastMessage.Font = new Font("Segoe UI", 8.5F);
            lblToastMessage.ForeColor = Color.FromArgb(112, 66, 75);
            lblToastMessage.Location = new Point(61, 34);
            lblToastMessage.Name = "lblToastMessage";
            lblToastMessage.Size = new Size(530, 37);
            lblToastMessage.TabIndex = 3;
            lblToastMessage.Text = "Hata açıklaması burada görünür ve birkaç saniye sonra kendiliğinden kapanır.";
            // toastTimer
            toastTimer.Interval = 5500;
            toastTimer.Tick += toastTimer_Tick;
            // ActionToolbarForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(25, 29, 43);
            ClientSize = new Size(610, 170);
            Controls.Add(panelToast);
            Controls.Add(panelToolbar);
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Name = "ActionToolbarForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "İşlem seçin";
            TopMost = true;
            Deactivate += ActionToolbarForm_Deactivate;
            Shown += ActionToolbarForm_Shown;
            KeyDown += ActionToolbarForm_KeyDown;
            panelToolbar.ResumeLayout(false);
            panelToast.ResumeLayout(false);
            panelToast.PerformLayout();
            ResumeLayout(false);
        }

        private Panel panelToolbar;
        private ProgressBar progressBusy;
        private Label lblStatus;
        private Button btnClose;
        private Panel panelSeparator;
        private Button btnMusic;
        private Button btnTranslate;
        private Button btnExtractText;
        private Panel panelToast;
        private Panel panelToastAccent;
        private Label lblToastIcon;
        private Label lblToastTitle;
        private Label lblToastMessage;
        private System.Windows.Forms.Timer toastTimer;
    }
}
