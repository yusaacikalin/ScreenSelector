using System.Diagnostics;

namespace ScreenSelector;

internal sealed class RecordingControlForm : Form
{
    private readonly Label _durationLabel;
    private readonly Button _stopButton;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _elapsed = new();

    internal RecordingControlForm(Rectangle selectedArea)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(25, 29, 43);
        ClientSize = new Size(316, 60);
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Text = "Ekran kaydı";

        var recordingLabel = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(14, 9),
            Size = new Size(168, 21),
            Text = "●  Kayıt yapılıyor"
        };
        recordingLabel.ForeColor = Color.FromArgb(255, 104, 116);

        _durationLabel = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(172, 180, 201),
            Location = new Point(32, 32),
            Size = new Size(118, 18),
            Text = "00:00:00"
        };

        _stopButton = new Button
        {
            BackColor = Color.FromArgb(237, 84, 99),
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(190, 9),
            Size = new Size(112, 42),
            Text = "■  Durdur",
            UseVisualStyleBackColor = false
        };
        _stopButton.FlatAppearance.BorderSize = 0;
        _stopButton.Click += (_, _) =>
        {
            _stopButton.Enabled = false;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(recordingLabel);
        Controls.Add(_durationLabel);
        Controls.Add(_stopButton);
        PositionOutsideSelection(selectedArea);

        _timer = new System.Windows.Forms.Timer { Interval = 250 };
        _timer.Tick += (_, _) => _durationLabel.Text = _elapsed.Elapsed.ToString(@"hh\:mm\:ss");
        Shown += RecordingControlForm_Shown;
        FormClosing += (_, _) => _timer.Stop();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode is not (Keys.Escape or Keys.F10)) return;
            _stopButton.PerformClick();
        };
    }

    private void RecordingControlForm_Shown(object? sender, EventArgs e)
    {
        // On supported Windows versions this keeps the controller itself out of
        // desktop-capture APIs if a full-screen selection leaves nowhere else to put it.
        NativeMethods.SetWindowDisplayAffinity(Handle, NativeMethods.WdaExcludeFromCapture);
        _elapsed.Start();
        _timer.Start();
    }

    private void PositionOutsideSelection(Rectangle selection)
    {
        const int gap = 10;
        var orderedScreens = Screen.AllScreens
            .OrderByDescending(screen => IntersectionArea(screen.WorkingArea, selection));

        foreach (var screen in orderedScreens)
        {
            var workingArea = screen.WorkingArea;
            var centeredX = Math.Clamp(selection.Left + (selection.Width - Width) / 2,
                workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - Width));
            var centeredY = Math.Clamp(selection.Top + (selection.Height - Height) / 2,
                workingArea.Top, Math.Max(workingArea.Top, workingArea.Bottom - Height));

            var candidates = new[]
            {
                new Point(centeredX, selection.Bottom + gap),
                new Point(centeredX, selection.Top - Height - gap),
                new Point(selection.Right + gap, centeredY),
                new Point(selection.Left - Width - gap, centeredY)
            };

            foreach (var candidate in candidates)
            {
                var bounds = new Rectangle(candidate, Size);
                if (workingArea.Contains(bounds) && !bounds.IntersectsWith(selection))
                {
                    Location = candidate;
                    return;
                }
            }
        }

        var fallback = Screen.FromRectangle(selection).WorkingArea;
        Location = new Point(
            Math.Max(fallback.Left, fallback.Right - Width - 10),
            fallback.Top + 10);
    }

    private static long IntersectionArea(Rectangle first, Rectangle second)
    {
        var intersection = Rectangle.Intersect(first, second);
        return (long)intersection.Width * intersection.Height;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _elapsed.Stop();
        }
        base.Dispose(disposing);
    }
}
