using System.Drawing.Imaging;
using Tesseract;

namespace ScreenSelector;

internal static class TesseractOcrService
{
    public static Task<IReadOnlyList<string>> RecognizeCandidatesAsync(IReadOnlyList<Bitmap> candidates,
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
                candidate.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                using var pix = Pix.LoadFromMemory(memory.ToArray());
                AddResult(engine, pix, PageSegMode.SingleBlock, results);

                if (candidate.Width >= candidate.Height * 4)
                    AddResult(engine, pix, PageSegMode.SingleLine, results);
                else
                    AddResult(engine, pix, PageSegMode.SparseText, results);
            }

            return results;
        });
    }

    private static void AddResult(TesseractEngine engine, Pix pix, PageSegMode mode, ICollection<string> results)
    {
        using var page = engine.Process(pix, mode);
        var text = page.GetText()?.Trim();
        if (!string.IsNullOrWhiteSpace(text)) results.Add(text);
    }

    private static string GetLanguage(string languageTag, string tessdataPath)
    {
        if (languageTag.StartsWith("tr", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(languageTag))
            return File.Exists(Path.Combine(tessdataPath, "tur.traineddata")) ? "tur+eng" : "eng";
        return "eng";
    }
}
