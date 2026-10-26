using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using ScreenSelector;

internal static class MusicChecks
{
    private static readonly CapturedSong Song = new("Demet Akalın", "Binlerce Dansöz Var", "Test oynatıcı", 220);
    private static readonly SpotifyTrack Track = new("track-id", Song.Title, [Song.Artist], 220, "TR123");
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    internal static async Task RunAsync()
    {
        CheckMatching();
        await CheckWorkflowAsync();
        await CheckApiAsync();
        await CheckOAuthAsync();
        CheckJournal();
        Console.WriteLine("PASS: music matching, routing, cancellation, concurrent deduplication, API and OAuth checks");
    }

    internal static async Task CheckLiveSpotifyAsync()
    {
        var settings = AppSettings.Load();
        var connection = new SpotifyConnection(() => settings.SpotifyClientId);
        var library = new SpotifyLibraryService(connection, () => settings);
        var playlists = await library.GetOwnedPlaylistsAsync(CancellationToken.None);
        foreach (var playlist in playlists) Console.WriteLine($"Playlist: {playlist.Name} [{playlist.Id}]");
        var classifier = new SongLanguageService();
        foreach (var song in new[] { new CapturedSong("Tarkan", "Şımarık", "Salt okunur doğrulama", 235),
                     new CapturedSong("Adele", "Hello", "Salt okunur doğrulama", 295) })
        {
            var match = await library.FindAsync(song, CancellationToken.None);
            Console.WriteLine(match is null ? $"Search: {song.Artist} - {song.Title}: no unambiguous match" : $"Search: {string.Join(", ", match.Artists)} - {match.Title}");
            if (match is null)
                foreach (var candidate in await library.FindCandidatesAsync(song, CancellationToken.None))
                    Console.WriteLine($"Candidate: {string.Join(", ", candidate.Artists)} - {candidate.Title} ({candidate.DurationSeconds}s) [{candidate.Isrc}]");
            var classification = await classifier.ClassifyAsync(song, CancellationToken.None);
            Console.WriteLine($"Classification: {classification.Category}. {classification.Reason}");
            if (match is not null && classification.Category != SongCategory.Unknown)
            {
                var target = await library.GetDestinationAsync(classification.Category, CancellationToken.None);
                var contains = await library.ContainsAsync(target.Id, match, CancellationToken.None);
                Console.WriteLine($"Destination: {target.Name}; already exists: {contains}");
            }
        }
        Console.WriteLine("READ ONLY: no playlist write was performed.");
    }

    private static void CheckMatching()
    {
        var parsed = CurrentSongService.ParseMetadata("Uploader", "Demet Akalın - Binlerce Dansöz Var (Official Video)", "chrome.exe", 220);
        Require(parsed?.Artist == Song.Artist && parsed.Title == Song.Title, "YouTube artist/title were not separated");
        Require(CurrentSongService.ParseMetadata("", "Video title", "chrome.exe", null) is null, "Missing artist was accepted");
        Require(SongMatching.Choose(Song, [Track]) == Track, "Exact recording did not match");
        Require(SongMatching.Choose(Song, [Track with { Artists = ["Cover singer"] }]) is null, "Cover performer was accepted");
        Require(SongMatching.Choose(Song, [Track with { Title = Song.Title + " (Live)" }]) is null, "Live version was accepted");
        Require(SongMatching.Choose(Song, [Track, Track with { Id = "different", Isrc = "OTHER" }]) is null, "Ambiguous recordings were silently selected");
        Require(SongMatching.Choose(Song, [Track, Track with { Id = "album-release" }]) is not null, "Duplicate releases of one ISRC rejected");
        Require(SongLanguageService.FromLanguages(["tr", "tr"]) == SongCategory.Local, "Turkish routing failed");
        Require(SongLanguageService.FromLanguages(["en", "de"]) == SongCategory.Foreign, "Foreign routing failed");
        Require(SongLanguageService.FromLanguages(["tr", "en"]) == SongCategory.Unknown, "Mixed language guessed");
        Require(SongLanguageService.FromLanguages(["auto", "auto"]) == SongCategory.Unknown, "Unknown language treated as foreign");
        var lists = new[] { new SpotifyPlaylist("local", "Türkçe"), new SpotifyPlaylist("foreign", "Yabancı") };
        Require(SpotifyLibraryService.ChooseDefaultPlaylist(lists, "", "Yerli")?.Id == "local", "Türkçe list alias failed");
        Require(SpotifyLibraryService.ChooseDefaultPlaylist([.. lists, new("local2", "Türkçe")], "", "Yerli") is null,
            "Ambiguous playlist selected silently");
        Require(SpotifyLibraryService.ChooseDefaultPlaylist([new("local", "YERLİ")], "", "Yerli")?.Id == "local",
            "Turkish uppercase playlist name did not match");
    }

