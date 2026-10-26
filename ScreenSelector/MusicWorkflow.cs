using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenSelector;

public enum SongCategory { Unknown, Local, Foreign }
public enum MusicStage
{
    Capturing, Classifying, Captured, Searching, Found, Adding, Added, AlreadyExists,
    NeedsReview, Cancelled, Failed, Uncertain, Interrupted, OnHold
}

public sealed record CapturedSong(string Artist, string Title, string Source,
    double? DurationSeconds = null, string? SpotifyId = null, string? Isrc = null);
public sealed record SpotifyTrack(string Id, string Title, string[] Artists, double DurationSeconds,
    string? Isrc = null)
{
    public string Uri => "spotify:track:" + Id;
}
public sealed record SpotifyPlaylist(string Id, string Name);
public sealed record SongClassification(SongCategory Category, string Reason);
public sealed record MusicLogEvent(DateTimeOffset Time, string Message);

public sealed class MusicJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;
    public CapturedSong? Song { get; set; }
    public SongCategory Category { get; set; }
    public string CategoryReason { get; set; } = "";
    public SpotifyTrack? SpotifyMatch { get; set; }
    public string PlaylistName { get; set; } = "";
    public MusicStage Stage { get; set; } = MusicStage.Capturing;
    public string Status { get; set; } = "Şarkı yakalanıyor…";
    public List<MusicLogEvent> Events { get; set; } = [];
    [JsonIgnore] public bool CanCancel => Stage is MusicStage.Capturing or MusicStage.Classifying or
        MusicStage.Captured or MusicStage.Searching or MusicStage.Found;
    [JsonIgnore] public bool IsRunning => CanCancel || Stage == MusicStage.Adding;
    [JsonIgnore] public string CategoryLabel => Category switch
    {
        SongCategory.Local => "Yerli", SongCategory.Foreign => "Yabancı", _ => "Belirsiz"
    };
    [JsonIgnore] public string Description => Song is null ? "Çalan medya okunuyor" :
        $"{Song.Artist} - {Song.Title} ({CategoryLabel})";
}

internal interface IMusicCapture
{
    Task<CapturedSong> CaptureAsync(CancellationToken cancellationToken);
}
internal interface ISongClassifier
{
    Task<SongClassification> ClassifyAsync(CapturedSong song, CancellationToken cancellationToken);
}
internal interface ISpotifyLibrary
{
    Task<SpotifyTrack?> FindAsync(CapturedSong song, CancellationToken cancellationToken);
    Task<SpotifyPlaylist> GetDestinationAsync(SongCategory category, CancellationToken cancellationToken);
    Task<bool> ContainsAsync(string playlistId, SpotifyTrack track, CancellationToken cancellationToken);
    Task AddAsync(string playlistId, SpotifyTrack track);
}

internal sealed class MusicWorkflow(IMusicCapture capture, ISongClassifier classifier, ISpotifyLibrary spotify)
{
    // Serializes duplicate checking and committing across simultaneous captures.
    private readonly SemaphoreSlim _playlistGate = new(1, 1);
    internal TimeSpan StageDelay { get; init; } = TimeSpan.FromMilliseconds(1200);
    internal event Action<MusicJob>? Changed;

    internal void Update(MusicJob job, MusicStage stage, string message)
    {
        job.Stage = stage;
        job.Status = message;
        job.Events.Add(new MusicLogEvent(DateTimeOffset.Now, message));
        Changed?.Invoke(job);
    }

