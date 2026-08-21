namespace ScreenSelector
{
    partial class SelectionForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            if (disposing)
            {
                DisposeDrawingResources();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelInstruction = new Panel();
            btnIdentifyMusic = new Button();
            btnCancel = new Button();
            lblInstructionSub = new Label();
            lblInstruction = new Label();
            panelInstruction.SuspendLayout();
            SuspendLayout();
            // panelInstruction
            panelInstruction.BackColor = Color.FromArgb(27, 31, 46);
            panelInstruction.Controls.Add(btnIdentifyMusic);
            panelInstruction.Controls.Add(btnCancel);
            panelInstruction.Controls.Add(lblInstructionSub);
            panelInstruction.Controls.Add(lblInstruction);
            panelInstruction.Location = new Point(160, 25);
            panelInstruction.Name = "panelInstruction";
            panelInstruction.Size = new Size(640, 72);
            panelInstruction.TabIndex = 0;
            // btnIdentifyMusic
            btnIdentifyMusic.BackColor = Color.FromArgb(106, 92, 255);
            btnIdentifyMusic.Cursor = Cursors.Hand;
            btnIdentifyMusic.FlatAppearance.BorderSize = 0;
            btnIdentifyMusic.FlatStyle = FlatStyle.Flat;
            btnIdentifyMusic.Font = new Font("Segoe UI Semibold", 9F);
            btnIdentifyMusic.ForeColor = Color.White;
            btnIdentifyMusic.Location = new Point(432, 17);
            btnIdentifyMusic.Name = "btnIdentifyMusic";
            btnIdentifyMusic.Size = new Size(128, 38);
            btnIdentifyMusic.TabIndex = 3;
            btnIdentifyMusic.Text = "♫  Şarkıyı bul";
            btnIdentifyMusic.UseVisualStyleBackColor = false;
            btnIdentifyMusic.Click += btnIdentifyMusic_Click;
            // btnCancel
            btnCancel.BackColor = Color.FromArgb(48, 53, 72);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI Semibold", 9F);
            btnCancel.ForeColor = Color.FromArgb(220, 224, 235);
            btnCancel.Location = new Point(570, 17);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(52, 38);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Esc";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // lblInstructionSub
            lblInstructionSub.AutoSize = true;
            lblInstructionSub.Font = new Font("Segoe UI", 8.5F);
            lblInstructionSub.ForeColor = Color.FromArgb(157, 165, 187);
            lblInstructionSub.Location = new Point(24, 42);
            lblInstructionSub.Name = "lblInstructionSub";
            lblInstructionSub.Size = new Size(274, 15);
            lblInstructionSub.TabIndex = 1;
            lblInstructionSub.Text = "Fareyi sürükleyin · İptal etmek için Esc tuşuna basın";
            // lblInstruction
            lblInstruction.AutoSize = true;
            lblInstruction.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblInstruction.ForeColor = Color.White;
            lblInstruction.Location = new Point(22, 15);
            lblInstruction.Name = "lblInstruction";
            lblInstruction.Size = new Size(240, 20);
            lblInstruction.TabIndex = 0;
            lblInstruction.Text = "İşlem yapmak istediğiniz alanı seçin";
            // SelectionForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(960, 540);
            Controls.Add(panelInstruction);
            Cursor = Cursors.Cross;
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Name = "SelectionForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Alan seç";
            TopMost = true;
            KeyDown += SelectionForm_KeyDown;
            MouseDown += SelectionForm_MouseDown;
            MouseMove += SelectionForm_MouseMove;
            MouseUp += SelectionForm_MouseUp;
            Paint += SelectionForm_Paint;
            Resize += SelectionForm_Resize;
            panelInstruction.ResumeLayout(false);
            panelInstruction.PerformLayout();
            ResumeLayout(false);
        }

        private Panel panelInstruction;
        private Button btnIdentifyMusic;
        private Button btnCancel;
        private Label lblInstructionSub;
        private Label lblInstruction;
    }
}
