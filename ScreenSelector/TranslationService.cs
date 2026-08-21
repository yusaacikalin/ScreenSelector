using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ScreenSelector;

internal sealed record TranslationResult(string Text, string DetectedSourceLanguage);

internal static class TranslationService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly ConcurrentDictionary<string, CachedTranslation> Cache = new();
    private static DateTimeOffset _googleBlockedUntil = DateTimeOffset.MinValue;

    public static async Task<TranslationResult> TranslateAsync(string text, string sourceLanguage,
        string targetLanguage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Çevrilecek metin boş.");
        if (text.Length > 12000)
            throw new InvalidOperationException("Çeviri için seçilen metin çok uzun. Daha küçük bir alan seçin.");
        if (sourceLanguage == targetLanguage)
            return new TranslationResult(text, sourceLanguage);

        var cacheKey = $"{sourceLanguage}|{targetLanguage}|{text}";
        if (Cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            return cached.Result;

        TranslationResult? result = null;
        if (DateTimeOffset.UtcNow >= _googleBlockedUntil)
        {
            try
            {
                result = await TranslateWithGoogleAsync(text, sourceLanguage, targetLanguage, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (HttpRequestException)
            {
                // Bağlantı veya Google uç noktası sorunu olduğunda yedek servis denenir.
            }
            catch (JsonException)
            {
                // Google beklenmeyen yanıt verdiğinde yedek servis denenir.
            }
        }

        result ??= await TranslateWithMyMemoryAsync(text, sourceLanguage, targetLanguage, cancellationToken);
        AddToCache(cacheKey, result);
        return result;
    }

    private static async Task<TranslationResult?> TranslateWithGoogleAsync(string text, string sourceLanguage,
        string targetLanguage, CancellationToken cancellationToken)
    {
        var googleSource = string.IsNullOrWhiteSpace(sourceLanguage) || sourceLanguage == "auto"
            ? "auto"
            : sourceLanguage;
        var url = "https://translate.googleapis.com/translate_a/single" +
                  $"?client=gtx&sl={Uri.EscapeDataString(googleSource)}" +
                  $"&tl={Uri.EscapeDataString(targetLanguage)}&dt=t";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["q"] = text });
        content.Headers.ContentType!.CharSet = Encoding.UTF8.WebName;
        using var response = await Client.PostAsync(url, content, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(10);
            _googleBlockedUntil = DateTimeOffset.UtcNow.Add(wait < TimeSpan.FromMinutes(2)
                ? TimeSpan.FromMinutes(2)
                : wait);
            return null;
        }
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0 ||
            root[0].ValueKind != JsonValueKind.Array)
            return null;

        var translated = new StringBuilder();
        foreach (var segment in root[0].EnumerateArray())
        {
            if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0 &&
                segment[0].ValueKind == JsonValueKind.String)
                translated.Append(segment[0].GetString());
        }

        var translatedText = WebUtility.HtmlDecode(translated.ToString()).Trim();
        if (string.IsNullOrWhiteSpace(translatedText)) return null;

        var detectedLanguage = googleSource;
        if (root.GetArrayLength() > 2 && root[2].ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(root[2].GetString()))
            detectedLanguage = root[2].GetString()!;
        if (detectedLanguage == "auto") detectedLanguage = sourceLanguage;
        return new TranslationResult(translatedText, detectedLanguage);
    }

    private static async Task<TranslationResult> TranslateWithMyMemoryAsync(string text, string sourceLanguage,
        string targetLanguage, CancellationToken cancellationToken)
    {
        var fallbackSource = string.IsNullOrWhiteSpace(sourceLanguage) || sourceLanguage == "auto"
            ? "autodetect"
            : sourceLanguage;
        var translatedParts = new List<string>();
        var detectedLanguage = sourceLanguage;

        foreach (var part in SplitForFallback(text, 450))
        {
            var url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(part) +
                      "&langpair=" + Uri.EscapeDataString($"{fallbackSource}|{targetLanguage}");
            using var response = await Client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (!root.TryGetProperty("responseData", out var responseData) ||
                !responseData.TryGetProperty("translatedText", out var translatedElement))
                throw new InvalidOperationException("Çeviri servisleri geçerli bir yanıt vermedi.");

            var translated = WebUtility.HtmlDecode(translatedElement.GetString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(translated))
                throw new InvalidOperationException("Çeviri servisleri boş yanıt verdi.");
            translatedParts.Add(translated);

            if (responseData.TryGetProperty("detectedLanguage", out var detectedElement) &&
                !string.IsNullOrWhiteSpace(detectedElement.GetString()))
                detectedLanguage = detectedElement.GetString()!;
        }

        return new TranslationResult(string.Join(Environment.NewLine, translatedParts), detectedLanguage);
    }

    private static IEnumerable<string> SplitForFallback(string text, int maximumLength)
    {
        var remaining = text.Trim();
        while (remaining.Length > maximumLength)
        {
            var splitAt = remaining.LastIndexOfAny(new[] { '\n', '.', '!', '?', ' ' }, maximumLength - 1);
            if (splitAt < maximumLength / 2) splitAt = maximumLength;
            else splitAt++;
            yield return remaining[..splitAt].Trim();
            remaining = remaining[splitAt..].TrimStart();
        }
        if (remaining.Length > 0) yield return remaining;
    }

    private static void AddToCache(string key, TranslationResult result)
    {
        if (Cache.Count >= 100)
        {
            foreach (var expired in Cache.Where(item => item.Value.ExpiresAt <= DateTimeOffset.UtcNow).Take(20))
                Cache.TryRemove(expired.Key, out _);
            if (Cache.Count >= 100 && Cache.FirstOrDefault() is var oldest && oldest.Key != null)
                Cache.TryRemove(oldest.Key, out _);
        }
        Cache[key] = new CachedTranslation(result, DateTimeOffset.UtcNow.AddMinutes(30));
    }

    private sealed record CachedTranslation(TranslationResult Result, DateTimeOffset ExpiresAt);
}
