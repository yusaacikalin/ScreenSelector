using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScreenSelector;

internal sealed class InlineTranslationSurface : IDisposable
{
    internal Bitmap Image { get; }
    internal Region VisibleRegion { get; }
    internal IReadOnlyList<InlineTranslationLayout> Layouts { get; }

    internal InlineTranslationSurface(Bitmap capture, IReadOnlyList<TranslatedTextLine> lines)
    {
        Image = new Bitmap(capture.Width, capture.Height, PixelFormat.Format32bppArgb);
        VisibleRegion = new Region();
        VisibleRegion.MakeEmpty();
        try
        {
            using var graphics = Graphics.FromImage(Image);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            Layouts = InlineTranslationLayoutEngine.Arrange(capture, lines);
            foreach (var layout in Layouts)
            {
                using var backgroundBrush = new SolidBrush(layout.Background);
                graphics.FillRectangle(backgroundBrush, layout.CoverBounds);
                VisibleRegion.Union(layout.CoverBounds);
            }
            foreach (var layout in Layouts)
            {
                using var path = InlineTextMetrics.CreatePath(layout.Text, layout.FontSize, layout.WrapWidth);
                var ink = path.GetBounds();
                using var transform = new Matrix();
                transform.Translate(layout.TextBounds.Left - ink.Left, layout.TextBounds.Top - ink.Top);
                path.Transform(transform);
                using var foregroundBrush = new SolidBrush(GetTextColor(layout.Background));
                var state = graphics.Save();
                graphics.SetClip(layout.CoverBounds, CombineMode.Intersect);
                graphics.FillPath(foregroundBrush, path);
                graphics.Restore(state);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal static StringFormat CreateTextFormat(string text, bool wrap = false)
    {
        var format = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.MeasureTrailingSpaces | (wrap ? 0 : StringFormatFlags.NoWrap),
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.None
        };
        // Preserve natural alignment for translations into Hebrew/Arabic.
        var firstLetter = text.FirstOrDefault(char.IsLetter);
        if (firstLetter is >= '\u0590' and <= '\u08ff')
            format.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
        return format;
    }

    internal static Color SampleBackground(Bitmap capture, Rectangle bounds)
    {
        // Quantize RGB, not just brightness: adjacent colored panels can have
        // identical luminance. The dominant local color excludes most glyphs.
        var buckets = new Dictionary<int, int>();
        var colors = new Dictionary<int, int>();
        var step = Math.Max(1, (int)Math.Sqrt((double)bounds.Width * bounds.Height / 12000));
        for (var y = bounds.Top; y < bounds.Bottom; y += step)
        for (var x = bounds.Left; x < bounds.Right; x += step)
        {
            var color = capture.GetPixel(x, y);
            var bucket = ColorBucket(color);
            buckets[bucket] = buckets.GetValueOrDefault(bucket) + 1;
            colors[color.ToArgb()] = colors.GetValueOrDefault(color.ToArgb()) + 1;
        }
        if (colors.Count == 0) return Color.White;
        var dominant = buckets.MaxBy(pair => pair.Value).Key;
        // Use an actual pixel color so antialiased edges do not tint a flat background.
        return Color.FromArgb(colors.Where(pair => ColorBucket(Color.FromArgb(pair.Key)) == dominant)
            .MaxBy(pair => pair.Value).Key);
    }

    private static int ColorBucket(Color color) =>
        (color.R >> 4) << 8 | (color.G >> 4) << 4 | (color.B >> 4);

    private static Color GetTextColor(Color background) =>
        (background.R * 299 + background.G * 587 + background.B * 114) / 1000 < 145
            ? Color.White : Color.FromArgb(20, 20, 20);

    public void Dispose()
    {
        Image.Dispose();
        VisibleRegion.Dispose();
    }
}
