using System.Drawing.Drawing2D;

namespace ScreenSelector;

internal static class InlineTextMetrics
{
    // OCR gives glyph bounds, not the font's line box. Measure and draw the
    // same outlines so ascender padding never changes the apparent font size.
    internal static GraphicsPath CreatePath(string text, float fontSize, float? wrapWidth = null)
    {
        var path = new GraphicsPath();
        try
        {
            using var family = new FontFamily("Segoe UI");
            using var format = InlineTranslationSurface.CreateTextFormat(text, wrapWidth.HasValue);
            format.Alignment = StringAlignment.Near;
            format.LineAlignment = StringAlignment.Near;
            path.AddString(text, family, (int)FontStyle.Regular, fontSize,
                new RectangleF(0, 0, wrapWidth ?? 100000, 100000), format);
            return path;
        }
        catch
        {
            path.Dispose();
            throw;
        }
    }

    internal static RectangleF Measure(string text, float fontSize, float? wrapWidth = null)
    {
        using var path = CreatePath(text, fontSize, wrapWidth);
        return path.GetBounds();
    }
}
