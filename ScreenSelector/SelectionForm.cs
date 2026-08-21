using System.Drawing.Drawing2D;

namespace ScreenSelector;

public partial class SelectionForm : Form
{
    private AppSettings _settings = new();
    private Bitmap? _screenshot;
    private Point _dragStart;
    private Rectangle _selection;
    private bool _dragging;
    private bool _actionOpened;

    public SelectionForm()
    {
        InitializeComponent();
        DoubleBuffered = true;
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
        _screenshot = new Bitmap(virtualScreen.Width, virtualScreen.Height);
        using var graphics = Graphics.FromImage(_screenshot);
        graphics.CopyFromScreen(virtualScreen.Left, virtualScreen.Top, 0, 0, virtualScreen.Size,
            CopyPixelOperation.SourceCopy);
    }

    private void PositionInstruction()
    {
        panelInstruction.Left = Math.Max(20, (ClientSize.Width - panelInstruction.Width) / 2);
        panelInstruction.Top = 24;
    }

    private void SelectionForm_Paint(object? sender, PaintEventArgs e)
    {
        if (_screenshot == null) return;

        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.DrawImageUnscaled(_screenshot, Point.Empty);
        using var shade = new SolidBrush(Color.FromArgb(135, 10, 12, 20));
        e.Graphics.FillRectangle(shade, ClientRectangle);

        if (_selection.Width <= 0 || _selection.Height <= 0) return;

        e.Graphics.SetClip(_selection);
        e.Graphics.DrawImageUnscaled(_screenshot, Point.Empty);
        e.Graphics.ResetClip();

        var border = _selection;
        border.Width = Math.Max(1, border.Width - 1);
        border.Height = Math.Max(1, border.Height - 1);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        using var lightDashes = new Pen(Color.FromArgb(245, 248, 255), 1F)
        {
            DashStyle = DashStyle.Custom,
            DashPattern = new[] { 5F, 4F },
            DashCap = DashCap.Flat
        };
        using var darkDashes = new Pen(Color.FromArgb(36, 39, 51), 1F)
        {
            DashStyle = DashStyle.Custom,
            DashPattern = new[] { 5F, 4F },
            DashOffset = 4.5F,
            DashCap = DashCap.Flat
        };
        e.Graphics.DrawRectangle(darkDashes, border);
        e.Graphics.DrawRectangle(lightDashes, border);

        if (!_dragging) return;

        using var sizeBackground = new SolidBrush(Color.FromArgb(225, 25, 29, 43));
        var sizeText = $"{_selection.Width} × {_selection.Height}";
        var textSize = e.Graphics.MeasureString(sizeText, Font);
        var badge = new RectangleF(_selection.Left, Math.Max(0, _selection.Top - 27), textSize.Width + 16, 24);
        e.Graphics.FillRectangle(sizeBackground, badge);
        using var textBrush = new SolidBrush(Color.White);
        e.Graphics.DrawString(sizeText, Font, textBrush, badge.Left + 8, badge.Top + 4);
    }

    private void SelectionForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || panelInstruction.Bounds.Contains(e.Location)) return;
        _dragging = true;
        _dragStart = e.Location;
        _selection = Rectangle.Empty;
        panelInstruction.Visible = false;
        Invalidate();
    }

    private void SelectionForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        _selection = NormalizeRectangle(_dragStart, e.Location);
        Invalidate();
    }

    private void SelectionForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        _selection = NormalizeRectangle(_dragStart, e.Location);

        if (_selection.Width < 8 || _selection.Height < 4)
        {
            _selection = Rectangle.Empty;
            panelInstruction.Visible = true;
            Invalidate();
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
