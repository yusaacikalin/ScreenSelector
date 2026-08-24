namespace ScreenSelector;

internal partial class SelectionToolbarForm : Form
{
    private readonly SelectionSession _session;

    internal SelectionToolbarForm(SelectionSession session, Screen screen)
    {
        _session = session;
        InitializeComponent();
        ModernWindowBehavior.EnableDragging(this, panelDragSurface);

        var workingArea = screen.WorkingArea;
        Location = new Point(
            workingArea.Left + Math.Max(0, (workingArea.Width - Width) / 2),
            workingArea.Top);
    }

    private void btnIdentifyMusic_Click(object? sender, EventArgs e) =>
        _session.OpenActions(autoIdentifyMusic: true);

    private void btnCancel_Click(object? sender, EventArgs e) => _session.Cancel();

    private void SelectionToolbarForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) _session.Cancel();
    }
}
