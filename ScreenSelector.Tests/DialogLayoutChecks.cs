using System.Drawing.Imaging;
using ScreenSelector;

internal static class DialogLayoutChecks
{
    internal static async Task RunAsync()
    {
        using var capture = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Fixtures", "dialog-text.png"));
        var lines = await OcrService.ExtractLinesAsync(capture, "en-US", CancellationToken.None);
        var translations = new (string Prefix, string Text)[]
        {
            ("Choose where", "Dot'unuzun nerede çalışabileceğini seçin"),
            ("Your dot has", "Dot'unuzun kendi bilgisayarı var, ancak sizin bilgisayarınızı da kullanabilir. Bunu"),
            ("can change", "istediğiniz zaman değiştirebilirsiniz."),
            ("Your dot's", "Dot'unuzun bilgisayarı"),
            ("A powerful", "Dot'unuzun çalıştığı güçlü bir bulut bilgisayarı"),
            ("Bu bilgisayar", "Bu bilgisayar"),
            ("Access files", "Bu bilgisayardaki dosyalara erişin ve üzerinde çalışın; nerede olursanız olun"),
            ("message your", "dot'unuza mesaj gönderin. Bu işlem, bağlı olan diğer tüm bilgisayarların bağlantısını"),
            ("desktop", "keser.")
        };
        var translated = translations.Select(item =>
        {
            var line = lines.Single(line => line.Text.StartsWith(item.Prefix, StringComparison.OrdinalIgnoreCase));
            return new TranslatedTextLine(item.Text, line.Bounds, line.Text);
        }).ToArray();
        using var surface = new InlineTranslationSurface(capture, translated);
        foreach (var layout in surface.Layouts)
        {
            if (layout.Text.Contains('…')) throw new Exception("Translation was shortened with an ellipsis");
            if (layout.TextBounds.Left != layout.SourceBounds.Left || layout.TextBounds.Top != layout.SourceBounds.Top)
                throw new Exception("A translation moved up or sideways from its original line");
        }
        foreach (var item in translated.Where(line => line.Text != line.SourceText))
            if (!surface.Layouts.Any(layout => layout.Text.Contains(item.Text, StringComparison.Ordinal)))
                throw new Exception($"The full translation was not retained: {item.Text}");
        var heading = surface.Layouts[0];
        if (heading.Wrap || !translated[0].Bounds.Contains(heading.TextBounds))
            throw new Exception("Readable heading expanded outside its original line");
        var description = surface.Layouts.Single(layout => layout.Text.StartsWith("Dot'unuzun kendi"));
        if (description.TextBounds.Height > translated[1].Bounds.Height || description.Wrap)
            throw new Exception("Readable body text was enlarged or wrapped unnecessarily");
        var paragraph = surface.Layouts.Single(layout => layout.Text.StartsWith("Bu bilgisayardaki"));
        if (!paragraph.Wrap || paragraph.TextBounds.Bottom > capture.Height)
            throw new Exception("Long paragraph did not flow downward completely");
        for (var i = 0; i < surface.Layouts.Count; i++)
        for (var j = i + 1; j < surface.Layouts.Count; j++)
            if (surface.Layouts[i].CoverBounds.IntersectsWith(surface.Layouts[j].CoverBounds))
                throw new Exception("Dialog translations overlap each other");
        if (surface.VisibleRegion.IsVisible(105, 218) || surface.VisibleRegion.IsVisible(447, 244))
            throw new Exception("Unchanged Turkish text or the toggle was overwritten");

        using var preview = new Bitmap(capture.Width * 2, capture.Height);
        using (var graphics = Graphics.FromImage(preview))
        {
            graphics.DrawImageUnscaled(capture, 0, 0);
            graphics.DrawImageUnscaled(capture, capture.Width, 0);
            graphics.DrawImageUnscaled(surface.Image, capture.Width, 0);
        }
        preview.Save(Path.Combine(AppContext.BaseDirectory, "dialog-translation-preview.png"), ImageFormat.Png);
        Console.WriteLine("PASS: supplied dialog keeps readable lines in place, wraps downward and retains all text");
    }
}
