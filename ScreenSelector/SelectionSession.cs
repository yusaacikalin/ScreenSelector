namespace ScreenSelector;

internal sealed class SelectionSession : IDisposable
{
    private readonly AppSettings _settings;
    private readonly List<SelectionForm> _forms = [];
    private readonly SelectionForm _instructionForm;
    private Point _dragStart;
    private Rectangle _selection;
    private SelectionForm? _dragOwner;
    private bool _shown;
    private bool _finished;
    private bool _disposed;

    internal SelectionSession(AppSettings settings)
    {
        _settings = settings;
        var instructionScreen = Screen.FromPoint(Cursor.Position);

        try
        {
            foreach (var screen in Screen.AllScreens)
            {
                var form = new SelectionForm(this, screen,
                    string.Equals(screen.DeviceName, instructionScreen.DeviceName,
                        StringComparison.OrdinalIgnoreCase));
                _forms.Add(form);
            }

            _instructionForm = _forms.First(form => form.ScreenBounds == instructionScreen.Bounds);
        }
        catch
        {
            foreach (var form in _forms) form.Dispose();
            throw;
        }
    }

    internal event EventHandler? Closed;

    internal void Show()
    {
        if (_shown || _disposed) return;
        _shown = true;

        // Show the instruction monitor last so the keyboard focus lands there.
        foreach (var form in _forms.Where(form => form != _instructionForm)) form.Show();
        _instructionForm.Show();
        _instructionForm.Activate();
    }

    internal bool BeginSelection(SelectionForm owner, Point screenPoint)
    {
        if (_finished || _dragOwner != null) return false;

        _dragOwner = owner;
        _dragStart = screenPoint;
        SetSelection(Rectangle.Empty);
        foreach (var form in _forms) form.SetInstructionVisible(false);
        return true;
    }

    internal void UpdateSelection(SelectionForm owner, Point screenPoint)
    {
        if (_finished || _dragOwner != owner) return;
        SetSelection(NormalizeRectangle(_dragStart, screenPoint));
    }

    internal void CompleteSelection(SelectionForm owner, Point screenPoint)
    {
        if (_finished || _dragOwner != owner) return;

        _dragOwner = null;
        SetSelection(NormalizeRectangle(_dragStart, screenPoint));
        if (_selection.Width < 8 || _selection.Height < 4)
        {
            SetSelection(Rectangle.Empty);
            _instructionForm.SetInstructionVisible(true);
            _instructionForm.Activate();
            return;
        }

        OpenActions(autoIdentifyMusic: false, screenPoint);
    }

    internal void OpenActions(bool autoIdentifyMusic, Point? selectionEnd = null)
    {
        if (_finished) return;

        try
        {
            var area = autoIdentifyMusic
                ? new Rectangle(_instructionForm.ScreenBounds.Left + _instructionForm.ScreenBounds.Width / 2 - 1,
                    _instructionForm.ScreenBounds.Top + _instructionForm.ScreenBounds.Height / 2 - 1, 2, 2)
                : _selection;
            var captureArea = GetCaptureArea(area, autoIdentifyMusic);

            foreach (var form in _forms) form.Hide();
            NativeMethods.DwmFlush();

            using var crop = new Bitmap(captureArea.Width, captureArea.Height);
            using (var graphics = Graphics.FromImage(crop))
            {
                graphics.CopyFromScreen(captureArea.Location, Point.Empty, captureArea.Size,
                    CopyPixelOperation.SourceCopy);
            }

            using var actions = new ActionToolbarForm(crop, _settings, area, autoIdentifyMusic, selectionEnd);
            actions.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Seçilen alan işlenemedi.\n\n{ex.Message}", "ScreenSelector",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Finish();
        }
    }

    internal void Cancel() => Finish();

    private void SetSelection(Rectangle selection)
    {
        if (_selection == selection) return;

        var previous = _selection;
        _selection = selection;
        foreach (var form in _forms) form.ApplySelection(previous, selection);
    }

    private static Rectangle NormalizeRectangle(Point start, Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        return new Rectangle(left, top, Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
    }

    private static Rectangle GetCaptureArea(Rectangle area, bool autoIdentifyMusic)
    {
        if (autoIdentifyMusic) return area;

        var horizontalInset = area.Width > 4 ? 2 : 1;
        var verticalInset = area.Height > 4 ? 2 : 1;
        return Rectangle.FromLTRB(area.Left + horizontalInset, area.Top + verticalInset,
            area.Right - horizontalInset, area.Bottom - verticalInset);
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;

        foreach (var form in _forms)
        {
            if (!form.IsDisposed) form.Close();
        }

        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var form in _forms) form.Dispose();
        _forms.Clear();
    }
}