    private static async Task CheckWorkflowAsync()
    {
        var api = new FakeSpotify();
        var flow = Flow(api);
        var job = new MusicJob();
        var stages = new List<MusicStage>();
        flow.Changed += j => stages.Add(j.Stage);
        await flow.RunAsync(job, CancellationToken.None);
        Require(job.Stage == MusicStage.Added && api.AddCalls == 1 && api.LastCategory == SongCategory.Local, "Local workflow failed");
        Require(stages.IndexOf(MusicStage.Captured) < stages.IndexOf(MusicStage.Found) &&
            stages.IndexOf(MusicStage.Found) < stages.IndexOf(MusicStage.Added), "Notification stages out of order");
        var foreign = new MusicJob { Song = Song, Category = SongCategory.Foreign };
        var otherApi = new FakeSpotify();
        await Flow(otherApi).RunAsync(foreign, CancellationToken.None);
        Require(otherApi.LastCategory == SongCategory.Foreign, "Foreign song was sent to local list");

        foreach (var stage in new[] { MusicStage.Capturing, MusicStage.Classifying, MusicStage.Captured, MusicStage.Searching, MusicStage.Found })
        {
            using var cancellation = new CancellationTokenSource();
            api = new FakeSpotify();
            flow = Flow(api);
            flow.Changed += j => { if (j.Stage == stage) cancellation.Cancel(); };
            job = new MusicJob();
            await flow.RunAsync(job, cancellation.Token);
            Require(job.Stage == MusicStage.Cancelled && api.AddCalls == 0, $"Cancellation at {stage} added a track");
        }
        using (var cancellation = new CancellationTokenSource())
        {
            api = new FakeSpotify();
            flow = Flow(api);
            flow.Changed += j => { if (j.Stage == MusicStage.Adding) cancellation.Cancel(); };
            job = new MusicJob();
            await flow.RunAsync(job, cancellation.Token);
            Require(job.Stage == MusicStage.Added, "Committed write was falsely reported as cancelled");
        }
        api = new FakeSpotify();
        flow = Flow(api);
        var one = new MusicJob();
        var two = new MusicJob();
        await Task.WhenAll(flow.RunAsync(one, CancellationToken.None), flow.RunAsync(two, CancellationToken.None));
        Require(api.AddCalls == 1 && new[] { one.Stage, two.Stage }.Contains(MusicStage.AlreadyExists), "Concurrent duplicate tracks were added");

        api = new FakeSpotify();
        flow = new MusicWorkflow(new FakeCapture(), new FakeClassifier(SongCategory.Unknown), api) { StageDelay = TimeSpan.Zero };
        job = new MusicJob();
        await flow.RunAsync(job, CancellationToken.None);
        Require(job.Stage == MusicStage.NeedsReview && api.SearchCalls == 0, "Unknown category was routed automatically");
        api = new FakeSpotify { Match = null };
        job = new MusicJob();
        await Flow(api).RunAsync(job, CancellationToken.None);
        Require(job.Stage == MusicStage.NeedsReview && api.AddCalls == 0, "No-match song was added");
        api = new FakeSpotify { AddFailure = new HttpRequestException("Timeout") };
        job = new MusicJob();
        await Flow(api).RunAsync(job, CancellationToken.None);
        Require(job.Stage == MusicStage.Uncertain && api.AddCalls == 1, "Uncertain POST was retried or misreported");
        api = new FakeSpotify { AddFailure = new SpotifyRequestRejectedException("Forbidden") };
        job = new MusicJob();
        await Flow(api).RunAsync(job, CancellationToken.None);
        Require(job.Stage == MusicStage.Failed, "Rejected POST was reported successful");
        api = new FakeSpotify();
        flow = new MusicWorkflow(new EmptyCapture(), new FakeClassifier(SongCategory.Local), api);
        job = new MusicJob();
        await flow.RunAsync(job, CancellationToken.None);
        Require(job.Stage == MusicStage.Failed && job.Song is null && api.SearchCalls == 0 && api.AddCalls == 0,
            "An unsuccessful capture continued to Spotify");
    }

