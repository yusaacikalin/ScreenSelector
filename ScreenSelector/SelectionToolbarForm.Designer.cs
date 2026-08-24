namespace ScreenSelector;

partial class SelectionToolbarForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        panelDragSurface = new Panel();
        lblDragGrip = new Label();
        lblInstruction = new Label();
        lblInstructionSub = new Label();
        btnIdentifyMusic = new Button();
        btnCancel = new Button();
        panelDragSurface.SuspendLayout();
        SuspendLayout();
        // panelDragSurface
        panelDragSurface.BackColor = Color.FromArgb(27, 31, 46);
        panelDragSurface.Controls.Add(lblInstructionSub);
        panelDragSurface.Controls.Add(lblInstruction);
        panelDragSurface.Controls.Add(lblDragGrip);
        panelDragSurface.Cursor = Cursors.SizeAll;
        panelDragSurface.Location = new Point(0, 0);
        panelDragSurface.Name = "panelDragSurface";
        panelDragSurface.Size = new Size(354, 44);
        panelDragSurface.TabIndex = 0;
        // lblDragGrip
        lblDragGrip.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblDragGrip.ForeColor = Color.FromArgb(121, 130, 154);
        lblDragGrip.Location = new Point(10, 10);
        lblDragGrip.Name = "lblDragGrip";
        lblDragGrip.Size = new Size(24, 24);
        lblDragGrip.TabIndex = 0;
        lblDragGrip.Text = "⋮⋮";
        lblDragGrip.TextAlign = ContentAlignment.MiddleCenter;
        // lblInstruction
        lblInstruction.AutoSize = true;
        lblInstruction.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        lblInstruction.ForeColor = Color.White;
        lblInstruction.Location = new Point(41, 13);
        lblInstruction.Name = "lblInstruction";
        lblInstruction.Size = new Size(70, 17);
        lblInstruction.TabIndex = 1;
        lblInstruction.Text = "Alanı seçin";
        // lblInstructionSub
        lblInstructionSub.AutoSize = true;
        lblInstructionSub.Font = new Font("Segoe UI", 8.5F);
        lblInstructionSub.ForeColor = Color.FromArgb(157, 165, 187);
        lblInstructionSub.Location = new Point(126, 14);
        lblInstructionSub.Name = "lblInstructionSub";
        lblInstructionSub.Size = new Size(101, 15);
        lblInstructionSub.TabIndex = 2;
        lblInstructionSub.Text = "Fareyi sürükleyin";
        // btnIdentifyMusic
        btnIdentifyMusic.BackColor = Color.FromArgb(106, 92, 255);
        btnIdentifyMusic.Cursor = Cursors.Hand;
        btnIdentifyMusic.FlatAppearance.BorderSize = 0;
        btnIdentifyMusic.FlatStyle = FlatStyle.Flat;
        btnIdentifyMusic.Font = new Font("Segoe UI Semibold", 8.5F);
        btnIdentifyMusic.ForeColor = Color.White;
        btnIdentifyMusic.Location = new Point(360, 6);
        btnIdentifyMusic.Name = "btnIdentifyMusic";
        btnIdentifyMusic.Size = new Size(126, 32);
        btnIdentifyMusic.TabIndex = 1;
        btnIdentifyMusic.Text = "♫  Şarkıyı bul";
        btnIdentifyMusic.UseVisualStyleBackColor = false;
        btnIdentifyMusic.Click += btnIdentifyMusic_Click;
        // btnCancel
        btnCancel.BackColor = Color.FromArgb(48, 53, 72);
        btnCancel.Cursor = Cursors.Hand;
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.Font = new Font("Segoe UI Semibold", 8.5F);
        btnCancel.ForeColor = Color.FromArgb(220, 224, 235);
        btnCancel.Location = new Point(492, 6);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(42, 32);
        btnCancel.TabIndex = 2;
        btnCancel.Text = "Esc";
        btnCancel.UseVisualStyleBackColor = false;
        btnCancel.Click += btnCancel_Click;
        // SelectionToolbarForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(27, 31, 46);
        ClientSize = new Size(540, 44);
        Controls.Add(btnCancel);
        Controls.Add(btnIdentifyMusic);
        Controls.Add(panelDragSurface);
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SelectionToolbarForm";
        Opacity = 1D;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Seçim araçları";
        TopMost = true;
        KeyDown += SelectionToolbarForm_KeyDown;
        panelDragSurface.ResumeLayout(false);
        panelDragSurface.PerformLayout();
        ResumeLayout(false);
    }

    private Panel panelDragSurface;
    private Label lblDragGrip;
    private Label lblInstruction;
    private Label lblInstructionSub;
    private Button btnIdentifyMusic;
    private Button btnCancel;
}
