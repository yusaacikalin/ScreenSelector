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
            SuspendLayout();
            // SelectionForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(960, 540);
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
            ResumeLayout(false);
        }
    }
}