    private static MusicWorkflow Flow(FakeSpotify api) => new(new FakeCapture(), new FakeClassifier(SongCategory.Local), api) { StageDelay = TimeSpan.Zero };

    private static async Task CheckApiAsync()
    {
        var requests = new List<string>();
        var settings = new AppSettings();
        var authCalls = 0;
        using var client = new HttpClient(new Handler(async request =>
        {
            var path = request.RequestUri!.PathAndQuery;
            requests.Add(request.Method + " " + path);
            Require(request.Headers.Authorization?.Parameter == "test-token", "Missing bearer token");
            if (path == "/v1/me" && ++authCalls == 1) return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            if (path == "/v1/me") return Json("""{"id":"owner","display_name":"Test"}""");
            if (path.StartsWith("/v1/me/playlists")) return Json("""
                {"items":[{"id":"local","name":"Yerli","owner":{"id":"owner"}},
                {"id":"foreign","name":"Yabancı","owner":{"id":"owner"}},
                {"id":"other","name":"Yerli","owner":{"id":"someone-else"}}],"next":null}
                """);
            if (request.Method == HttpMethod.Get && path.Contains("/items"))
                return path.Contains("offset=50") ? Json("""{"items":[{"item":{"id":"track-id"}}],"next":null}""")
                    : Json("""{"items":[{"item":null},{"track":{"id":"unrelated"}}],"next":"another-page"}""");
            if (request.Method == HttpMethod.Post)
            {
                var body = await request.Content!.ReadAsStringAsync();
                Require(body.Contains("spotify:track:track-id") && path == "/v1/playlists/local/items", "Wrong playlist POST body/path");
                return Json("""{"snapshot_id":"snapshot"}""", HttpStatusCode.Created);
            }
            throw new Exception("Unexpected API call " + path);
        }));
        var refreshes = 0;
        var library = new SpotifyLibraryService((ct, refresh) => { if (refresh) refreshes++; return Task.FromResult("test-token"); }, () => settings, client);
        var playlists = await library.GetOwnedPlaylistsAsync(CancellationToken.None);
        Require(playlists.Count == 2 && refreshes == 1, "Ownership filtering or read-token refresh failed");
        Require((await library.GetDestinationAsync(SongCategory.Foreign, CancellationToken.None)).Id == "foreign", "Named playlist routing failed");
        Require(await library.ContainsAsync("local", Track, CancellationToken.None), "Playlist pagination failed");
        await library.AddAsync("local", Track);
        Require(requests.Count(r => r.StartsWith("POST")) == 1, "Write was repeated");
        var posts = 0;
        using var failureClient = new HttpClient(new Handler(_ =>
        { posts++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); }));
        var failing = new SpotifyLibraryService((_, _) => Task.FromResult("token"), () => settings, failureClient);
        try { await failing.AddAsync("local", Track); throw new Exception("POST 503 accepted"); }
        catch (HttpRequestException) { }
        Require(posts == 1, "POST was automatically retried");
    }

    private static async Task CheckOAuthAsync()
    {
        Require(SpotifyConnection.Base64Url([255, 255, 255]) == "____", "PKCE base64url encoding failed");
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var receive = SpotifyConnection.ReceiveCodeAsync(listener, "expected", cancellation.Token);
        using var client = new HttpClient();
        using var rejected = await client.GetAsync($"http://127.0.0.1:{port}/callback/?code=bad&state=wrong", cancellation.Token);
        Require(rejected.StatusCode == HttpStatusCode.BadRequest && !receive.IsCompleted, "OAuth state mismatch accepted");
        using var accepted = await client.GetAsync($"http://127.0.0.1:{port}/callback/?code=valid&state=expected", cancellation.Token);
        Require(accepted.IsSuccessStatusCode && await receive == "valid", "Valid OAuth callback failed");
    }

    private static void CheckJournal()
    {
        var path = Path.Combine(Path.GetTempPath(), "ScreenSelector-test-" + Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            var journal = new MusicJournal(path);
            var job = new MusicJob { Song = Song, Stage = MusicStage.Capturing };
            journal.Append(job);
            job.Stage = MusicStage.Added;
            journal.Append(job);
            var interrupted = new MusicJob { Stage = MusicStage.Adding };
            journal.Append(interrupted);
            File.AppendAllText(path, "{incomplete");
            var loaded = journal.Load();
            Require(loaded.Count == 2 && loaded.Single(j => j.Id == job.Id).Stage == MusicStage.Added, "Journal did not retain latest song state");
            Require(loaded.Single(j => j.Id == interrupted.Id).Stage == MusicStage.Interrupted, "Interrupted commit was silently resumed");
            journal.Append(new MusicJob { Stage = MusicStage.Cancelled });
            Require(journal.Load().Count == 3, "First update after a truncated log line was lost");
            journal.Delete(job.Id);
            var reopened = new MusicJournal(path);
            Require(reopened.Load().Count == 2 && reopened.Load().All(j => j.Id != job.Id),
                "Deleted history record reappeared from an older snapshot after reopening");
            Require(reopened.Load().Any(j => j.Id == interrupted.Id), "Deleting a record removed another song");
            Require(!File.ReadAllText(path).Contains(job.Id.ToString()), "An old snapshot of the deleted record remains on disk");
            reopened.Delete(job.Id);
            reopened.Append(new MusicJob { Stage = MusicStage.Failed });
            Require(reopened.Load().Count == 3, "History cannot be appended after deletion");
        }
        finally { File.Delete(path); }
    }

    internal static void RenderPreviews()
    {
        using var form = new PreviewForm();
        var temporaryHistory = Path.Combine(Path.GetTempPath(), "ScreenSelector-empty-history-" + Guid.NewGuid().ToString("N") + ".jsonl");
        typeof(Form1).GetField("_musicJournal", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, new MusicJournal(temporaryHistory));
        var jobs = (Dictionary<Guid, MusicJob>)typeof(Form1).GetField("_musicJobs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        var grid = (DataGridView)typeof(Form1).GetField("_musicGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        var change = typeof(Form1).GetMethod("MusicJobChanged", BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            var emptyJob = new MusicJob();
            jobs.Add(emptyJob.Id, emptyJob);
            change.Invoke(form, [emptyJob]);
            Require(grid.Rows.Count == 0 && !File.Exists(temporaryHistory), "Capture placeholder was written before identifying a song");
            emptyJob.Stage = MusicStage.Failed;
            emptyJob.Status = "Şu anda çalan bir şarkı bulunamadı.";
            change.Invoke(form, [emptyJob]);
            Require(grid.Rows.Count == 0 && jobs.Count == 0 && !File.Exists(temporaryHistory), "Empty capture left a history entry");
        }
        finally { File.Delete(temporaryHistory); }
        ((Label)typeof(Form1).GetField("_journalStatus", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!).Text =
            "Her şarkının yakalama, Spotify ve listeye ekleme durumu burada tutulur.";
        var method = typeof(Form1).GetMethod("btnNavMusic_Click", BindingFlags.Instance | BindingFlags.NonPublic)!;
        typeof(Form1).GetMethod("ResetMusicShortcutDisplay", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null);
        method.Invoke(form, [null, EventArgs.Empty]);
        var job = new MusicJob { Song = Song, Category = SongCategory.Local, SpotifyMatch = Track,
            PlaylistName = "Yerli", Stage = MusicStage.Added, Status = "Listeye Eklendi: Yerli" };
        job.Events.Add(new MusicLogEvent(DateTimeOffset.Now, "Şarkı Yakalandı: " + job.Description));
        job.Events.Add(new MusicLogEvent(DateTimeOffset.Now, "Spotify'da Bulundu"));
        job.Events.Add(new MusicLogEvent(DateTimeOffset.Now, "Listeye Eklendi: Yerli"));
        jobs.Add(job.Id, job);
        typeof(Form1).GetMethod("UpdateMusicRow", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, [job]);
        form.Show();
        method.Invoke(form, [null, EventArgs.Empty]);
        using (var bitmap = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(AppContext.BaseDirectory, "music-page-preview.png"), ImageFormat.Png);
        }
        var openHistory = typeof(Form1).GetMethod("OpenMusicHistory", BindingFlags.Instance | BindingFlags.NonPublic)!;
        openHistory.Invoke(form, null);
        var history = (Form)typeof(Form1).GetField("_musicHistoryForm", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        Require(history.Visible && form.Enabled && grid.FindForm() == history, "Journal must open in a separate modeless window");
        openHistory.Invoke(form, null);
        Require(form.OwnedForms.Count(f => f is MusicHistoryForm) == 1 && form.Enabled, "Repeated journal opening created duplicate or blocking windows");
        history.Close();
        Require(!history.Visible && !history.IsDisposed && form.Enabled, "Closing the journal blocked the main window or disposed live controls");
        openHistory.Invoke(form, null);
        Require(history.Visible && grid.Rows.Count == 1, "Reopening the journal lost its rows");
        using (var bitmap = new Bitmap(history.Width, history.Height))
        {
            history.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(AppContext.BaseDirectory, "music-history-preview.png"), ImageFormat.Png);
        }
        CheckManualHistoryStatuses(form, grid, job);
        history.Hide();
        form.Hide();
        using var toast = new MusicToastForm();
        toast.UpdateJob(job);
        var foreground = NativeMethods.GetForegroundWindow();
        toast.Show();
        Require(NativeMethods.GetForegroundWindow() == foreground, "Silent toast stole keyboard focus");
        using (var bitmap = new Bitmap(toast.Width, toast.Height))
        {
            toast.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(AppContext.BaseDirectory, "music-toast-preview.png"), ImageFormat.Png);
        }
        toast.Hide();
        Require(toast.Controls.OfType<Button>().Count() == 2, "Toast must have independent dismiss/stop controls");
        Require(toast.Controls.OfType<Button>().Single(b => b.AccessibleName == "Şarkı işlemini durdur").Enabled == false,
            "Completed toast offers cancellation");
        using var runningToast = new MusicToastForm();
        var stopEvents = 0;
        runningToast.StopRequested += () => stopEvents++;
        runningToast.UpdateJob(new MusicJob { Song = Song, Stage = MusicStage.Searching });
        runningToast.Show();
        runningToast.Controls.OfType<Button>().Single(b => b.AccessibleName == "Bu şarkının bildirimlerini kapat").PerformClick();
        Require(stopEvents == 0 && runningToast.IsDisposed, "Dismissing the notification also stopped the song");
        using var stopToast = new MusicToastForm();
        stopToast.StopRequested += () => stopEvents++;
        stopToast.UpdateJob(new MusicJob { Song = Song, Stage = MusicStage.Searching });
        stopToast.Show();
        stopToast.Controls.OfType<Button>().Single(b => b.AccessibleName == "Şarkı işlemini durdur").PerformClick();
        Require(stopEvents == 1, "Stop button did not request cancellation");
        stopToast.Hide();
        Console.WriteLine("PASS: music menu and silent toast rendered for visual inspection");
    }

    private static void CheckManualHistoryStatuses(Form1 form, DataGridView grid, MusicJob job)
    {
        var path = Path.Combine(Path.GetTempPath(), "ScreenSelector-manual-history-" + Guid.NewGuid().ToString("N") + ".jsonl");
        var journal = new MusicJournal(path);
        typeof(Form1).GetField("_musicJournal", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, journal);
        var hold = (Button)typeof(Form1).GetField("_holdMusicRecord", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        var added = (Button)typeof(Form1).GetField("_markMusicAdded", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
        var change = typeof(Form1).GetMethod("MusicJobChanged", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var setStatus = typeof(Form1).GetMethod("SetSelectedMusicStatus", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var row = grid.Rows.Cast<DataGridViewRow>().Single(r => r.Tag is Guid id && id == job.Id);
        try
        {
            foreach (var stage in new[] { MusicStage.Added, MusicStage.AlreadyExists })
            {
                job.Stage = stage;
                change.Invoke(form, [job]);
                Require(hold.Enabled && added.Enabled, "Completed record cannot be marked manually");
                hold.PerformClick();
                Require(job.Stage == MusicStage.OnHold && !job.IsRunning && row.DefaultCellStyle.BackColor.IsEmpty &&
                    row.DefaultCellStyle.SelectionBackColor.IsEmpty, "Holding a record did not clear its green colors");
                Require(new MusicJournal(path).Load().Single().Stage == MusicStage.OnHold, "Held state did not survive reopening the journal");
                added.PerformClick();
                Require(job.Stage == MusicStage.Added && row.DefaultCellStyle.BackColor == Color.FromArgb(229, 246, 233) &&
                    row.DefaultCellStyle.SelectionBackColor == Color.FromArgb(192, 230, 202), "Manual added state did not turn the record green");
                Require(new MusicJournal(path).Load().Single().Stage == MusicStage.Added, "Manual added state did not survive reopening the journal");
            }
            foreach (var stage in new[] { MusicStage.Capturing, MusicStage.Classifying, MusicStage.Captured,
                         MusicStage.Searching, MusicStage.Found, MusicStage.Adding })
            {
                job.Stage = stage;
                change.Invoke(form, [job]);
                Require(!hold.Enabled && !added.Enabled, "Running record exposes manual status controls");
                var events = job.Events.Count;
                setStatus.Invoke(form, [false]);
                setStatus.Invoke(form, [true]);
                Require(job.Stage == stage && job.Events.Count == events, "Manual status overwrote an active workflow");
            }
            job.Stage = MusicStage.NeedsReview;
            job.PlaylistName = "";
            job.SpotifyMatch = null;
            change.Invoke(form, [job]);
            added.PerformClick();
            Require(row.Cells[5].Value?.ToString() == "Eklendi olarak işaretlendi." && row.Cells[4].Value?.ToString() == "—",
                "Manually marking an unmatched song fabricated a Spotify match or an empty playlist label");
            Require(journal.Load().Single().Events.Last().Message == "Eklendi olarak işaretlendi.", "Manual status change was not logged");
            grid.ClearSelection();
            Require(!hold.Enabled && !added.Enabled, "Manual status controls remain enabled without a selection");
            Console.WriteLine("PASS: manual journal status colors, persistence and running-operation guards");
        }
        finally
        {
            File.Delete(path);
            job.Stage = MusicStage.Added;
            job.PlaylistName = "Yerli";
            job.SpotifyMatch = Track;
        }
    }

    private sealed class PreviewForm : Form1
    {
        protected override void OnLoad(EventArgs e) { } // Do not register hotkeys or touch user settings.
        protected override void OnShown(EventArgs e) { }
    }
    private sealed class FakeCapture : IMusicCapture
    {
        public Task<CapturedSong> CaptureAsync(CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(Song); }
    }
    private sealed class EmptyCapture : IMusicCapture
    {
        public Task<CapturedSong> CaptureAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Şu anda çalan bir şarkı bulunamadı.");
    }
    private sealed class FakeClassifier(SongCategory category) : ISongClassifier
    {
        public Task<SongClassification> ClassifyAsync(CapturedSong song, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(new SongClassification(category, "Test")); }
    }
    private sealed class FakeSpotify : ISpotifyLibrary
    {
        public int AddCalls;
        public int SearchCalls;
        public SongCategory LastCategory;
        public SpotifyTrack? Match = Track;
        public Exception? AddFailure;
        private bool _contains;
        public Task<SpotifyTrack?> FindAsync(CapturedSong song, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); SearchCalls++; return Task.FromResult(Match); }
        public Task<SpotifyPlaylist> GetDestinationAsync(SongCategory category, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); LastCategory = category; return Task.FromResult(new SpotifyPlaylist("list", category == SongCategory.Local ? "Yerli" : "Yabancı")); }
        public Task<bool> ContainsAsync(string playlistId, SpotifyTrack track, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(_contains); }
        public async Task AddAsync(string playlistId, SpotifyTrack track)
        { AddCalls++; await Task.Delay(10); if (AddFailure is not null) throw AddFailure; _contains = true; }
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
