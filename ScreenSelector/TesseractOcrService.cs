using System.Drawing.Imaging;
using Tesseract;

namespace ScreenSelector;

internal static class TesseractOcrService
{
    public static Task<IReadOnlyList<IReadOnlyList<OcrTextLine>>> RecognizeLayoutsAsync(
        IReadOnlyList<OcrImageCandidate> candidates, Size sourceSize, string languageTag,
        CancellationToken cancellationToken)
    {
        return Task.Run<IReadOnlyList<IReadOnlyList<OcrTextLine>>>(() =>
        {
            var directory = Path.GetDirectoryName(typeof(TesseractOcrService).Assembly.Location)
                            ?? AppContext.BaseDirectory;
            var tessdataPath = Path.Combine(directory, "tessdata");
            if (!Directory.Exists(tessdataPath)) return [];

            using var engine = new TesseractEngine(tessdataPath, GetLanguage(languageTag, tessdataPath),
                EngineMode.LstmOnly);
            engine.SetVariable("user_defined_dpi", "300");
            var results = new List<IReadOnlyList<OcrTextLine>>();
            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var memory = new MemoryStream();
                candidate.Image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                using var pix = Pix.LoadFromMemory(memory.ToArray());
                // Even SparseText can put an entire menu bar on one TextLine.
                // Read word boxes and split that line using the actual gaps.
                using var page = engine.Process(pix, PageSegMode.SparseText);
                using var iterator = page.GetIterator();
                iterator.Begin();
                var lines = new List<OcrTextLine>();
                var words = new List<OcrTextLine>();
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (iterator.IsAtBeginningOf(PageIteratorLevel.TextLine))
                    {
                        lines.AddRange(OcrTextLine.SplitAtLargeGaps(words));
                        words.Clear();
                    }
                    if (!iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var box) ||
                        iterator.GetConfidence(PageIteratorLevel.TextLine) < 45f) continue;
                    var text = iterator.GetText(PageIteratorLevel.Word)?.Trim();
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var bounds = candidate.ToSourceBounds(
                        RectangleF.FromLTRB(box.X1, box.Y1, box.X2, box.Y2), sourceSize);
                    if (bounds.Width > 0 && bounds.Height > 0) words.Add(new OcrTextLine(text, bounds));
                } while (iterator.Next(PageIteratorLevel.Word));
                lines.AddRange(OcrTextLine.SplitAtLargeGaps(words));
                results.Add(lines.Where(line => line.Text.Any(char.IsLetterOrDigit)).ToArray());
            }
            return results;
        }, cancellationToken);
    }

    public static Task<IReadOnlyList<string>> RecognizeCandidatesAsync(IReadOnlyList<OcrImageCandidate> candidates,
        string languageTag)
    {
        return Task.Run<IReadOnlyList<string>>(() =>
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(TesseractOcrService).Assembly.Location)
                                    ?? AppContext.BaseDirectory;
            var tessdataPath = Path.Combine(assemblyDirectory, "tessdata");
            if (!Directory.Exists(tessdataPath)) return Array.Empty<string>();

            var language = GetLanguage(languageTag, tessdataPath);
            using var engine = new TesseractEngine(tessdataPath, language, EngineMode.LstmOnly);
            engine.SetVariable("preserve_interword_spaces", "1");
            engine.SetVariable("user_defined_dpi", "300");
            var results = new List<string>();

            foreach (var candidate in candidates)
            {
                using var memory = new MemoryStream();
                candidate.Image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                using var pix = Pix.LoadFromMemory(memory.ToArray());
                AddResult(engine, pix, PageSegMode.SingleBlock, candidate.ContentBounds, results);

                if (candidate.Image.Width >= candidate.Image.Height * 4)
                    AddResult(engine, pix, PageSegMode.SingleLine, candidate.ContentBounds, results);
                else
                    AddResult(engine, pix, PageSegMode.SparseText, candidate.ContentBounds, results);
            }

            return results;
        });
    }

    private static void AddResult(TesseractEngine engine, Pix pix, PageSegMode mode, Rectangle contentBounds,
        ICollection<string> results)
    {
        using var page = engine.Process(pix, mode);
        using var iterator = page.GetIterator();
        iterator.Begin();
        var completeLines = new List<string>();
        do
        {
            if (!iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var bounds) ||
                TouchesSelectionEdge(bounds.X1, bounds.Y1, bounds.X2, bounds.Y2, contentBounds) ||
                iterator.GetConfidence(PageIteratorLevel.TextLine) < 72F)
                continue;

            var line = iterator.GetText(PageIteratorLevel.TextLine)?.Trim();
            if (!string.IsNullOrWhiteSpace(line)) completeLines.Add(line);
        } while (iterator.Next(PageIteratorLevel.TextLine));

        if (completeLines.Count > 0) results.Add(string.Join(Environment.NewLine, completeLines));
    }

    private static bool TouchesSelectionEdge(double left, double top, double right, double bottom,
        Rectangle contentBounds)
    {
        const int tolerance = 2;
        return left <= contentBounds.Left + tolerance || top <= contentBounds.Top + tolerance ||
               right >= contentBounds.Right - tolerance || bottom >= contentBounds.Bottom - tolerance;
    }

    private static string GetLanguage(string languageTag, string tessdataPath)
    {
        if (languageTag.StartsWith("tr", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(languageTag))
            return File.Exists(Path.Combine(tessdataPath, "tur.traineddata")) ? "tur+eng" : "eng";
        return "eng";
    }
}
