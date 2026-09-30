using System.Drawing.Imaging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using System.Text.RegularExpressions;

namespace ScreenSelector;

internal static class OcrService
{
    public static async Task<IReadOnlyList<OcrTextLine>> ExtractLinesAsync(Bitmap bitmap, string languageTag,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var candidates = OcrImagePreprocessor.CreateCandidates(bitmap, includeInverted: true);
        var results = new List<IReadOnlyList<OcrTextLine>>();
        Exception? lastError = null;
        var recognized = false;
        try
        {
            try
            {
                var layouts = await TesseractOcrService.RecognizeLayoutsAsync(candidates, bitmap.Size,
                    languageTag, cancellationToken);
                recognized = layouts.Count > 0;
                results.AddRange(layouts.Where(lines => lines.Count > 0));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { lastError = ex; }

            try
            {
                var engine = CreateEngine(languageTag);
                foreach (var candidate in candidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var lines = await RecognizeLinesAsync(candidate, engine, bitmap.Size, excludeEdges: false);
                    recognized = true;
                    if (lines.Count > 0) results.Add(lines);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { lastError = ex; }

            cancellationToken.ThrowIfCancellationRequested();
            if (results.Count == 0)
            {
                if (!recognized && lastError != null) throw lastError;
                return [];
            }

            var texts = results.Select(lines => string.Join(Environment.NewLine, lines.Select(line => line.Text)))
                .ToArray();
            var best = results[Array.IndexOf(texts, SelectBestResult(texts))].ToList();
            // Mixed light/dark panels can be recognized by different passes.
            // Keep the preferred wording, then add only previously unseen areas.
            foreach (var layout in results)
            foreach (var line in layout)
            {
                if (!best.Any(existing => OccupiesSameArea(existing.Bounds, line.Bounds))) best.Add(line);
            }
            return best.OrderBy(line => line.Bounds.Top).ThenBy(line => line.Bounds.Left).ToArray();
        }
        finally
        {
            foreach (var candidate in candidates) candidate.Dispose();
        }
    }

    private static bool OccupiesSameArea(RectangleF first, RectangleF second)
    {
        var intersection = RectangleF.Intersect(first, second);
        return intersection.Width > 0 && intersection.Height > 0 &&
               intersection.Width * intersection.Height >=
               Math.Min(first.Width * first.Height, second.Width * second.Height) * 0.35f;
    }

    public static async Task<string> ExtractTextAsync(Bitmap bitmap, string languageTag)
    {
        var engine = CreateEngine(languageTag);
        var candidates = OcrImagePreprocessor.CreateCandidates(bitmap);
        var recognizedResults = new List<string>();
        Exception? lastError = null;

        try
        {
            var tesseractResults = await TesseractOcrService.RecognizeCandidatesAsync(candidates, languageTag);
            foreach (var text in tesseractResults)
            {
                var cleanedText = CleanResult(text);
                if (!string.IsNullOrWhiteSpace(cleanedText)) recognizedResults.Add(cleanedText);
            }
        }
        catch
        {
            // Tesseract yerel çalışma zamanı yoksa Windows OCR yedek olarak devam eder.
        }

        foreach (var candidate in candidates)
        {
            using (candidate)
            {
                try
                {
                    var lines = await RecognizeLinesAsync(candidate, engine, bitmap.Size, excludeEdges: true);
                    var text = string.Join(Environment.NewLine, lines.Select(line => line.Text));
                    var cleanedText = CleanResult(text);
                    if (!string.IsNullOrWhiteSpace(cleanedText)) recognizedResults.Add(cleanedText);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }
        }

        if (recognizedResults.Count == 0 && lastError != null) throw lastError;
        if (recognizedResults.Count == 0) return string.Empty;
        return SelectBestResult(recognizedResults).Trim();
    }

    private static OcrEngine CreateEngine(string languageTag)
    {
        OcrEngine? engine = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(languageTag))
                engine = OcrEngine.TryCreateFromLanguage(new Language(languageTag));
        }
        catch
        {
            // İstenen dil paketi yüklü değilse kullanıcı profili dilleri denenir.
        }

        engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
        return engine ?? throw new InvalidOperationException(
            "Windows OCR motoru kullanılamıyor. Windows Ayarları > Dil ve bölge bölümünden ilgili dilin OCR bileşenini yükleyin.");
    }

    private static async Task<IReadOnlyList<OcrTextLine>> RecognizeLinesAsync(OcrImageCandidate candidate,
        OcrEngine engine, Size sourceSize, bool excludeEdges)
    {
        using var memory = new MemoryStream();
        candidate.Image.Save(memory, ImageFormat.Png);
        using var randomAccessStream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(randomAccessStream))
        {
            writer.WriteBytes(memory.ToArray());
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        randomAccessStream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        var longestSide = Math.Max(decoder.PixelWidth, decoder.PixelHeight);
        var scale = longestSide > OcrEngine.MaxImageDimension
            ? (double)OcrEngine.MaxImageDimension / longestSide
            : 1d;
        var transform = new BitmapTransform
        {
            ScaledWidth = Math.Max(1u, (uint)(decoder.PixelWidth * scale)),
            ScaledHeight = Math.Max(1u, (uint)(decoder.PixelHeight * scale))
        };

        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);
        var result = await engine.RecognizeAsync(softwareBitmap);
        var scaleX = (float)transform.ScaledWidth / candidate.Image.Width;
        var scaleY = (float)transform.ScaledHeight / candidate.Image.Height;
        var completeLines = new List<OcrTextLine>();
        foreach (var line in result.Lines)
        {
            if (line.Words.Count == 0) continue;
            var left = line.Words.Min(word => word.BoundingRect.X);
            var top = line.Words.Min(word => word.BoundingRect.Y);
            var right = line.Words.Max(word => word.BoundingRect.X + word.BoundingRect.Width);
            var bottom = line.Words.Max(word => word.BoundingRect.Y + word.BoundingRect.Height);
            var candidateBounds = RectangleF.FromLTRB((float)left / scaleX, (float)top / scaleY,
                (float)right / scaleX, (float)bottom / scaleY);
            if (excludeEdges && TouchesSelectionEdge(candidateBounds.Left, candidateBounds.Top,
                    candidateBounds.Right, candidateBounds.Bottom, candidate.ContentBounds)) continue;
            var bounds = candidate.ToSourceBounds(candidateBounds, sourceSize);
            if (bounds.Width > 0 && bounds.Height > 0 && !string.IsNullOrWhiteSpace(line.Text))
            {
                if (excludeEdges) completeLines.Add(new OcrTextLine(line.Text.Trim(), bounds));
                else
                {
                    var words = line.Words.Select(word => new OcrTextLine(word.Text,
                        candidate.ToSourceBounds(new RectangleF((float)word.BoundingRect.X / scaleX,
                            (float)word.BoundingRect.Y / scaleY, (float)word.BoundingRect.Width / scaleX,
                            (float)word.BoundingRect.Height / scaleY), sourceSize)))
                        .Where(word => word.Bounds.Width > 0 && word.Bounds.Height > 0);
                    completeLines.AddRange(OcrTextLine.SplitAtLargeGaps(words));
                }
            }
        }
        return completeLines;
    }

    private static bool TouchesSelectionEdge(double left, double top, double right, double bottom,
        RectangleF contentBounds)
    {
        const int tolerance = 2;
        return left <= contentBounds.Left + tolerance || top <= contentBounds.Top + tolerance ||
               right >= contentBounds.Right - tolerance || bottom >= contentBounds.Bottom - tolerance;
    }

    private static double ScoreResult(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var meaningful = text.Count(char.IsLetterOrDigit);
        var symbols = text.Count(character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character));
        var languageSpecificCharacters = text.Count(character => "çğıöşüÇĞİÖŞÜ".Contains(character));
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return meaningful * 2.2 + words * 2 + lines * 8 + languageSpecificCharacters * 2.5
               - Math.Max(0, symbols - meaningful / 3d);
    }

