namespace ScreenSelector;

internal sealed record InlineTranslationLayout(string Text, RectangleF SourceBounds, RectangleF TextBounds,
    Rectangle CoverBounds, Color Background, float FontSize, float? WrapWidth)
{
    internal bool Wrap => WrapWidth.HasValue;
}

internal static class InlineTranslationLayoutEngine
{
    internal const float MinimumFontSize = 11.5f;
    private const float Gap = 2f;

    internal static IReadOnlyList<InlineTranslationLayout> Arrange(Bitmap capture,
        IReadOnlyList<TranslatedTextLine> lines)
    {
        var image = new RectangleF(PointF.Empty, capture.Size);
        var sources = lines.Select(line => line with
            {
                Bounds = RectangleF.Intersect(image, line.Bounds), Text = Normalize(line.Text)
            }).Where(line => line.Bounds.Width > 0 && line.Bounds.Height > 0 && line.Text.Length > 0)
            .OrderBy(line => line.Bounds.Top).ThenBy(line => line.Bounds.Left).ToArray();
        var layouts = new List<InlineTranslationLayout>();
        var unchanged = sources.Where(IsUnchanged).Select(line => line.Bounds).ToArray();
        for (var index = 0; index < sources.Length; index++)
        {
            var source = sources[index];
            if (IsUnchanged(source)) continue;
            var background = InlineTranslationSurface.SampleBackground(capture, Cover(source.Bounds, capture.Size));
            var maximumFont = EstimateSourceFont(source);
            var fontSize = FitOriginalLine(source.Text, source.Bounds.Size, maximumFont);
            if (fontSize >= MinimumFontSize)
            {
                // First choice is always the original line, with no expansion
                // and no enlargement of an already readable translation.
                var ink = InlineTextMetrics.Measure(source.Text, fontSize);
                var textBounds = new RectangleF(source.Bounds.Location, ink.Size);
                layouts.Add(CreateLayout(source.Text, source.Bounds, textBounds, background, fontSize, null,
                    capture.Size));
                continue;
            }

            var reserved = layouts.Select(layout => (RectangleF)layout.CoverBounds).Concat(unchanged).ToArray();
            var consumed = 1;
            InlineTranslationLayout? expanded = null;
            while (true)
            {
                var group = sources.Skip(index).Take(consumed).ToArray();
                var sourceBounds = group.Select(line => line.Bounds).Aggregate(RectangleF.Union);
                var text = string.Join(' ', group.Select(line => line.Text));
                var obstacles = reserved.Concat(sources.Skip(index + consumed).Select(line => line.Bounds)).ToArray();
                expanded = TryExpand(capture, text, sourceBounds, group.Select(line => line.Bounds).ToArray(),
                    obstacles, background);
                if (expanded != null) break;

                // Only reflow a genuine continuation when it is blocking a
                // needed extra line. Menu items and separate titles stay apart.
                var nextIndex = index + consumed;
                if (nextIndex >= sources.Length || IsUnchanged(sources[nextIndex]) ||
                    !IsContinuation(group[^1], sources[nextIndex])) break;
                var nextBackground = InlineTranslationSurface.SampleBackground(capture,
                    Cover(sources[nextIndex].Bounds, capture.Size));
                if (!SameBackground(nextBackground, background)) break;
                consumed++;
            }
            if (expanded == null)
                throw new InvalidOperationException(
                    "Çeviri, diğer metinleri kapatmadan bu alana sığmıyor. Biraz daha geniş bir alan seçin.");
            layouts.Add(expanded);
            index += consumed - 1;
        }
        return layouts;
    }

    private static float EstimateSourceFont(TranslatedTextLine source)
    {
        var hasSource = !string.IsNullOrWhiteSpace(source.SourceText);
        var ink = InlineTextMetrics.Measure(hasSource ? source.SourceText! : "Hg", 100);
        var byHeight = source.Bounds.Height * 100 / Math.Max(1, ink.Height);
        var byWidth = hasSource ? source.Bounds.Width * 100 / Math.Max(1, ink.Width) : byHeight;
        return Math.Min(byHeight, byWidth);
    }

    private static float FitOriginalLine(string text, SizeF available, float maximum)
    {
        var low = 0.1f;
        var high = Math.Max(low, maximum);
        for (var iteration = 0; iteration < 18; iteration++)
        {
            var size = (low + high) / 2;
            var ink = InlineTextMetrics.Measure(text, size);
            if (ink.Width <= available.Width && ink.Height <= available.Height) low = size;
            else high = size;
        }
        return low;
    }

