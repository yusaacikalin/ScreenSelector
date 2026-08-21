using System.Text.Json;
using NAudio.Wave;

namespace ScreenSelector;

public sealed record MusicRecognitionResult(string Artist, string Title, string Album, string? Link);

internal static class MusicRecognitionService
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(45)
    };

    public static async Task<MusicRecognitionResult> IdentifyCurrentAudioAsync(string apiToken,
        TimeSpan duration, CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"ScreenSelector-{Guid.NewGuid():N}.wav");
        try
        {
            await RecordLoopbackAsync(tempFile, duration, cancellationToken);
            var audioBytes = await File.ReadAllBytesAsync(tempFile, cancellationToken);
            if (audioBytes.Length < 4096)
                throw new InvalidOperationException("Bilgisayar çıkışından yeterli ses alınamadı. Bir parçanın çaldığından emin olun.");

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(apiToken), "api_token");
            form.Add(new StringContent("apple_music,spotify"), "return");
            var audio = new ByteArrayContent(audioBytes);
            audio.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            form.Add(audio, "file", "screen-selector.wav");

            using var response = await Client.PostAsync("https://api.audd.io/", form, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if (root.TryGetProperty("status", out var status) && status.GetString() == "error")
            {
                var message = root.TryGetProperty("error", out var error) &&
                              error.TryGetProperty("error_message", out var errorMessage)
                    ? errorMessage.GetString()
                    : "Şarkı tanıma servisi isteği reddetti.";
                throw new InvalidOperationException(message);
            }

            if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Bu ses örneği için eşleşen bir şarkı bulunamadı. Müziğin sesini yükseltip tekrar deneyin.");

            return new MusicRecognitionResult(
                GetString(result, "artist", "Bilinmeyen sanatçı"),
                GetString(result, "title", "Bilinmeyen parça"),
                GetString(result, "album", "Albüm bilgisi yok"),
                result.TryGetProperty("song_link", out var link) ? link.GetString() : null);
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    private static async Task RecordLoopbackAsync(string outputPath, TimeSpan duration,
        CancellationToken cancellationToken)
    {
        using var capture = new WasapiLoopbackCapture();
        using var writer = new WaveFileWriter(outputPath, capture.WaveFormat);
        var stopped = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        capture.DataAvailable += (_, args) => writer.Write(args.Buffer, 0, args.BytesRecorded);
        capture.RecordingStopped += (_, args) => stopped.TrySetResult(args.Exception);

        capture.StartRecording();
        try
        {
            await Task.Delay(duration, cancellationToken);
        }
        finally
        {
            capture.StopRecording();
            var error = await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await writer.FlushAsync(cancellationToken);
            if (error != null) throw error;
        }
    }

    private static string GetString(JsonElement element, string propertyName, string fallback) =>
        element.TryGetProperty(propertyName, out var property) && !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()!
            : fallback;
}
