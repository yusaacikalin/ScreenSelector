using System.Drawing.Imaging;
using ScreenSelector;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            CheckCoordinateMapping();
            CheckWordGrouping();
            CheckRendering();
            CheckExpansionObstacles();
            DialogLayoutChecks.RunAsync().GetAwaiter().GetResult();
            CheckTranslationResponses();
            CheckLineTranslationAsync().GetAwaiter().GetResult();
            CheckOcrAsync().GetAwaiter().GetResult();
            CheckMenuScreenshotAsync(args.Length > 0 ? args[0] :
                    Path.Combine(AppContext.BaseDirectory, "Fixtures", "menu-toolbar.png"))
                .GetAwaiter().GetResult();
            CheckWindowLifecycle();
            Console.WriteLine("PASS: all inline translation checks");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void CheckTranslationResponses()
    {
        using var sameLanguage = System.Text.Json.JsonDocument.Parse("""
            {"responseData":{"translatedText":"PLEASE SELECT TWO DISTINCT LANGUAGES"},"responseStatus":400}
            """);
        var result = TranslationService.ParseMyMemoryResult(sameLanguage.RootElement, "Bu bilgisayar", "auto", "tr");
        Require(result.Text == "Bu bilgisayar", "A service error replaced an already-Turkish label");
        using var failed = System.Text.Json.JsonDocument.Parse("""
            {"responseData":{"translatedText":"REQUEST LIMIT EXCEEDED"},"responseStatus":"429"}
            """);
        try
        {
            TranslationService.ParseMyMemoryResult(failed.RootElement, "Your computer", "en", "tr");
            throw new Exception("A service failure was accepted as translation text");
        }
        catch (InvalidOperationException) { }
        using var success = System.Text.Json.JsonDocument.Parse("""
            {"responseData":{"translatedText":"Bilgisayarınız","detectedLanguage":"en"},"responseStatus":200}
            """);
        Require(TranslationService.ParseMyMemoryResult(success.RootElement, "Your computer", "en", "tr").Text ==
            "Bilgisayarınız", "A valid translation was rejected");
        Console.WriteLine("PASS: translation service errors are not rendered as translated text");
    }

    private static void CheckWordGrouping()
    {
        var words = new[]
        {
            new OcrTextLine("File", new RectangleF(10, 10, 17, 10)),
            new OcrTextLine("Edit", new RectangleF(48, 10, 20, 10)),
            new OcrTextLine("Solution", new RectangleF(90, 10, 45, 10)),
            new OcrTextLine("Explorer", new RectangleF(139, 10, 47, 10)),
            new OcrTextLine("Help", new RectangleF(207, 10, 25, 10))
        };
        foreach (var scale in new[] { 1f, 1.5f, 2f })
        {
            var scaled = words.Select(word => word with { Bounds = new RectangleF(word.Bounds.X * scale,
                word.Bounds.Y * scale, word.Bounds.Width * scale, word.Bounds.Height * scale) }).ToArray();
            var groups = OcrTextLine.SplitAtLargeGaps(scaled).ToArray();
            Require(groups.Select(group => group.Text).SequenceEqual(new[] { "File", "Edit", "Solution Explorer", "Help" }),
                "Menu labels were merged or a multiword label was broken apart");
            Require(groups[0].Bounds == scaled[0].Bounds && groups[1].Bounds == scaled[1].Bounds &&
                    groups[2].Bounds == RectangleF.Union(scaled[2].Bounds, scaled[3].Bounds),
                "Splitting labels changed their screen positions");
        }
        Console.WriteLine("PASS: menu spacing and multiword labels at multiple scales");
    }

    private static async Task CheckMenuScreenshotAsync(string path)
    {
        using var capture = new Bitmap(path);
        var lines = await OcrService.ExtractLinesAsync(capture, "en-US", CancellationToken.None);
        var labels = new[] { "File", "Edit", "View", "Git", "Project", "Build", "Debug", "Test", "Analyze", "Tools",
            "Extensions", "Window", "Help" };
        var menuLines = lines.Where(line => line.Bounds.Top < 25).ToArray();
        foreach (var label in labels)
            Require(menuLines.Count(line => line.Text.Equals(label, StringComparison.OrdinalIgnoreCase)) == 1,
                $"Menu '{label}' was merged, missed or moved");

        var menuStart = menuLines.Single(line => line.Text == "File").Bounds.Left;
        var menuEnd = menuLines.Single(line => line.Text == "Help").Bounds.Right;
        Require(menuLines.Where(line => line.Bounds.Left >= menuStart && line.Bounds.Right <= menuEnd)
            .All(line => line.Bounds.Width < 65), "A translation box spans multiple menu items");

        var candidates = OcrImagePreprocessor.CreateCandidates(capture, includeInverted: true);
        try
        {
            var tesseractLayouts = await TesseractOcrService.RecognizeLayoutsAsync(candidates, capture.Size,
                "en-US", CancellationToken.None);
            Require(tesseractLayouts.Count > 0, "Tesseract regression was not exercised");
            foreach (var layout in tesseractLayouts)
                Require(layout.Where(line => line.Bounds.Top < 25).All(line =>
                        labels.Count(label => line.Text.Split(' ').Contains(label)) <= 1),
                    "Tesseract still joins multiple menu labels before layout selection");
        }
        finally
        {
            foreach (var candidate in candidates) candidate.Dispose();
        }

        var translations = new[] { "Dosya", "Düzenle", "Görünüm", "Git", "Proje", "Derle", "Hata Ayıkla", "Test",
            "Analiz", "Araçlar", "Uzantılar", "Pencere", "Yardım" };
        var translationMap = labels.Zip(translations).ToDictionary(pair => pair.First, pair => pair.Second);
        var translated = lines.Select(line => new TranslatedTextLine(
            translationMap.GetValueOrDefault(line.Text, line.Text), line.Bounds, line.Text)).ToArray();
        // The cropped regression fixture contains only glyphs and the menu
        // row. Include blank space below it to exercise readable wrapping.
        using var renderCapture = new Bitmap(capture.Width, Math.Max(60, capture.Height));
        using (var graphics = Graphics.FromImage(renderCapture))
        {
            graphics.Clear(capture.GetPixel(0, 0));
            graphics.DrawImageUnscaled(capture, 0, 0);
        }
        using var surface = new InlineTranslationSurface(renderCapture, translated);
        foreach (var layout in surface.Layouts)
        {
            Require(layout.FontSize >= InlineTranslationLayoutEngine.MinimumFontSize,
                $"Translation is too small: {layout.Text}");
        }
        CheckLayoutCollisions(surface.Layouts.Where(layout => translations.Contains(layout.Text)).ToArray());
        using var preview = new Bitmap(renderCapture.Width, renderCapture.Height * 2);
        using (var graphics = Graphics.FromImage(preview))
        {
            graphics.DrawImageUnscaled(renderCapture, 0, 0);
            graphics.DrawImageUnscaled(renderCapture, 0, renderCapture.Height);
            graphics.DrawImageUnscaled(surface.Image, 0, renderCapture.Height);
        }
        preview.Save(Path.Combine(AppContext.BaseDirectory, "menu-translation-preview.png"), ImageFormat.Png);
        Console.WriteLine("PASS: screenshot menu positions, readable fonts, complete text and no overlap");
    }

    private static void CheckCoordinateMapping()
    {
        using var candidate = new OcrImageCandidate(new Bitmap(348, 248), new Rectangle(24, 24, 300, 200));
        var mapped = candidate.ToSourceBounds(new RectangleF(54, 64, 120, 40), new Size(150, 100));
        Require(mapped == new RectangleF(15, 20, 60, 20), "OCR scale/padding mapped incorrectly");
        var clipped = candidate.ToSourceBounds(new RectangleF(0, 0, 348, 248), new Size(150, 100));
        Require(clipped == new RectangleF(0, 0, 150, 100), "OCR bounds escaped the capture");
        Console.WriteLine("PASS: OCR padding, scaling and edge coordinates");
    }

    private static void CheckRendering()
    {
        using var capture = CreateSample();
        var lines = new[]
        {
            new TranslatedTextLine("Ayarlar", new RectangleF(24, 25, 120, 28)),
            new TranslatedTextLine("Değişikliklerinizi kaydedin", new RectangleF(24, 94, 248, 28)),
            new TranslatedTextLine("Bu uzun çeviri özgün satırın sınırlarını aşmadan bütünüyle sığmalıdır.",
                new RectangleF(24, 187, 250, 25))
        };
        using var surface = new InlineTranslationSurface(capture, lines);
        var white = surface.Layouts[0].Background;
        var dark = surface.Layouts[1].Background;
        Require(white.ToArgb() == Color.White.ToArgb(), "Light background was not preserved");
        Require(dark.ToArgb() == Color.FromArgb(30, 40, 60).ToArgb(),
            "Dark background was not preserved");
        Require(surface.Image.GetPixel(400, 60).A == 0 && !surface.VisibleRegion.IsVisible(400, 60),
            "Non-text content must remain transparent");
        using (var graphics = Graphics.FromImage(surface.Image))
        {
            foreach (var layout in surface.Layouts)
            {
                var measured = InlineTextMetrics.Measure(layout.Text, layout.FontSize, layout.WrapWidth);
                Require(measured.Width <= layout.TextBounds.Width && measured.Height <= layout.TextBounds.Height,
                    "The complete translation does not fit its area");
                Require(layout.FontSize >= InlineTranslationLayoutEngine.MinimumFontSize,
                    "Translation shrank below a readable size");
            }
        }
        Require(surface.Layouts.Last().Wrap, "Long text did not wrap when it could not fit at a readable size");
        CheckLayoutCollisions(surface.Layouts);
        using var preview = new Bitmap(capture.Width * 2, capture.Height);
        using (var graphics = Graphics.FromImage(preview))
        {
            graphics.DrawImageUnscaled(capture, 0, 0);
            graphics.DrawImageUnscaled(capture, capture.Width, 0);
            graphics.DrawImageUnscaled(surface.Image, capture.Width, 0);
        }
        var previewPath = Path.Combine(AppContext.BaseDirectory, "inline-translation-preview.png");
        preview.Save(previewPath, ImageFormat.Png);
        Console.WriteLine($"PASS: local backgrounds, transparency and font fitting; preview: {previewPath}");
    }

    private static void CheckLayoutCollisions(IReadOnlyList<InlineTranslationLayout> layouts)
    {
        for (var i = 0; i < layouts.Count; i++)
        for (var j = i + 1; j < layouts.Count; j++)
            Require(!layouts[i].CoverBounds.IntersectsWith(layouts[j].CoverBounds),
                $"Expanded translations overlap: {layouts[i].Text} / {layouts[j].Text}");
    }

    private static void CheckExpansionObstacles()
    {
        using var capture = new Bitmap(400, 120);
        using (var graphics = Graphics.FromImage(capture))
        {
            graphics.Clear(Color.FromArgb(30, 40, 60));
            graphics.FillRectangle(Brushes.White, new Rectangle(100, 30, 15, 60)); // An icon OCR did not recognize.
            graphics.FillRectangle(Brushes.White, new Rectangle(150, 0, 250, 120)); // A different panel background.
        }
        var lines = new[]
        {
            new TranslatedTextLine("Değişiklikleri kaydet", new RectangleF(20, 45, 12, 10)),
            new TranslatedTextLine("İleri", new RectangleF(190, 45, 30, 10))
        };
        using var surface = new InlineTranslationSurface(capture, lines);
        Require(surface.Layouts.All(layout => layout.FontSize >= InlineTranslationLayoutEngine.MinimumFontSize),
            "An expanded label became unreadable");
        Require(surface.Layouts[0].TextBounds.Width > lines[0].Bounds.Width && surface.Layouts[0].Wrap,
            "A narrow label did not expand and wrap");
        Require(surface.Layouts[0].CoverBounds.Right <= 100, "Translation covered an unrecognized icon");
        Require(surface.Layouts[1].CoverBounds.Left >= 150, "Translation crossed a panel background boundary");
        Require(surface.Image.GetPixel(105, 50).A == 0, "An icon pixel was painted over");
        CheckLayoutCollisions(surface.Layouts);
        Console.WriteLine("PASS: readable wrapping respects icons and different background panels");
    }

    private static async Task CheckLineTranslationAsync()
    {
        var lines = new[]
        {
            new OcrTextLine("Settings", new RectangleF(10, 10, 100, 20)),
            new OcrTextLine("Save changes", new RectangleF(10, 40, 150, 20)),
            new OcrTextLine("Settings", new RectangleF(300, 10, 100, 20))
        };
        // Same-language translation is deterministic and does not call the network.
        var translated = await TranslationService.TranslateLinesAsync(lines, "en", "en", CancellationToken.None);
        Require(translated.Count == lines.Length, "A repeated label was dropped");
        for (var i = 0; i < lines.Length; i++)
            Require(translated[i].Text == lines[i].Text && translated[i].Bounds == lines[i].Bounds,
                "A translation moved to another line");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try
        {
            await TranslationService.TranslateLinesAsync(lines, "en", "en", cancellation.Token);
            throw new Exception("Cancelled translation continued");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: translation positions, duplicate labels and cancellation");
    }

    private static async Task CheckOcrAsync()
    {
        using var sample = CreateSample();
        var lines = await OcrService.ExtractLinesAsync(sample, "en-US", CancellationToken.None);
        foreach (var expected in new[] { ("Settings", 25), ("Save changes", 94), ("Welcome", 187) })
        {
            var line = lines.FirstOrDefault(line => line.Text.Contains(expected.Item1, StringComparison.OrdinalIgnoreCase));
            Require(line != null, $"OCR missed '{expected.Item1}': {string.Join(" | ", lines.Select(line => line.Text))}");
            Require(Math.Abs(line!.Bounds.Top - expected.Item2) < 15, "OCR returned a scaled or shifted line position");
        }
        using var empty = new Bitmap(180, 60);
        using (var graphics = Graphics.FromImage(empty)) graphics.Clear(Color.White);
        Require((await OcrService.ExtractLinesAsync(empty, "en-US", CancellationToken.None)).Count == 0,
            "An empty selection created a translation");
        using var small = new Bitmap(220, 45);
        using (var graphics = Graphics.FromImage(small))
        using (var font = new Font("Segoe UI", 24, GraphicsUnit.Pixel))
        {
            graphics.Clear(Color.White);
            graphics.DrawString("Settings", font, Brushes.Black, 1, 3);
        }
        var smallLines = await OcrService.ExtractLinesAsync(small, "en-US", CancellationToken.None);
        Require(smallLines.Any(line => line.Text.Contains("Settings") && line.Bounds.Left < 10 && line.Bounds.Height < 35),
            "Upscaled OCR lost text near the selection edge or returned the wrong coordinates");
        using var columns = new Bitmap(720, 100);
        using (var graphics = Graphics.FromImage(columns))
        using (var font = new Font("Segoe UI", 26, GraphicsUnit.Pixel))
        {
            graphics.Clear(Color.White);
            graphics.DrawString("Settings", font, Brushes.Black, 20, 25);
            graphics.DrawString("Welcome", font, Brushes.Black, 500, 25);
        }
        var columnLines = await OcrService.ExtractLinesAsync(columns, "en-US", CancellationToken.None);
        Require(columnLines.Count == 2 && columnLines.All(line => line.Bounds.Width < 200),
            $"Separate columns were joined or duplicated: {string.Join(" | ", columnLines)}");
        Console.WriteLine("PASS: real OCR on light/dark backgrounds, empty selection, small capture and columns");
    }

    private static void CheckWindowLifecycle()
    {
        using var capture = CreateSample();
        var origin = new Point(-450, -200);
        using var form = new TestToolbar(capture, new Rectangle(origin, capture.Size));
        _ = form.Handle;
        var originalHandle = form.Handle;
        var translated = new[] { new TranslatedTextLine("Ayarlar", new RectangleF(24, 25, 120, 28)) };
        form.ShowInlineTranslation(translated);
        Require(form.Handle == originalHandle, "Translation created another window");
        Require(form.Bounds.Location == origin && form.ClientSize == capture.Size, "Screen position or pixel size changed");
        Require(form.Region!.IsVisible(24, 25) && !form.Region.IsVisible(300, 200), "Overlay region covers unrelated content");
        form.ClickTranslation();
        Application.DoEvents();
        Require(form.IsDisposed, "Click did not dismiss the translation");
        form.Dispose(); // Repeated disposal must not access a disposed cancellation source.
        Console.WriteLine("PASS: existing window reused, negative screen coordinates, region and click dismissal");
    }

    private static Bitmap CreateSample()
    {
        var image = new Bitmap(520, 250);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.White);
        using var dark = new SolidBrush(Color.FromArgb(30, 40, 60));
        using var green = new SolidBrush(Color.FromArgb(220, 242, 230));
        graphics.FillRectangle(dark, 0, 75, 520, 78);
        graphics.FillRectangle(green, 0, 153, 520, 97);
        using var font = new Font("Segoe UI", 26, GraphicsUnit.Pixel);
        graphics.DrawString("Settings", font, Brushes.Black, 24, 20);
        graphics.DrawString("Save changes", font, Brushes.White, 24, 89);
        graphics.DrawString("Welcome", font, Brushes.Black, 24, 182);
        return image;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class TestToolbar(Bitmap capture, Rectangle bounds)
        : ActionToolbarForm(capture, new AppSettings(), bounds, false, captureScreenArea: bounds)
    {
        protected override void OnShown(EventArgs e) { } // Avoid automatic clipboard writes in tests.
        internal void ClickTranslation() => OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 30, 30, 0));
    }
}
