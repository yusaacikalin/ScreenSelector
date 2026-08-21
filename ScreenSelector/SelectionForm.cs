using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScreenSelector;

public partial class SelectionForm : Form
{
    private const int SelectionBorderPadding = 3;
    private const int SizeBadgeTopOffset = 27;
    private const int SizeBadgeHeight = 24;

    private AppSettings _settings = new();
    private Bitmap? _screenshot;
    private Bitmap? _dimmedScreenshot;
    private Point _dragStart;
    private Rectangle _selection;
    private bool _dragging;
    private bool _actionOpened;
    private readonly SolidBrush _sizeBackground = new(Color.FromArgb(225, 25, 29, 43));
    private readonly SolidBrush _sizeTextBrush = new(Color.White);
    private readonly Pen _lightDashes = new(Color.FromArgb(245, 248, 255), 1F)
    {
        DashStyle = DashStyle.Custom,
        DashPattern = [5F, 4F],
        DashCap = DashCap.Flat
    };
    private readonly Pen _darkDashes = new(Color.FromArgb(36, 39, 51), 1F)
    {
        DashStyle = DashStyle.Custom,
        DashPattern = [5F, 4F],
        DashOffset = 4.5F,
        DashCap = DashCap.Flat
    };

    public SelectionForm()
    {
        InitializeComponent();

        // OptimizedDoubleBuffer copies the entire virtual desktop for every mouse
        // move. Paint directly into the small invalid regions instead; the form is
        // opaque, so suppressing background erase still keeps the drawing flicker-free.
        DoubleBuffered = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
        UpdateStyles();
    }

    public SelectionForm(AppSettings settings) : this()
    {
        _settings = settings;
        Bounds = SystemInformation.VirtualScreen;
        CaptureDesktop();
        PositionInstruction();
    }

    private void CaptureDesktop()
    {
        var virtualScreen = SystemInformation.VirtualScreen;
        var screenshot = new Bitmap(virtualScreen.Width, virtualScreen.Height, PixelFormat.Format32bppPArgb);
        try
        {
            using (var graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(virtualScreen.Left, virtualScreen.Top, 0, 0, virtualScreen.Size,
                    CopyPixelOperation.SourceCopy);
            }

            // The dark overlay never changes, so blend it once instead of blending
            // millions of pixels again on every MouseMove.
            var dimmedScreenshot = new Bitmap(virtualScreen.Width, virtualScreen.Height,
                PixelFormat.Format32bppPArgb);
            try
            {
                using var graphics = Graphics.FromImage(dimmedScreenshot);
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.DrawImageUnscaled(screenshot, Point.Empty);
                using var shade = new SolidBrush(Color.FromArgb(135, 10, 12, 20));
                graphics.FillRectangle(shade, new Rectangle(Point.Empty, virtualScreen.Size));
            }
            catch
            {
                dimmedScreenshot.Dispose();
                throw;
            }

            _screenshot = screenshot;
            _dimmedScreenshot = dimmedScreenshot;
        }
        catch
        {
            screenshot.Dispose();
            throw;
        }
    }

    private void PositionInstruction()
    {
        panelInstruction.Left = Math.Max(20, (ClientSize.Width - panelInstruction.Width) / 2);
        panelInstruction.Top = 24;
    }

    private void SelectionForm_Paint(object? sender, PaintEventArgs e)
    {
        if (_screenshot == null || _dimmedScreenshot == null) return;

        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
        e.Graphics.DrawImageUnscaled(_dimmedScreenshot, Point.Empty);

        if (_selection.Width <= 0 || _selection.Height <= 0) return;

        var graphicsState = e.Graphics.Save();
        e.Graphics.SetClip(_selection, CombineMode.Intersect);
        e.Graphics.DrawImageUnscaled(_screenshot, Point.Empty);
        e.Graphics.Restore(graphicsState);

        var border = _selection;
        border.Width = Math.Max(1, border.Width - 1);
        border.Height = Math.Max(1, border.Height - 1);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.DrawRectangle(_darkDashes, border);
        e.Graphics.DrawRectangle(_lightDashes, border);

        if (!_dragging) return;

        var sizeText = $"{_selection.Width} × {_selection.Height}";
        var badge = GetSizeBadgeBounds(_selection);
        e.Graphics.FillRectangle(_sizeBackground, badge);
        e.Graphics.DrawString(sizeText, Font, _sizeTextBrush, badge.Left + 8, badge.Top + 4);
    }

