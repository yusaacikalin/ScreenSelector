using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;

namespace ScreenSelector;

internal sealed record SpotifyToken(string ClientId, string AccessToken, string RefreshToken,
    DateTimeOffset ExpiresAt, string Scope);

internal sealed class SpotifyConnection(Func<string> clientId)
{
    internal const string RedirectUri = "http://127.0.0.1:8888/callback/";
    internal const string Scopes = "playlist-read-private playlist-modify-private playlist-modify-public";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly SemaphoreSlim _tokenGate = new(1, 1);
    private SpotifyToken? _token;
    private bool _loaded;
    private static string TokenPath => Path.Combine(AppSettings.SettingsDirectory, "spotify-session.bin");
    internal bool HasSavedConnection => File.Exists(TokenPath);

    internal static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    internal async Task ConnectAsync(Action<Uri> openAuthorization, CancellationToken cancellationToken)
    {
        var id = clientId().Trim();
        if (id.Length != 32 || !id.All(Uri.IsHexDigit))
            throw new InvalidOperationException("Spotify geliştirici panelindeki 32 karakterli Client ID değerini girin.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(4));
        var ct = timeout.Token;
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(64));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        // A loopback-only TCP listener works as a normal Windows user without URL ACL changes.
        using var listener = new TcpListener(IPAddress.Loopback, 8888);
        try { listener.Start(); }
        catch (SocketException) { throw new InvalidOperationException("Spotify bağlantısı için 8888 portu kullanımda. Önceki bağlantı penceresini kapatıp tekrar deneyin."); }
        var url = "https://accounts.spotify.com/authorize?response_type=code&client_id=" + Uri.EscapeDataString(id) +
            "&redirect_uri=" + Uri.EscapeDataString(RedirectUri) + "&scope=" + Uri.EscapeDataString(Scopes) +
            "&state=" + state + "&code_challenge_method=S256&code_challenge=" + challenge;
        openAuthorization(new Uri(url));
        var code = await ReceiveCodeAsync(listener, state, ct);
        await _tokenGate.WaitAsync(ct);
        try
        {
            _token = await ExchangeAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["code"] = code,
                ["redirect_uri"] = RedirectUri, ["client_id"] = id, ["code_verifier"] = verifier
            }, id, null, ct);
            await SaveAsync(_token, ct);
            _loaded = true;
        }
        finally { _tokenGate.Release(); }
    }

    internal static async Task<string> ReceiveCodeAsync(TcpListener listener, string expectedState, CancellationToken ct)
    {
        while (true)
        {
            using var socket = await listener.AcceptTcpClientAsync(ct);
            using var stream = socket.GetStream();
            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            requestTimeout.CancelAfter(TimeSpan.FromSeconds(5));
            var buffer = new byte[8192];
            var length = 0;
            try
            {
                while (length < buffer.Length)
                {
                    var count = await stream.ReadAsync(buffer.AsMemory(length), requestTimeout.Token);
                    if (count == 0) break;
                    length += count;
                    if (Encoding.ASCII.GetString(buffer, 0, length).Contains("\r\n\r\n")) break;
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { continue; }
            var firstLine = Encoding.ASCII.GetString(buffer, 0, length).Split("\r\n")[0].Split(' ');
            var valid = firstLine.Length == 3 && firstLine[0] == "GET" && firstLine[1].StartsWith("/callback/?", StringComparison.Ordinal);
            var query = valid ? ParseQuery(new Uri("http://127.0.0.1:8888" + firstLine[1]).Query) : new Dictionary<string, string>();
            valid &= query.TryGetValue("state", out var receivedState) && receivedState == expectedState;
            var error = query.ContainsKey("error");
            var code = query.GetValueOrDefault("code");
            var accepted = valid && !error && !string.IsNullOrEmpty(code);
            var body = accepted ? "Spotify izni alındı. ScreenSelector'a dönebilirsiniz." :
                valid && error ? "Spotify bağlantı izni verilmedi. ScreenSelector'a dönebilirsiniz." : "Geçersiz dönüş adresi veya bağlantı isteği.";
            var bytes = Encoding.UTF8.GetBytes("<!doctype html><html lang=\"tr\"><meta charset=\"utf-8\"><title>ScreenSelector · Spotify</title><body><p>" + body + "</p></body></html>");
            var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {(accepted ? "200 OK" : "400 Bad Request")}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header, ct);
            await stream.WriteAsync(bytes, ct);
            if (accepted) return code!;
            if (valid && error) throw new InvalidOperationException("Spotify bağlantı izni verilmedi.");
        }
    }

    private static Dictionary<string, string> ParseQuery(string query) => query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key,
            g => g.First().Length == 2 ? Uri.UnescapeDataString(g.First()[1].Replace('+', ' ')) : "");

    internal async Task<string> GetAccessTokenAsync(CancellationToken ct, bool refresh = false)
    {
        await _tokenGate.WaitAsync(ct);
        try
        {
            if (!_loaded)
            {
                try
                {
                    if (File.Exists(TokenPath))
                    {
                        var bytes = await File.ReadAllBytesAsync(TokenPath, ct);
                        var plain = await new DataProtectionProvider().UnprotectAsync(CryptographicBuffer.CreateFromByteArray(bytes)).AsTask(ct);
                        _token = JsonSerializer.Deserialize<SpotifyToken>(CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, plain));
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new InvalidOperationException("Kaydedilmiş Spotify bağlantısı okunamadı. Yeniden bağlanın.");
                }
                _loaded = true;
            }
            if (_token is null || _token.ClientId != clientId().Trim())
                throw new InvalidOperationException("Spotify bağlı değil. Şarkı menüsünden hesabınızı bağlayın; bu kayıt günlükte saklandı.");
            if (refresh || _token.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(1))
            {
                _token = await ExchangeAsync(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token", ["refresh_token"] = _token.RefreshToken, ["client_id"] = _token.ClientId
                }, _token.ClientId, _token, ct);
                await SaveAsync(_token, ct);
            }
            return _token.AccessToken;
        }
        finally { _tokenGate.Release(); }
    }

    internal async Task DisconnectAsync()
    {
        await _tokenGate.WaitAsync();
        try
        {
            if (File.Exists(TokenPath)) File.Delete(TokenPath);
            _token = null;
            _loaded = true;
        }
        finally { _tokenGate.Release(); }
    }

    private static async Task<SpotifyToken> ExchangeAsync(Dictionary<string, string> values, string id, SpotifyToken? previous, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(values);
        using var response = await Client.PostAsync("https://accounts.spotify.com/api/token", content, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Spotify oturumu yenilenemedi veya giriş reddedildi. Client ID ve dönüş adresini kontrol edip yeniden bağlanın.");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        var refresh = root.TryGetProperty("refresh_token", out var token) ? token.GetString() : previous?.RefreshToken;
        if (string.IsNullOrWhiteSpace(refresh)) throw new InvalidOperationException("Spotify yenileme anahtarı vermedi. Yeniden bağlanın.");
        return new SpotifyToken(id, root.GetProperty("access_token").GetString()!, refresh,
            DateTimeOffset.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32()),
            root.TryGetProperty("scope", out var scope) ? scope.GetString() ?? "" : previous?.Scope ?? "");
    }

    private static async Task SaveAsync(SpotifyToken token, CancellationToken ct)
    {
        var data = CryptographicBuffer.ConvertStringToBinary(JsonSerializer.Serialize(token), BinaryStringEncoding.Utf8);
        var protectedData = await new DataProtectionProvider("LOCAL=user").ProtectAsync(data).AsTask(ct);
        CryptographicBuffer.CopyToByteArray(protectedData, out var bytes);
        Directory.CreateDirectory(AppSettings.SettingsDirectory);
        var temp = TokenPath + ".tmp";
        await File.WriteAllBytesAsync(temp, bytes, ct);
        File.Move(temp, TokenPath, true);
    }
}
