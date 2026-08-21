namespace ScreenSelector
{
    partial class ResultForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && pictureSelection?.Image != null) pictureSelection.Image.Dispose();
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelHeader = new Panel();
            btnClose = new Button();
            lblSubtitle = new Label();
            lblTitle = new Label();
            panelContent = new Panel();
            linkResult = new LinkLabel();
            btnCopySecondary = new Button();
            txtSecondary = new RichTextBox();
            lblSecondary = new Label();
            btnCopyPrimary = new Button();
            txtPrimary = new RichTextBox();
            lblPrimary = new Label();
            pictureSelection = new PictureBox();
            panelHeader.SuspendLayout();
            panelContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureSelection).BeginInit();
            SuspendLayout();
            // header
            panelHeader.BackColor = Color.FromArgb(25, 29, 43);
            panelHeader.Controls.Add(btnClose);
            panelHeader.Controls.Add(lblSubtitle);
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Size = new Size(650, 88);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.BackColor = Color.FromArgb(45, 50, 68);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI Semibold", 12F);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(594, 21);
            btnClose.Size = new Size(38, 38);
            btnClose.Text = "×";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 8.5F);
            lblSubtitle.ForeColor = Color.FromArgb(158, 166, 188);
            lblSubtitle.Location = new Point(27, 57);
            lblSubtitle.Text = "Seçiminiz başarıyla işlendi";
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(25, 20);
            lblTitle.Text = "Sonuç";
            // content
            panelContent.AutoScroll = true;
            panelContent.BackColor = Color.FromArgb(244, 246, 251);
            panelContent.Controls.Add(linkResult);
            panelContent.Controls.Add(btnCopySecondary);
            panelContent.Controls.Add(txtSecondary);
            panelContent.Controls.Add(lblSecondary);
            panelContent.Controls.Add(btnCopyPrimary);
            panelContent.Controls.Add(txtPrimary);
            panelContent.Controls.Add(lblPrimary);
            panelContent.Controls.Add(pictureSelection);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Padding = new Padding(26);
            linkResult.AutoSize = true;
            linkResult.Font = new Font("Segoe UI Semibold", 9F);
            linkResult.LinkColor = Color.FromArgb(87, 72, 225);
            linkResult.Location = new Point(26, 537);
            linkResult.Text = "Sonucu web'de aç ↗";
            linkResult.Visible = false;
            linkResult.LinkClicked += linkResult_LinkClicked;
            ConfigureCopyButton(btnCopySecondary, 523);
            btnCopySecondary.Click += btnCopySecondary_Click;
            txtSecondary.BackColor = Color.White;
            txtSecondary.BorderStyle = BorderStyle.None;
            txtSecondary.Font = new Font("Segoe UI", 10.5F);
            txtSecondary.ForeColor = Color.FromArgb(37, 44, 64);
            txtSecondary.Location = new Point(26, 394);
            txtSecondary.ReadOnly = true;
            txtSecondary.Size = new Size(598, 116);
            lblSecondary.AutoSize = true;
            lblSecondary.Font = new Font("Segoe UI Semibold", 9.5F);
            lblSecondary.ForeColor = Color.FromArgb(72, 80, 101);
            lblSecondary.Location = new Point(26, 368);
            lblSecondary.Text = "Çeviri";
            ConfigureCopyButton(btnCopyPrimary, 345);
            btnCopyPrimary.Click += btnCopyPrimary_Click;
            txtPrimary.BackColor = Color.White;
            txtPrimary.BorderStyle = BorderStyle.None;
            txtPrimary.Font = new Font("Segoe UI", 10.5F);
            txtPrimary.ForeColor = Color.FromArgb(37, 44, 64);
            txtPrimary.Location = new Point(26, 216);
            txtPrimary.ReadOnly = true;
            txtPrimary.Size = new Size(598, 116);
            lblPrimary.AutoSize = true;
            lblPrimary.Font = new Font("Segoe UI Semibold", 9.5F);
            lblPrimary.ForeColor = Color.FromArgb(72, 80, 101);
            lblPrimary.Location = new Point(26, 190);
            lblPrimary.Text = "Algılanan metin";
            pictureSelection.BackColor = Color.FromArgb(224, 228, 238);
            pictureSelection.Location = new Point(26, 26);
            pictureSelection.Size = new Size(598, 142);
            pictureSelection.SizeMode = PictureBoxSizeMode.Zoom;
            // ResultForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(244, 246, 251);
            ClientSize = new Size(650, 680);
            Controls.Add(panelContent);
            Controls.Add(panelHeader);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ResultForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ScreenSelector — Sonuç";
            TopMost = true;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelContent.ResumeLayout(false);
            panelContent.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureSelection).EndInit();
            ResumeLayout(false);
        }

        private static void ConfigureCopyButton(Button button, int top)
        {
            button.BackColor = Color.FromArgb(238, 236, 255);
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderSize = 0;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Segoe UI Semibold", 8.5F);
            button.ForeColor = Color.FromArgb(82, 68, 221);
            button.Location = new Point(527, top);
            button.Size = new Size(97, 30);
            button.Text = "Kopyala";
            button.UseVisualStyleBackColor = false;
        }

        private Panel panelHeader, panelContent;
        private Button btnClose, btnCopySecondary, btnCopyPrimary;
        private Label lblSubtitle, lblTitle, lblSecondary, lblPrimary;
        private LinkLabel linkResult;
        private RichTextBox txtSecondary, txtPrimary;
        private PictureBox pictureSelection;
    }
}