    private void SelectionForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || panelInstruction.Bounds.Contains(e.Location)) return;
        _dragging = true;
        _dragStart = e.Location;
        _selection = Rectangle.Empty;
        panelInstruction.Visible = false;
    }

    private void SelectionForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var nextSelection = NormalizeRectangle(_dragStart, e.Location);
        if (nextSelection == _selection) return;

        var previousSelection = _selection;
        _selection = nextSelection;
        InvalidateSelectionChange(previousSelection, nextSelection, includeSizeBadges: true);
    }

    private void SelectionForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var previousSelection = _selection;
        _dragging = false;
        _selection = NormalizeRectangle(_dragStart, e.Location);

        if (_selection.Width < 8 || _selection.Height < 4)
        {
            _selection = Rectangle.Empty;
            panelInstruction.Visible = true;
            InvalidateSelectionChange(previousSelection, Rectangle.Empty, includeSizeBadges: true);
            return;
        }

        OpenActions(autoIdentifyMusic: false);
    }

    private static Rectangle NormalizeRectangle(Point start, Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        return new Rectangle(left, top, Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
    }

    private Rectangle GetSizeBadgeBounds(Rectangle selection)
    {
        var text = $"{selection.Width} × {selection.Height}";
        var textSize = TextRenderer.MeasureText(text, Font, Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        return new Rectangle(selection.Left, Math.Max(0, selection.Top - SizeBadgeTopOffset),
            textSize.Width + 16, SizeBadgeHeight);
    }

    private void InvalidateSelectionChange(Rectangle previous, Rectangle current, bool includeSizeBadges)
    {
        using var dirtyRegion = new Region();
        dirtyRegion.MakeEmpty();

        // Only pixels whose selected/unselected state changed need their image
        // restored. The old/new outlines and size badges are added separately.
        if (!previous.IsEmpty) dirtyRegion.Union(previous);
        if (!current.IsEmpty) dirtyRegion.Xor(current);
        AddSelectionOutline(dirtyRegion, previous);
        AddSelectionOutline(dirtyRegion, current);

        if (includeSizeBadges)
        {
            if (!previous.IsEmpty) dirtyRegion.Union(GetSizeBadgeBounds(previous));
            if (!current.IsEmpty) dirtyRegion.Union(GetSizeBadgeBounds(current));
        }

        Invalidate(dirtyRegion);
    }

    private static void AddSelectionOutline(Region dirtyRegion, Rectangle selection)
    {
        if (selection.IsEmpty) return;

        var outer = selection;
        outer.Inflate(SelectionBorderPadding, SelectionBorderPadding);
        using var outline = new Region(outer);

        var inner = selection;
        inner.Inflate(-SelectionBorderPadding, -SelectionBorderPadding);
        if (inner.Width > 0 && inner.Height > 0) outline.Exclude(inner);
        dirtyRegion.Union(outline);
    }

    private void DisposeDrawingResources()
    {
        _dimmedScreenshot?.Dispose();
        _lightDashes.Dispose();
        _darkDashes.Dispose();
        _sizeBackground.Dispose();
        _sizeTextBrush.Dispose();
    }

    private void OpenActions(bool autoIdentifyMusic)
    {
        if (_actionOpened || _screenshot == null) return;
        _actionOpened = true;

        var area = autoIdentifyMusic
            ? new Rectangle(Math.Max(0, ClientSize.Width / 2 - 1), Math.Max(0, ClientSize.Height / 2 - 1), 2, 2)
            : _selection;
        var captureArea = area;
        if (!autoIdentifyMusic)
        {
            var horizontalInset = area.Width > 4 ? 2 : 1;
            var verticalInset = area.Height > 4 ? 2 : 1;
            captureArea = Rectangle.FromLTRB(area.Left + horizontalInset, area.Top + verticalInset,
                area.Right - horizontalInset, area.Bottom - verticalInset);
        }

        var crop = new Bitmap(captureArea.Width, captureArea.Height);
        using (var graphics = Graphics.FromImage(crop))
            graphics.DrawImage(_screenshot, new Rectangle(Point.Empty, crop.Size), captureArea, GraphicsUnit.Pixel);

        Hide();
        using (crop)
        using (var actions = new ActionToolbarForm(crop, _settings, RectangleToScreen(area), autoIdentifyMusic))
            actions.ShowDialog();
        Close();
    }

    private void btnIdentifyMusic_Click(object? sender, EventArgs e) => OpenActions(autoIdentifyMusic: true);
    private void btnCancel_Click(object? sender, EventArgs e) => Close();

    private void SelectionForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Close();
    }

    private void SelectionForm_Resize(object? sender, EventArgs e) => PositionInstruction();
}
