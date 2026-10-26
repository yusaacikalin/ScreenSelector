using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ScreenSelector;

internal sealed class SpotifyLibraryService : ISpotifyLibrary
{
    private readonly Func<CancellationToken, bool, Task<string>> _getToken;
    private readonly Func<AppSettings> _settings;
    private readonly HttpClient _client;
    internal string AccountName { get; private set; } = "";
    private string _userId = "";
    internal SpotifyLibraryService(SpotifyConnection connection, Func<AppSettings> settings, HttpClient? client = null)
    {
        _getToken = (ct, refresh) => connection.GetAccessTokenAsync(ct, refresh);
        _settings = settings;
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    internal SpotifyLibraryService(Func<CancellationToken, bool, Task<string>> getToken, Func<AppSettings> settings, HttpClient client)
    {
        _getToken = getToken;
        _settings = settings;
        _client = client;
    }

    internal void ResetAccount() { _userId = ""; AccountName = ""; }

    internal static SpotifyPlaylist? ChooseDefaultPlaylist(IReadOnlyList<SpotifyPlaylist> lists, string selectedId, string name)
    {
        var selected = lists.FirstOrDefault(p => p.Id == selectedId);
        if (selected is not null) return selected;
        var matches = lists.Where(p => SongMatching.Normalize(p.Name) == SongMatching.Normalize(name)).ToArray();
        if (matches.Length == 0 && name == "Yerli")
            matches = lists.Where(p => SongMatching.Normalize(p.Name) == "turkce").ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private async Task<JsonDocument> ReadAsync(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, ct);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? body, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, "https://api.spotify.com/v1/" + path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                await _getToken(ct, attempt == 1));
            if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request, ct);
            // Only refresh/retry reads. Never blindly retry playlist writes.
            if (response.StatusCode == HttpStatusCode.Unauthorized && method == HttpMethod.Get && attempt == 0)
            {
                response.Dispose();
                continue;
            }
            if (response.IsSuccessStatusCode) return response;
            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Spotify bağlantısının süresi dolmuş. Yeniden bağlanın.",
                HttpStatusCode.Forbidden => "Spotify erişimi reddetti. Premium, izin verilen kullanıcılar ve liste düzenleme izinlerini kontrol edin.",
                HttpStatusCode.TooManyRequests => "Spotify istek sınırına ulaşıldı. Biraz sonra günlükten yeniden deneyin.",
                HttpStatusCode.NotFound => "Spotify parçası veya hedef liste bulunamadı. Liste bağlantısını kontrol edin.",
                _ => $"Spotify isteği tamamlanamadı (HTTP {(int)response.StatusCode})."
            };
            var status = response.StatusCode;
            response.Dispose();
            if ((int)status >= 500 && method != HttpMethod.Get) throw new HttpRequestException(message);
            throw new SpotifyRequestRejectedException(message);
        }
    }

    internal async Task<IReadOnlyList<SpotifyPlaylist>> GetOwnedPlaylistsAsync(CancellationToken ct)
    {
        using (var profile = await ReadAsync("me", ct))
        {
            _userId = profile.RootElement.GetProperty("id").GetString()!;
            AccountName = profile.RootElement.TryGetProperty("display_name", out var name) ? name.GetString() ?? _userId : _userId;
        }
        var lists = new List<SpotifyPlaylist>();
        for (var offset = 0; ; offset += 50)
        {
            using var document = await ReadAsync($"me/playlists?limit=50&offset={offset}", ct);
            var root = document.RootElement;
            foreach (var item in root.GetProperty("items").EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("owner", out var owner) ||
                    owner.GetProperty("id").GetString() != _userId) continue;
                lists.Add(new SpotifyPlaylist(item.GetProperty("id").GetString()!, item.GetProperty("name").GetString()!));
            }
            if (!root.TryGetProperty("next", out var next) || next.ValueKind == JsonValueKind.Null) break;
        }
        return lists;
    }

    public async Task<SpotifyPlaylist> GetDestinationAsync(SongCategory category, CancellationToken ct)
    {
        if (category == SongCategory.Unknown) throw new InvalidOperationException("Şarkının Yerli / Yabancı türü seçilmedi.");
        var settings = _settings();
        if (!string.IsNullOrWhiteSpace(settings.SpotifyLocalPlaylistId) &&
            settings.SpotifyLocalPlaylistId == settings.SpotifyForeignPlaylistId)
            throw new InvalidOperationException("Yerli ve Yabancı için farklı hedef listeler seçin.");
        var name = category == SongCategory.Local ? "Yerli" : "Yabancı";
        var id = category == SongCategory.Local ? settings.SpotifyLocalPlaylistId : settings.SpotifyForeignPlaylistId;
        if (string.IsNullOrWhiteSpace(id))
        {
            var lists = await GetOwnedPlaylistsAsync(ct);
            return ChooseDefaultPlaylist(lists, "", name) ??
                throw new InvalidOperationException($"{name} için tek bir hedef bulunamadı. Şarkı menüsünden hedef listeyi seçin.");
        }
        if (string.IsNullOrEmpty(_userId)) await GetOwnedPlaylistsAsync(ct);
        using var document = await ReadAsync("playlists/" + Uri.EscapeDataString(id), ct);
        var playlist = document.RootElement;
        if (playlist.GetProperty("owner").GetProperty("id").GetString() != _userId)
            throw new InvalidOperationException("Seçili hedef liste bağlı Spotify hesabına ait değil. Hedef listeleri yeniden seçin.");
        return new SpotifyPlaylist(id, playlist.GetProperty("name").GetString() ?? name);
    }

    public async Task<SpotifyTrack?> FindAsync(CapturedSong song, CancellationToken ct)
    {
        if (song.SpotifyId is not null)
        {
            using var direct = await ReadAsync("tracks/" + Uri.EscapeDataString(song.SpotifyId), ct);
            return ParseTrack(direct.RootElement);
        }
        return SongMatching.Choose(song, await FindCandidatesAsync(song, ct));
    }

    internal async Task<IReadOnlyList<SpotifyTrack>> FindCandidatesAsync(CapturedSong song, CancellationToken ct)
    {
        var query = song.Isrc is null ? $"track:{song.Title} artist:{song.Artist}" : "isrc:" + song.Isrc;
        using var document = await ReadAsync("search?type=track&limit=10&q=" + Uri.EscapeDataString(query), ct);
        var tracks = document.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray()
            .Where(item => !item.TryGetProperty("is_playable", out var playable) || playable.ValueKind != JsonValueKind.False)
            .Select(ParseTrack).ToArray();
        return tracks;
    }

    internal static SpotifyTrack ParseTrack(JsonElement item) => new(item.GetProperty("id").GetString()!,
        item.GetProperty("name").GetString()!,
        item.GetProperty("artists").EnumerateArray().Select(a => a.GetProperty("name").GetString() ?? "").ToArray(),
        item.GetProperty("duration_ms").GetDouble() / 1000,
        item.TryGetProperty("external_ids", out var ids) && ids.TryGetProperty("isrc", out var isrc) ? isrc.GetString() : null);

    public async Task<bool> ContainsAsync(string playlistId, SpotifyTrack track, CancellationToken ct)
    {
        for (var offset = 0; ; offset += 50)
        {
            using var document = await ReadAsync($"playlists/{Uri.EscapeDataString(playlistId)}/items?limit=50&offset={offset}", ct);
            var root = document.RootElement;
            foreach (var entry in root.GetProperty("items").EnumerateArray())
            {
                if (!entry.TryGetProperty("item", out var item) && !entry.TryGetProperty("track", out item)) continue;
                if (item.ValueKind != JsonValueKind.Object) continue;
                if (item.TryGetProperty("id", out var id) && id.GetString() == track.Id) return true;
                if (!string.IsNullOrWhiteSpace(track.Isrc) && item.TryGetProperty("external_ids", out var ids) &&
                    ids.TryGetProperty("isrc", out var isrc) && string.Equals(isrc.GetString(), track.Isrc, StringComparison.OrdinalIgnoreCase)) return true;
            }
            if (!root.TryGetProperty("next", out var next) || next.ValueKind == JsonValueKind.Null) return false;
        }
    }

    public async Task AddAsync(string playlistId, SpotifyTrack track)
    {
        using var response = await SendAsync(HttpMethod.Post, "playlists/" + Uri.EscapeDataString(playlistId) + "/items",
            JsonSerializer.Serialize(new { uris = new[] { track.Uri } }), CancellationToken.None);
        if (response.StatusCode != HttpStatusCode.Created)
            throw new HttpRequestException("Spotify ekleme sonucunu doğrulamadı.");
    }
}