    internal async Task RunAsync(MusicJob job, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (job.Song is null)
            {
                Update(job, MusicStage.Capturing, "Şarkı yakalanıyor…");
                job.Song = await capture.CaptureAsync(cancellationToken);
                Update(job, MusicStage.Classifying, $"Şarkı bulundu: {job.Song.Artist} - {job.Song.Title}. Dil belirleniyor…");
            }
            if (job.Category == SongCategory.Unknown)
            {
                var classification = await classifier.ClassifyAsync(job.Song, cancellationToken);
                job.Category = classification.Category;
                job.CategoryReason = classification.Reason;
            }
            cancellationToken.ThrowIfCancellationRequested();
            Update(job, MusicStage.Captured, $"Şarkı Yakalandı: {job.Description}. {job.CategoryReason}");
            await Task.Delay(StageDelay, cancellationToken);
            if (job.Category == SongCategory.Unknown)
            {
                Update(job, MusicStage.NeedsReview, "Dil belirlenemedi. Günlükten Yerli veya Yabancı seçin; otomatik ekleme bekletildi.");
                return;
            }
            Update(job, MusicStage.Searching, "Spotify'da aranıyor…");
            job.SpotifyMatch = await spotify.FindAsync(job.Song, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (job.SpotifyMatch is null)
            {
                Update(job, MusicStage.NeedsReview, "Spotify'da güvenilir eşleşme bulunamadı. Elle arayabilirsiniz.");
                return;
            }
            Update(job, MusicStage.Found, $"Spotify'da Bulundu: {string.Join(", ", job.SpotifyMatch.Artists)} - {job.SpotifyMatch.Title}");
            await Task.Delay(StageDelay, cancellationToken);
            var destination = await spotify.GetDestinationAsync(job.Category, cancellationToken);
            job.PlaylistName = destination.Name;
            await _playlistGate.WaitAsync(cancellationToken);
            try
            {
                if (await spotify.ContainsAsync(destination.Id, job.SpotifyMatch, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Update(job, MusicStage.AlreadyExists, $"{destination.Name} listesinde zaten var; tekrar eklenmedi.");
                    return;
                }
                cancellationToken.ThrowIfCancellationRequested();
                // The POST is the commit boundary. Once sent, cancellation cannot honestly
                // promise that Spotify did not add the track. Do not cancel or retry this POST.
                Update(job, MusicStage.Adding, $"{destination.Name} listesine ekleniyor…");
                try
                {
                    await spotify.AddAsync(destination.Id, job.SpotifyMatch);
                    Update(job, MusicStage.Added, $"Listeye Eklendi: {destination.Name}");
                }
                catch (SpotifyRequestRejectedException ex)
                {
                    Update(job, MusicStage.Failed, ex.Message);
                }
                catch (Exception)
                {
                    Update(job, MusicStage.Uncertain,
                        "Spotify ekleme yanıtı alınamadı. Sonuç belirsiz; tekrar denemeden önce liste kontrol edilecek.");
                }
            }
            finally { _playlistGate.Release(); }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Update(job, MusicStage.Cancelled, "İşlem durduruldu. Spotify'a eklenmedi; şarkıyı elle bulabilirsiniz.");
        }
        catch (Exception ex)
        {
            Update(job, MusicStage.Failed, ex is OperationCanceledException
                ? "İstek zaman aşımına uğradı. Günlükten yeniden deneyebilirsiniz." : ex.Message);
        }
    }
}

internal sealed class MusicJournal(string path)
{
    internal static string DefaultPath => Path.Combine(AppSettings.SettingsDirectory, "music-history.jsonl");

    internal IReadOnlyList<MusicJob> Load()
    {
        if (!File.Exists(path)) return [];
        var jobs = new Dictionary<Guid, MusicJob>();
        foreach (var line in File.ReadLines(path))
        {
            try
            {
                var job = JsonSerializer.Deserialize<MusicJob>(line);
                if (job is not null) jobs[job.Id] = job;
            }
            catch (JsonException) { /* A crash may leave the last line incomplete. */ }
        }
        foreach (var job in jobs.Values.Where(j => j.IsRunning))
        {
            job.Status = job.Stage == MusicStage.Adding
                ? "Uygulama ekleme sırasında kapandı. Yeniden denemeden önce liste kontrol edilecek."
                : "Önceki işlem uygulama kapanınca kesildi; otomatik sürdürülmedi.";
            job.Stage = MusicStage.Interrupted;
        }
        return jobs.Values.OrderByDescending(j => j.StartedAt).Take(500).ToArray();
    }

    internal void Append(MusicJob job)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        var needsNewline = false;
        if (stream.Length > 0)
        {
            stream.Seek(-1, SeekOrigin.End);
            needsNewline = stream.ReadByte() != '\n';
        }
        stream.Seek(0, SeekOrigin.End);
        using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
        if (needsNewline) writer.WriteLine();
        writer.WriteLine(JsonSerializer.Serialize(job));
    }

    internal void Delete(Guid id)
    {
        if (!File.Exists(path)) return;
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            // Remove every snapshot of this record so older states cannot reappear on restart.
            // Preserve the rest of the file, including records beyond the visible history limit.
            using (var writer = new StreamWriter(temporaryPath, false, new System.Text.UTF8Encoding(false)))
            {
                foreach (var line in File.ReadLines(path))
                {
                    try
                    {
                        if (JsonSerializer.Deserialize<MusicJob>(line)?.Id == id) continue;
                    }
                    catch (JsonException) { }
                    writer.WriteLine(line);
                }
            }
            File.Move(temporaryPath, path, true);
        }
        finally { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
    }
}

internal sealed class SpotifyRequestRejectedException(string message) : Exception(message);