    private static string SelectBestResult(IReadOnlyList<string> results)
    {
        var normalized = results.Select(NormalizeForComparison).ToArray();
        var bestIndex = 0;
        var bestScore = double.MinValue;
        for (var i = 0; i < results.Count; i++)
        {
            var exactMatches = normalized.Count(value => value == normalized[i]) - 1;
            var similaritySupport = 0d;
            for (var j = 0; j < results.Count; j++)
            {
                if (i != j) similaritySupport += TokenSimilarity(normalized[i], normalized[j]);
            }

            var score = ScoreResult(results[i]) + exactMatches * 35 + similaritySupport * 7;
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }
        return results[bestIndex];
    }

    private static string NormalizeForComparison(string text)
    {
        var characters = text.ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
            .ToArray();
        return string.Join(' ', new string(characters)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static double TokenSimilarity(string first, string second)
    {
        if (first == second) return 1;
        var firstWords = first.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var secondWords = second.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (firstWords.Count == 0 || secondWords.Count == 0) return 0;
        var intersection = firstWords.Count(secondWords.Contains);
        return (double)intersection / Math.Max(firstWords.Count, secondWords.Count);
    }

    private static string CleanResult(string text)
    {
        var cleanedLines = new List<string>();
        foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = Regex.Replace(rawLine, @"\s{2,}[\p{L}|¦]+\s*$", string.Empty).Trim();
            line = line.Trim('|', '¦', '—', '_');
            line = Regex.Replace(line, @"^[A-ZÇĞİÖŞÜ]{1,3}\s+(?=[\""'“])", string.Empty);
            var meaningful = line.Count(char.IsLetterOrDigit);
            if (meaningful < 2) continue;
            cleanedLines.Add(line.Trim());
        }
        return string.Join(Environment.NewLine, cleanedLines);
    }
}