    private static InlineTranslationLayout? TryExpand(Bitmap capture, string text, RectangleF source,
        IReadOnlyList<RectangleF> originals, IReadOnlyList<RectangleF> obstacles, Color background)
    {
        // Expansion is only reached after the original line would need an
        // unreadably small font. Use the floor, not a heading/row-derived size.
        var ink = InlineTextMetrics.Measure(text, MinimumFontSize);
        var bandBottom = Math.Max(source.Bottom, source.Top + ink.Height);
        var right = Math.Min(capture.Width, source.Right + Math.Max(32, originals[0].Height * 3));
        foreach (var obstacle in obstacles)
            if (Overlap(source.Top, bandBottom, obstacle.Top, obstacle.Bottom) && obstacle.Left >= source.Right)
                right = Math.Min(right, obstacle.Left - Gap);
        right = Math.Max(source.Right, right);
        for (var x = (int)Math.Ceiling(source.Right); x < Math.Floor(right); x++)
        {
            if (Blank(capture, new RectangleF(x, source.Top, 1, bandBottom - source.Top), originals, background)) continue;
            right = x;
            break;
        }

        var left = source.Left;
        if (source.Width <= originals[0].Height * 6 && obstacles.Any(obstacle =>
                Overlap(source.Top, bandBottom, obstacle.Top, obstacle.Bottom)))
        {
            // Tiny menu labels can share a little whitespace on both sides.
            // Keep the baseline and never move by more than a glyph height.
            left = Math.Max(0, source.Left - Math.Min(originals[0].Height,
                Math.Max(0, (ink.Width - source.Width) / 2)));
            foreach (var obstacle in obstacles)
                if (Overlap(source.Top, bandBottom, obstacle.Top, obstacle.Bottom) && obstacle.Right <= source.Left)
                    left = Math.Max(left, obstacle.Right + Gap);
        }
        var availableWidth = right - source.Left;
        var single = new RectangleF(left, source.Top, ink.Width, ink.Height);
        if (single.Right <= right && single.Bottom <= capture.Height &&
            Free(single, obstacles) && Blank(capture, single, originals, background))
            return CreateLayout(text, source, single, background, MinimumFontSize, null, capture.Size);

        // Keep the first line anchored and use only safe whitespace to avoid
        // needless line breaks. Additional lines grow down, never up.
        foreach (var width in new[] { availableWidth, source.Width }.Distinct())
        {
            if (width <= 0) continue;
            var wrappedInk = InlineTextMetrics.Measure(text, MinimumFontSize, width);
            var wrapped = new RectangleF(source.Location, wrappedInk.Size);
            if (wrapped.Width > width + 0.1f || wrapped.Bottom > capture.Height || !Free(wrapped, obstacles) ||
                !Blank(capture, wrapped, originals, background)) continue;
            return CreateLayout(text, source, wrapped, background, MinimumFontSize, width, capture.Size);
        }
        return null;
    }

    private static bool Blank(Bitmap capture, RectangleF area, IReadOnlyList<RectangleF> originals, Color background)
    {
        var rectangle = Rectangle.Intersect(new Rectangle(Point.Empty, capture.Size), RoundOut(area));
        for (var y = rectangle.Top; y < rectangle.Bottom; y++)
        for (var x = rectangle.Left; x < rectangle.Right; x++)
        {
            if (originals.Any(original => RectangleF.Inflate(original, 1, 1).Contains(x + 0.5f, y + 0.5f))) continue;
            if (!SameBackground(capture.GetPixel(x, y), background)) return false;
        }
        return true;
    }

    private static bool Free(RectangleF rectangle, IReadOnlyList<RectangleF> obstacles) =>
        obstacles.All(obstacle => !RectangleF.Inflate(obstacle, Gap, Gap).IntersectsWith(rectangle));

    private static bool IsContinuation(TranslatedTextLine first, TranslatedTextLine next)
    {
        if (first.SourceText == null || first.SourceText.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries).Length < 3 || first.Bounds.Width < first.Bounds.Height * 8)
            return false;
        var firstLetter = next.SourceText?.FirstOrDefault(char.IsLetter) ?? '\0';
        if (!char.IsLower(firstLetter)) return false;
        var a = first.Bounds;
        var b = next.Bounds;
        var aligned = Math.Abs(a.Left - b.Left) <= 5 ||
            Math.Abs(a.Left + a.Width / 2 - b.Left - b.Width / 2) <= 5;
        return aligned && b.Top >= a.Bottom && b.Top - a.Bottom <= a.Height &&
            b.Height >= a.Height * 0.75f && b.Height <= a.Height * 1.33f;
    }

    private static InlineTranslationLayout CreateLayout(string text, RectangleF source, RectangleF bounds,
        Color background, float size, float? wrapWidth, Size imageSize) =>
        new(text, source, bounds, Cover(RectangleF.Union(source, bounds), imageSize), background, size, wrapWidth);

    private static Rectangle Cover(RectangleF bounds, Size size) => Rectangle.Intersect(
        new Rectangle(Point.Empty, size), Rectangle.Inflate(RoundOut(bounds), 1, 1));

    private static Rectangle RoundOut(RectangleF bounds) => Rectangle.FromLTRB((int)Math.Floor(bounds.Left),
        (int)Math.Floor(bounds.Top), (int)Math.Ceiling(bounds.Right), (int)Math.Ceiling(bounds.Bottom));

    private static bool SameBackground(Color first, Color second) => Math.Abs(first.R - second.R) <= 32 &&
        Math.Abs(first.G - second.G) <= 32 && Math.Abs(first.B - second.B) <= 32;

    private static bool Overlap(float firstStart, float firstEnd, float secondStart, float secondEnd) =>
        firstStart < secondEnd && secondStart < firstEnd;

    private static string Normalize(string text) => string.Join(' ', text.Split((char[]?)null,
        StringSplitOptions.RemoveEmptyEntries));

    private static bool IsUnchanged(TranslatedTextLine line) =>
        line.SourceText != null && Normalize(line.SourceText) == line.Text;
}
