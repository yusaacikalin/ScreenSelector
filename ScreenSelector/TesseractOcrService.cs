using System.Drawing.Imaging;
using Tesseract;

namespace ScreenSelector;

internal static class TesseractOcrService
{
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
