using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Media.Control;

namespace ScreenSelector;

internal sealed class CurrentSongService(Func<string> audioToken) : IMusicCapture
{
    private readonly SemaphoreSlim _captureGate = new(1, 1);

    public async Task<CapturedSong> CaptureAsync(CancellationToken cancellationToken)
    {
        var hasPlayingSession = false;
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(cancellationToken);
            var playing = manager.GetSessions().Where(s =>
                s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing).ToArray();
            var current = manager.GetCurrentSession();
            var session = current?.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
                ? current : (playing.Length == 1 ? playing[0] : null);
            hasPlayingSession = playing.Length > 0 || session is not null;
            if (playing.Length > 1 && session is null)
                throw new InvalidOperationException("Birden fazla kaynak çalıyor. Diğerini duraklatıp tekrar yakalayın.");
            if (session is not null)
            {
                var properties = await session.TryGetMediaPropertiesAsync().AsTask(cancellationToken);
                var timeline = session.GetTimelineProperties();
                var duration = (timeline.EndTime - timeline.StartTime).TotalSeconds;
                var song = ParseMetadata(properties.Artist, properties.Title, session.SourceAppUserModelId,
                    duration > 0 ? duration : null);
                if (song is not null) return song;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (System.Runtime.InteropServices.COMException) { /* Some players do not expose media metadata. */ }

        if (string.IsNullOrWhiteSpace(audioToken()))
            throw new InvalidOperationException(hasPlayingSession
                ? "Çalan parçanın sanatçı/başlık bilgisi alınamadı. Yedek ses tanıma için AudD anahtarı girin."
                : "Şu anda çalan bir şarkı bulunamadı.");
        if (!hasPlayingSession && !await MusicRecognitionService.HasOutputAudioAsync(cancellationToken))
            throw new InvalidOperationException("Şu anda çalan bir şarkı bulunamadı.");
        // Do not queue a later recording: that could capture a different song.
        if (!await _captureGate.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("Bir ses örneği zaten kaydediliyor. Tamamlanmasını bekleyin.");
        try
        {
            var result = await MusicRecognitionService.IdentifyCurrentAudioAsync(audioToken(),
                TimeSpan.FromSeconds(8), cancellationToken);
            return new CapturedSong(result.Artist, result.Title, "AudD · bilgisayar sesi", SpotifyId: result.SpotifyId, Isrc: result.Isrc);
        }
        finally { _captureGate.Release(); }
    }

    internal static CapturedSong? ParseMetadata(string? artist, string? title, string source, double? duration)
    {
        title = SongMatching.CleanTitle(title ?? "");
        artist = Regex.Replace(artist ?? "", @"\s*(-\s*Topic|VEVO)$", "", RegexOptions.IgnoreCase).Trim();
        // Many videos put the artist and track together in the title; the Artist field may be the uploader.
        var parts = Regex.Split(title, @"\s+[-–—]\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
        if (parts.Length == 2 && !SongMatching.IsVersion(parts[1]) &&
            (source.Contains("chrome", StringComparison.OrdinalIgnoreCase) ||
             source.Contains("edge", StringComparison.OrdinalIgnoreCase) ||
             source.Contains("firefox", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(artist)))
        {
            artist = parts[0].Trim();
            title = parts[1].Trim();
        }
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist)) return null;
        return new CapturedSong(artist, title, source, duration);
    }
}

internal static class SongMatching
{
    internal static string CleanTitle(string text) => Regex.Replace(text,
        @"[\(\[]\s*(official\s*(music\s*)?(video|audio)|official|lyrics?|lyric video|hd|4k|resmi\s*(video|klip))\s*[\)\]]",
        "", RegexOptions.IgnoreCase).Trim();

    internal static string Normalize(string text)
    {
        var normalized = CleanTitle(text).Replace('İ', 'I').ToLowerInvariant().Replace('ı', 'i').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    internal static bool IsVersion(string value) => Regex.IsMatch(value,
        @"\b(live|remix|remaster(ed)?|acoustic|instrumental|karaoke|cover|sped|slowed|radio edit|canlı|akustik)\b",
        RegexOptions.IgnoreCase);

    internal static SpotifyTrack? Choose(CapturedSong song, IEnumerable<SpotifyTrack> tracks)
    {
        var matches = tracks.Where(t => !string.IsNullOrWhiteSpace(t.Id)).DistinctBy(t => t.Id).ToArray();
        if (!string.IsNullOrWhiteSpace(song.SpotifyId))
            return matches.FirstOrDefault(t => t.Id == song.SpotifyId);
        if (!string.IsNullOrWhiteSpace(song.Isrc))
        {
            var exact = matches.FirstOrDefault(t => string.Equals(t.Isrc, song.Isrc, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return exact;
        }
        var artist = Normalize(song.Artist);
        var title = Normalize(song.Title);
        var reliable = matches.Where(t => Normalize(t.Title) == title &&
            (t.Artists.Any(a => Normalize(a) == artist) || Normalize(string.Join(" ", t.Artists)) == artist) &&
            (song.DurationSeconds is null || Math.Abs(t.DurationSeconds - song.DurationSeconds.Value) <= 12))
            .OrderBy(t => song.DurationSeconds is null ? 0 : Math.Abs(t.DurationSeconds - song.DurationSeconds.Value)).ToArray();
        if (reliable.Length == 0) return null;
        // Multiple releases of one recording are fine; genuinely different recordings need review.
        if (reliable.Length > 1 && reliable.Select(t => t.Isrc ?? t.Id).Distinct().Count() > 1 &&
            (song.DurationSeconds is null || Math.Abs(reliable[0].DurationSeconds - reliable[1].DurationSeconds) < 3))
            return null;
        return reliable[0];
    }
}

internal sealed class SongLanguageService : ISongClassifier
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly Dictionary<string, SongClassification> _cache = new();
    private readonly string _path;

    internal SongLanguageService(string? path = null)
    {
        _path = path ?? Path.Combine(AppSettings.SettingsDirectory, "song-categories.json");
        try
        {
            if (File.Exists(_path)) _cache = JsonSerializer.Deserialize<Dictionary<string, SongClassification>>(File.ReadAllText(_path)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
    }

    private static string Key(CapturedSong song) => SongMatching.Normalize(song.Artist) + "|" + SongMatching.Normalize(song.Title);

    internal void Remember(CapturedSong song, SongCategory category)
    {
        _cache[Key(song)] = new(category, "Kullanıcının bu şarkı için seçimi.");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_cache));
        File.Move(temp, _path, true);
    }

    public async Task<SongClassification> ClassifyAsync(CapturedSong song, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(Key(song), out var cached)) return cached;
        try
        {
            var url = "https://lrclib.net/api/search?artist_name=" + Uri.EscapeDataString(song.Artist) +
                      "&track_name=" + Uri.EscapeDataString(song.Title);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("ScreenSelector/1.0");
            using var response = await Client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.ValueKind != JsonValueKind.Array) return Unknown("Şarkı sözü yanıtı okunamadı.");
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (SongMatching.Normalize(item.GetProperty("artistName").GetString() ?? "") != SongMatching.Normalize(song.Artist) ||
                    SongMatching.Normalize(item.GetProperty("trackName").GetString() ?? "") != SongMatching.Normalize(song.Title)) continue;
                if (song.DurationSeconds.HasValue && item.TryGetProperty("duration", out var duration) &&
                    Math.Abs(duration.GetDouble() - song.DurationSeconds.Value) > 12) continue;
                if (item.TryGetProperty("instrumental", out var instrumental) && instrumental.ValueKind == JsonValueKind.True)
                    return Unknown("Enstrümantal kayıt; dili belirlenemedi.");
                if (!item.TryGetProperty("plainLyrics", out var lyricsElement) || lyricsElement.ValueKind != JsonValueKind.String) continue;
                var lyrics = Regex.Replace(lyricsElement.GetString() ?? "", @"\[[^\]]*\]", "").Trim();
                if (lyrics.Length < 120) continue;
                // Classify separate portions; do not infer the language from the performer's nationality.
                var half = lyrics.Length / 2;
                var samples = new[] { lyrics[..Math.Min(400, half)], lyrics[half..Math.Min(lyrics.Length, half + 400)] };
                var languages = new List<string>();
                foreach (var sample in samples)
                    languages.Add((await TranslationService.TranslateAsync(sample, "auto", "en", cancellationToken)).DetectedSourceLanguage);
                var category = FromLanguages(languages);
                var result = new SongClassification(category, category == SongCategory.Unknown
                    ? "Şarkı sözlerinde dil belirsiz veya birden fazla dil var."
                    : "Eşleşen şarkı sözlerinden algılandı: " + string.Join(", ", languages.Distinct()));
                if (category != SongCategory.Unknown) _cache[Key(song)] = result;
                return result;
            }
            return Unknown("Sanatçı ve başlıkla eşleşen yeterli şarkı sözü bulunamadı.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or OperationCanceledException)
        {
            return Unknown("Dil servisine ulaşılamadı. Günlükten Yerli / Yabancı seçebilirsiniz.");
        }
    }

    internal static SongCategory FromLanguages(IEnumerable<string> languages)
    {
        var values = languages.ToArray();
        if (values.Length < 2 || values.Any(v => string.IsNullOrWhiteSpace(v) || v is "auto" or "autodetect")) return SongCategory.Unknown;
        if (values.All(v => v == "tr")) return SongCategory.Local;
        return values.All(v => v != "tr") ? SongCategory.Foreign : SongCategory.Unknown;
    }
    private static SongClassification Unknown(string reason) => new(SongCategory.Unknown, reason);
}
