using System.Drawing.Drawing2D;

namespace ScreenSelector;

internal partial class SelectionForm : Form
{
    private const int SelectionBorderPadding = 3;

    private SelectionSession? _session;
    private Rectangle _screenBounds;
    private Rectangle _selection;
    private Rectangle _regionHole;
    private bool _dragging;
    private readonly Pen _selectionBorder = new(Color.White, 1F)
    {
        DashStyle = DashStyle.Dash,
        DashPattern = [5F, 4F],
        DashCap = DashCap.Flat
    };

    public SelectionForm()
    {
        InitializeComponent();

        // The overlay is just a solid translucent surface composed by DWM. It
        // does not allocate or repaint a desktop-sized bitmap while dragging.
        DoubleBuffered = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
        UpdateStyles();
    }

    internal SelectionForm(SelectionSession session, Screen screen, bool showInstruction) : this()
    {
        _session = session;
        _screenBounds = screen.Bounds;
        Bounds = screen.Bounds;
        Opacity = 0.52D;
        panelInstruction.Visible = showInstruction;
        PositionInstruction();
    }

    internal Rectangle ScreenBounds => _screenBounds;

    private void PositionInstruction()
    {
        panelInstruction.Left = Math.Max(20, (ClientSize.Width - panelInstruction.Width) / 2);
        panelInstruction.Top = 24;
    }

    private void SelectionForm_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.FillRectangle(Brushes.Black, e.ClipRectangle);
        if (!HasArea(_selection)) return;

        var border = _selection;
        border.Width = Math.Max(1, border.Width - 1);
        border.Height = Math.Max(1, border.Height - 1);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.DrawRectangle(_selectionBorder, border);
    }

    internal void ApplySelection(Rectangle previousScreenSelection, Rectangle currentScreenSelection)
    {
        var previous = TranslateToClient(previousScreenSelection);
        var current = TranslateToClient(currentScreenSelection);
        _selection = current;

        UpdateWindowRegion(current);
        InvalidateSelectionChange(previous, current);
    }

    internal void SetInstructionVisible(bool visible) => panelInstruction.Visible = visible;

    private Rectangle TranslateToClient(Rectangle screenRectangle) => new(
        screenRectangle.X - _screenBounds.X,
        screenRectangle.Y - _screenBounds.Y,
        screenRectangle.Width,
        screenRectangle.Height);

    private void UpdateWindowRegion(Rectangle selection)
    {
        var visibleSelection = Rectangle.Intersect(selection, ClientRectangle);
        var hole = GetHoleRectangle(selection, visibleSelection);
        if (hole == _regionHole) return;
        _regionHole = hole;

        if (!HasArea(hole))
        {
            NativeMethods.SetWindowRgn(Handle, IntPtr.Zero, false);
            return;
        }

        var windowRegion = NativeMethods.CreateRectRgn(0, 0, ClientSize.Width, ClientSize.Height);
        var holeRegion = NativeMethods.CreateRectRgn(hole.Left, hole.Top, hole.Right, hole.Bottom);
        if (windowRegion == IntPtr.Zero || holeRegion == IntPtr.Zero)
        {
            if (windowRegion != IntPtr.Zero) NativeMethods.DeleteObject(windowRegion);
            if (holeRegion != IntPtr.Zero) NativeMethods.DeleteObject(holeRegion);
            return;
        }

        NativeMethods.CombineRgn(windowRegion, windowRegion, holeRegion, NativeMethods.RgnDiff);
        NativeMethods.DeleteObject(holeRegion);

        // On success Windows owns windowRegion. Redraw is intentionally disabled;
        // only the thin changed strips are invalidated below.
        if (NativeMethods.SetWindowRgn(Handle, windowRegion, false) == 0)
            NativeMethods.DeleteObject(windowRegion);
    }

    private Rectangle GetHoleRectangle(Rectangle selection, Rectangle visibleSelection)
    {
        if (!HasArea(visibleSelection)) return Rectangle.Empty;

        var hole = visibleSelection;

        // Keep one pixel of the overlay only on the selection's real outer edges.
        // Do not add a line at monitor seams when a selection spans displays.
        if (selection.Left >= ClientRectangle.Left)
        {
            hole.X++;
            hole.Width--;
        }
        if (selection.Top >= ClientRectangle.Top)
        {
            hole.Y++;
            hole.Height--;
        }
        if (selection.Right <= ClientRectangle.Right) hole.Width--;
        if (selection.Bottom <= ClientRectangle.Bottom) hole.Height--;

        return HasArea(hole) ? hole : Rectangle.Empty;
    }

    private void SelectionForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || panelInstruction.Visible && panelInstruction.Bounds.Contains(e.Location))
            return;

        if (_session?.BeginSelection(this, PointToScreen(e.Location)) != true) return;
        _dragging = true;
        Capture = true;
    }

    private void SelectionForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragging) _session?.UpdateSelection(this, PointToScreen(e.Location));
    }

    private void SelectionForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        var end = PointToScreen(e.Location);
        _dragging = false;
        Capture = false;
        _session?.CompleteSelection(this, end);
    }

    private void InvalidateSelectionChange(Rectangle previous, Rectangle current)
    {
        var previousVisible = Rectangle.Intersect(previous, ClientRectangle);
        var currentVisible = Rectangle.Intersect(current, ClientRectangle);

        // SetWindowRgn clips these rectangles to the solid overlay. During a
        // normal expanding drag this leaves only a few border pixels to repaint;
        // no managed Region or full-screen bitmap is created per mouse event.
        if (HasArea(previousVisible))
        {
            previousVisible.Inflate(SelectionBorderPadding, SelectionBorderPadding);
            Invalidate(previousVisible);
        }
        if (HasArea(currentVisible))
        {
            currentVisible.Inflate(SelectionBorderPadding, SelectionBorderPadding);
            Invalidate(currentVisible);
        }
    }

    private static bool HasArea(Rectangle rectangle) => rectangle.Width > 0 && rectangle.Height > 0;

    private void DisposeDrawingResources() => _selectionBorder.Dispose();

    private void btnIdentifyMusic_Click(object? sender, EventArgs e) => _session?.OpenActions(autoIdentifyMusic: true);
    private void btnCancel_Click(object? sender, EventArgs e) => _session?.Cancel();

    private void SelectionForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) _session?.Cancel();
    }

    private void SelectionForm_Resize(object? sender, EventArgs e) => PositionInstruction();
}
