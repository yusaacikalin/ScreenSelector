namespace ScreenSelector;

internal sealed class RecordingCountdownForm : Form
{
    private readonly Label _countdownLabel;
    private readonly System.Windows.Forms.Timer _timer;
    private int _remaining = 3;

    internal RecordingCountdownForm(Rectangle selectedArea)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(23, 27, 40);
        ClientSize = new Size(116, 116);
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Text = "Kayıt geri sayımı";

        _countdownLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 48F, FontStyle.Bold),
            ForeColor = Color.White,
            Text = "3",
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(_countdownLabel);

        var center = new Point(
            selectedArea.Left + selectedArea.Width / 2,
            selectedArea.Top + selectedArea.Height / 2);
        var screen = Screen.FromPoint(center).WorkingArea;
        Location = new Point(
            Math.Clamp(center.X - Width / 2, screen.Left, Math.Max(screen.Left, screen.Right - Width)),
            Math.Clamp(center.Y - Height / 2, screen.Top, Math.Max(screen.Top, screen.Bottom - Height)));

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += Timer_Tick;
        Shown += (_, _) => _timer.Start();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Escape) return;
            DialogResult = DialogResult.Cancel;
            Close();
        };
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remaining--;
        if (_remaining > 0)
        {
            _countdownLabel.Text = _remaining.ToString();
            return;
        }

        _timer.Stop();
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _countdownLabel.Dispose();
        }
        base.Dispose(disposing);
    }
}
