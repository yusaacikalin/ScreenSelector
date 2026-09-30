namespace ScreenSelector;

internal sealed record OcrTextLine(string Text, RectangleF Bounds)
{
    internal static IEnumerable<OcrTextLine> SplitAtLargeGaps(IEnumerable<OcrTextLine> words)
    {
        var wordList = words.ToArray();
        if (wordList.Length == 0) yield break;
        var heights = wordList.Select(word => word.Bounds.Height).Order().ToArray();
        // UI labels often have a gap of one character height, while ordinary
        // word spaces are much smaller. Work in original capture pixels so
        // preprocessing scale and a tall icon cannot inflate the threshold.
        var maximumWordGap = Math.Max(3f, heights[heights.Length / 2] * 0.9f);
        var text = new List<string>();
        var bounds = RectangleF.Empty;
        var previous = RectangleF.Empty;
        foreach (var word in wordList)
        {
            var gap = Math.Max(word.Bounds.Left - previous.Right, previous.Left - word.Bounds.Right);
            if (text.Count > 0 && gap > maximumWordGap)
            {
                yield return new OcrTextLine(string.Join(' ', text), bounds);
                text.Clear();
            }
            bounds = text.Count == 0 ? word.Bounds : RectangleF.Union(bounds, word.Bounds);
            text.Add(word.Text);
            previous = word.Bounds;
        }
        if (text.Count > 0) yield return new OcrTextLine(string.Join(' ', text), bounds);
    }
}
