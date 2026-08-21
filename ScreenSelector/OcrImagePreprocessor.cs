using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenSelector;

internal static class OcrImagePreprocessor
{
    private const int Padding = 24;
    private const int PreferredMaximumSide = 2400;

    public static IReadOnlyList<Bitmap> CreateCandidates(Bitmap source)
    {
        var scale = ChooseScale(source.Size);
        var scaledWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        var scaledHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
        var background = EstimateBackgroundColor(source);
        var darkBackground = Luminance(background.R, background.G, background.B) < 145;

        var original = new Bitmap(scaledWidth + Padding * 2, scaledHeight + Padding * 2,
            PixelFormat.Format32bppArgb);
        original.SetResolution(96, 96);
        using (var graphics = Graphics.FromImage(original))
        {
            graphics.Clear(background);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(Padding, Padding, scaledWidth, scaledHeight),
                0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
        }

        var normalized = CreateNormalizedGrayscale(original, darkBackground, binary: false);
        var binary = CreateNormalizedGrayscale(original, darkBackground, binary: true);
        return new[] { normalized, binary, original };
    }

    private static float ChooseScale(Size size)
    {
        var desired = size.Height switch
        {
            <= 24 => 4.5f,
            <= 45 => 3.5f,
            <= 80 => 2.75f,
            <= 130 => 2f,
            <= 220 => 1.5f,
            _ => 1f
        };

        var maximumScale = Math.Min((float)(PreferredMaximumSide - Padding * 2) / size.Width,
            (float)(PreferredMaximumSide - Padding * 2) / size.Height);
        return Math.Max(0.75f, Math.Min(desired, maximumScale));
    }

    private static Color EstimateBackgroundColor(Bitmap bitmap)
    {
        var counts = new int[16];
        var red = new long[16];
        var green = new long[16];
        var blue = new long[16];
        var stepX = Math.Max(1, bitmap.Width / 80);
        var stepY = Math.Max(1, bitmap.Height / 50);
        for (var y = 0; y < bitmap.Height; y += stepY)
        {
            for (var x = 0; x < bitmap.Width; x += stepX)
            {
                var color = bitmap.GetPixel(x, y);
                var bucket = Math.Min(15, Luminance(color.R, color.G, color.B) / 16);
                counts[bucket]++;
                red[bucket] += color.R;
                green[bucket] += color.G;
                blue[bucket] += color.B;
            }
        }

        var dominantBucket = Array.IndexOf(counts, counts.Max());
        var count = Math.Max(1, counts[dominantBucket]);
        return Color.FromArgb((int)(red[dominantBucket] / count),
            (int)(green[dominantBucket] / count), (int)(blue[dominantBucket] / count));
    }

    private static Bitmap CreateNormalizedGrayscale(Bitmap source, bool darkBackground, bool binary)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        var rectangle = new Rectangle(0, 0, source.Width, source.Height);
        var sourceData = source.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var resultData = result.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            var byteCount = Math.Abs(sourceData.Stride) * source.Height;
            var sourceBytes = new byte[byteCount];
            var resultBytes = new byte[Math.Abs(resultData.Stride) * result.Height];
            Marshal.Copy(sourceData.Scan0, sourceBytes, 0, sourceBytes.Length);

            var histogram = new int[256];
            var luminances = new byte[source.Width * source.Height];
            for (var y = 0; y < source.Height; y++)
            {
                for (var x = 0; x < source.Width; x++)
                {
                    var sourceIndex = y * sourceData.Stride + x * 4;
                    var luminance = Luminance(sourceBytes[sourceIndex + 2], sourceBytes[sourceIndex + 1],
                        sourceBytes[sourceIndex]);
                    if (darkBackground) luminance = 255 - luminance;
                    luminances[y * source.Width + x] = (byte)luminance;
                    histogram[luminance]++;
                }
            }

            var low = Percentile(histogram, luminances.Length, 0.02);
            var high = Percentile(histogram, luminances.Length, 0.98);
            if (high - low < 35) { low = 0; high = 255; }

            var normalizedHistogram = new int[256];
            for (var i = 0; i < luminances.Length; i++)
            {
                var value = Math.Clamp((luminances[i] - low) * 255 / Math.Max(1, high - low), 0, 255);
                luminances[i] = (byte)value;
                normalizedHistogram[value]++;
            }
            var threshold = OtsuThreshold(normalizedHistogram, luminances.Length);

            RemoveLongHorizontalLines(luminances, result.Width, result.Height, binary ? threshold : 65);

            for (var y = 0; y < result.Height; y++)
            {
                for (var x = 0; x < result.Width; x++)
                {
                    var value = luminances[y * result.Width + x];
                    if (binary) value = (byte)(value > threshold ? 255 : 0);
                    var resultIndex = y * resultData.Stride + x * 4;
                    resultBytes[resultIndex] = value;
                    resultBytes[resultIndex + 1] = value;
                    resultBytes[resultIndex + 2] = value;
                    resultBytes[resultIndex + 3] = 255;
                }
            }

            Marshal.Copy(resultBytes, 0, resultData.Scan0, resultBytes.Length);
        }
        finally
        {
            source.UnlockBits(sourceData);
            result.UnlockBits(resultData);
        }

        return result;
    }

    private static void RemoveLongHorizontalLines(byte[] pixels, int width, int height, int darkThreshold)
    {
        var rowsToClear = new List<int>();
        for (var y = 0; y < height; y++)
        {
            var darkPixels = 0;
            for (var x = 0; x < width; x++)
            {
                if (pixels[y * width + x] <= darkThreshold) darkPixels++;
            }
            if (darkPixels >= width * 0.72) rowsToClear.Add(y);
        }

        foreach (var row in rowsToClear)
        {
            for (var y = Math.Max(0, row - 1); y <= Math.Min(height - 1, row + 1); y++)
                Array.Fill(pixels, (byte)255, y * width, width);
        }
    }

    private static int Percentile(int[] histogram, int total, double percentile)
    {
        var target = (int)(total * percentile);
        var sum = 0;
        for (var i = 0; i < histogram.Length; i++)
        {
            sum += histogram[i];
            if (sum >= target) return i;
        }
        return 255;
    }

    private static int OtsuThreshold(int[] histogram, int total)
    {
        long weightedSum = 0;
        for (var i = 0; i < histogram.Length; i++) weightedSum += (long)i * histogram[i];

        long backgroundSum = 0;
        var backgroundWeight = 0;
        var bestVariance = double.MinValue;
        var bestThreshold = 128;
        for (var threshold = 0; threshold < 256; threshold++)
        {
            backgroundWeight += histogram[threshold];
            if (backgroundWeight == 0) continue;
            var foregroundWeight = total - backgroundWeight;
            if (foregroundWeight == 0) break;
            backgroundSum += (long)threshold * histogram[threshold];
            var backgroundMean = (double)backgroundSum / backgroundWeight;
            var foregroundMean = (double)(weightedSum - backgroundSum) / foregroundWeight;
            var difference = backgroundMean - foregroundMean;
            var variance = (double)backgroundWeight * foregroundWeight * difference * difference;
            if (variance > bestVariance)
            {
                bestVariance = variance;
                bestThreshold = threshold;
            }
        }
        return bestThreshold;
    }

    private static int Luminance(int red, int green, int blue) =>
        Math.Clamp((red * 299 + green * 587 + blue * 114) / 1000, 0, 255);
}
