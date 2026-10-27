using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using ScreenSelector;

internal static class MusicToastChecks
{
    private static readonly CapturedSong Song = new("Mustafa Sandal", "Geliversen", "Test");
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    internal static void Run()
    {
        CheckAdaptiveLayout();
        CheckLifetimeAndActions();
        CheckWorkflowControls();
        Console.WriteLine("PASS: toast fade timing, persistent red actions, category resume, background execution and stop history");
    }

    private static void CheckAdaptiveLayout()
    {
        using var toast = new MusicToastForm();
        var job = new MusicJob { Song = new CapturedSong("DYSTINCT", "YAMA", "Test"), Stage = MusicStage.NeedsReview };
        toast.UpdateJob(job);
        toast.Show();
        var buttons = toast.Controls.OfType<Button>().ToArray();
        var positions = buttons.Select(button => (Right: toast.ClientSize.Width - button.Right,
            Top: button.Top, Size: button.Size)).ToArray();
        var labels = toast.Controls.OfType<Label>().ToArray();
        var initialWidth = toast.Width;
        var initialHeight = toast.Height;
        var stop = Button(toast, "Şarkı işlemini durdur");
        Require(buttons.All(button => button.Top == stop.Top && button.Height == stop.Height),
            "Category actions are not aligned with the adjacent controls");
        SavePreview(toast, "music-toast-aligned-preview.png");

        job.Song = new CapturedSong("Uzun Sanatçı Adı ve Konuk Sanatçı", "Şarkının Uzun Adı (Özel Konser Kaydı)", "Test");
        toast.UpdateJob(job);
        Require(toast.Width > initialWidth && toast.Height == initialHeight,
            "A long song title did not expand the popup horizontally");
        Require(labels.All(label => label.Width >= label.GetPreferredSize(Size.Empty).Width),
            "Song text was truncated despite available screen space");
        Require(labels.All(label => buttons.Where(button => button.Visible)
            .All(button => !label.Bounds.IntersectsWith(button.Bounds))), "Song text overlaps an action");
        SavePreview(toast, "music-toast-long-title-preview.png");

        var titleWidth = toast.Width;
        job.Song = Song;
        job.Stage = MusicStage.Failed;
        job.Status = "Spotify bağlantısı kurulamadı. İnternet bağlantınızı ve hesap ayarlarını kontrol edip şarkıyı yeniden eklemeyi deneyin.";
        toast.UpdateJob(job);
        Require(toast.Width > titleWidth && labels.All(label => label.Width >= label.GetPreferredSize(Size.Empty).Width),
            "A long status did not receive enough text space");
        var expandedWidth = toast.Width;
        job.Stage = MusicStage.NeedsReview;
        foreach (var category in new[] { SongCategory.Unknown, SongCategory.Foreign, SongCategory.Unknown })
        {
            job.Category = category;
            toast.UpdateJob(job);
            Require(toast.Width == expandedWidth, "Shorter text or changing actions shrank the popup");
            Require(buttons.Select(button => (Right: toast.ClientSize.Width - button.Right,
                    Top: button.Top, Size: button.Size)).SequenceEqual(positions),
                "Text changes moved or resized buttons within the action group");
        }

        job.Song = new CapturedSong("Sanatçı", new string('W', 1000), "Test");
        toast.UpdateJob(job);
        Require(toast.Width <= Screen.FromControl(toast).WorkingArea.Width - 28 &&
            buttons.All(button => toast.ClientRectangle.Contains(button.Bounds)),
            "Oversized text pushed the popup or its actions beyond the screen");
        Console.WriteLine("PASS: aligned toast actions, adaptive title/status width and stable action spacing");
    }

    private static void CheckLifetimeAndActions()
    {
        using var categoryToast = new MusicToastForm();
        var categoryJob = new MusicJob { Song = Song, Stage = MusicStage.NeedsReview,
            Status = "Dil belirlenemedi. Yerli veya Yabancı seçin." };
        categoryToast.UpdateJob(categoryJob);
        categoryToast.Show();
        SavePreview(categoryToast, "music-toast-category-preview.png");
        var categories = new List<SongCategory>();
        categoryToast.CategoryRequested += categories.Add;
        Button(categoryToast, "Yerli").PerformClick();
        Button(categoryToast, "Yabancı").PerformClick();
        Require(categories.SequenceEqual(new[] { SongCategory.Local, SongCategory.Foreign }), "Category buttons did not report the chosen category");

        using var searchToast = new MusicToastForm();
        searchToast.UpdateJob(new MusicJob { Song = Song, Category = SongCategory.Foreign, Stage = MusicStage.NeedsReview });
        searchToast.Show();
        SavePreview(searchToast, "music-toast-search-preview.png");
        var searches = 0;
        searchToast.SearchRequested += () => searches++;
        Button(searchToast, "Elle ara").PerformClick();
        Require(searches == 1 && searchToast.Visible, "Search request was lost or closed before the host could confirm it opened");
        Require(!Button(searchToast, "Yerli").Visible && !Button(categoryToast, "Elle ara").Visible,
            "Toast offered actions from the wrong stage");

        using var success = new MusicToastForm();
        var elapsed = Stopwatch.StartNew();
        success.UpdateJob(new MusicJob { Song = Song, Category = SongCategory.Local, Stage = MusicStage.Added, PlaylistName = "Yerli" });
        success.Show();
        PumpFor(650);
        Require(!success.IsDisposed && success.Opacity == 1, "Completed toast faded before its one-second hold");
        PumpUntil(() => success.IsDisposed || success.Opacity < 1);
        Require(!success.IsDisposed && success.Opacity > 0 && elapsed.ElapsedMilliseconds >= 950,
            "Completed toast closed abruptly instead of fading after one second");
        PumpUntil(() => success.IsDisposed);
        PumpFor(350);
        Require(categoryToast.Visible && searchToast.Visible && categoryToast.Opacity == 1 && searchToast.Opacity == 1,
            "A review notification disappeared without user action");
        Require(categoryToast.BackColor.R > categoryToast.BackColor.G * 2 && searchToast.BackColor == categoryToast.BackColor,
            "Review notifications are not red");

        var title = categoryToast.Controls.OfType<Label>().Single(label => label.Font.Bold).Text;
        categoryJob.Category = SongCategory.Local;
        categoryJob.Stage = MusicStage.Added;
        categoryToast.UpdateJob(categoryJob);
        PumpUntil(() => categoryToast.Opacity < 1);
        categoryJob.Stage = MusicStage.Searching;
        categoryToast.UpdateJob(categoryJob);
        PumpFor(1400);
        Require(categoryToast.Visible && categoryToast.Opacity == 1 && categoryToast.BackColor.R < categoryToast.BackColor.B,
            "Resumed toast kept its old fade timer or warning color");
        Require(categoryToast.Controls.OfType<Label>().Single(label => label.Font.Bold).Text == title &&
            !Button(categoryToast, "Yerli").Visible && !Button(categoryToast, "Yabancı").Visible,
            "Resuming changed the song title or left obsolete category buttons");
    }

    private static void CheckWorkflowControls()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ScreenSelector-toast-" + Guid.NewGuid().ToString("N"));
        var journalPath = Path.Combine(directory, "history.jsonl");
        var categoryPath = Path.Combine(directory, "categories.json");
        using var form = new PreviewForm();
        var capture = new Capture();
        var library = new Library();
        var workflow = new MusicWorkflow(capture, new Classifier(), library) { StageDelay = TimeSpan.Zero };
        Field(form, "_musicWorkflow").SetValue(form, workflow);
        Field(form, "_songClassifier").SetValue(form, new SongLanguageService(categoryPath));
        Field(form, "_musicJournal").SetValue(form, new MusicJournal(journalPath));
        workflow.Changed += job => Invoke(form, "MusicJobChanged", job);
        var jobs = (Dictionary<Guid, MusicJob>)Field(form, "_musicJobs").GetValue(form)!;
        var toasts = (Dictionary<Guid, MusicToastForm>)Field(form, "_musicToasts").GetValue(form)!;
        var grid = (DataGridView)Field(form, "_musicGrid").GetValue(form)!;
        Task Start(MusicJob job)
        {
            jobs[job.Id] = job;
            return (Task)Invoke(form, "RunMusicJobAsync", job)!;
        }
        try
        {
            var local = new MusicJob();
            var foreign = new MusicJob();
            Await(Start(local));
            Await(Start(foreign));
            var localToast = toasts[local.Id];
            grid.ClearSelection();
            grid.Rows.Cast<DataGridViewRow>().Single(row => (Guid)row.Tag! == foreign.Id).Selected = true;
            Button(localToast, "Yerli").PerformClick();
            PumpUntil(() => !local.IsRunning);
            Require(local.Stage == MusicStage.Added && local.Category == SongCategory.Local && library.LastCategory == SongCategory.Local &&
                foreign.Category == SongCategory.Unknown && ReferenceEquals(toasts[local.Id], localToast),
                "Toast category selection resumed the selected history row or replaced its own notification");
            Require(local.Events.Any(e => e.Message.Contains("Kullanıcı Yerli seçti")), "Manual category choice was not logged");

            library.HasMatch = false;
            var foreignToast = toasts[foreign.Id];
            Button(foreignToast, "Yabancı").PerformClick();
            PumpUntil(() => !foreign.IsRunning);
            Require(foreign.Category == SongCategory.Foreign && foreign.Stage == MusicStage.NeedsReview &&
                Button(foreignToast, "Elle ara").Visible && library.AddCalls == 1,
                "Manual classification did not stop for an unreliable Spotify match");
            Button(foreignToast, "Şarkı işlemini durdur").PerformClick();
            Require(foreign.Stage == MusicStage.Cancelled && foreign.LastActiveStage == MusicStage.Searching &&
                foreign.Events.Any(e => e.Message.Contains("güvenilir eşleşme bulunamadı")), "Stopping a review lost its blocking stage or earlier log");

            capture.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var background = new MusicJob();
            var backgroundRun = Start(background);
            Button(toasts[background.Id], "Arka planda sürdür").PerformClick();
            capture.Pending.SetResult(Song);
            Await(backgroundRun);
            Require(background.Stage == MusicStage.NeedsReview && !toasts.ContainsKey(background.Id) &&
                background.Events.Any(e => e.Message.Contains("arka plana alındı")),
                "Backgrounding cancelled the workflow or reopened its notification when blocked");

            capture.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var failedCapture = new MusicJob();
            var failedRun = Start(failedCapture);
            Button(toasts[failedCapture.Id], "Arka planda sürdür").PerformClick();
            capture.Pending.SetException(new InvalidOperationException("Test yakalama hatası"));
            Await(failedRun);
            Require(failedCapture.Stage == MusicStage.Failed && failedCapture.Song is null &&
                failedCapture.Status.Contains("Şarkı yakalama"), "Background capture failure lost its exact stage");

            capture.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopped = new MusicJob();
            var stoppedRun = Start(stopped);
            Button(toasts[stopped.Id], "Şarkı işlemini durdur").PerformClick();
            Await(stoppedRun);
            Require(stopped.Stage == MusicStage.Cancelled && stopped.Song is null && stopped.LastActiveStage == MusicStage.Capturing,
                "Stop did not cancel an active capture");

            library.HasMatch = true;
            library.PendingAdd = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var committing = new MusicJob { Song = Song, Category = SongCategory.Local };
            var committingRun = Start(committing);
            PumpUntil(() => committing.Stage == MusicStage.Adding);
            Button(toasts[committing.Id], "Şarkı işlemini durdur").PerformClick();
            Require(committing.StopRequested && !committingRun.IsCompleted, "In-flight write was falsely treated as cancelled");
            library.PendingAdd.SetResult();
            Await(committingRun);
            Require(committing.Stage == MusicStage.Added && committing.Status.Contains("isteği gönderilmişti"),
                "In-flight stop did not record the actual Spotify result");

            var saved = new MusicJournal(journalPath).Load();
            foreach (var job in new[] { local, foreign, background, failedCapture, stopped, committing })
            {
                var restored = saved.Single(entry => entry.Id == job.Id);
                Require(restored.Stage == job.Stage && restored.LastActiveStage == job.LastActiveStage &&
                    restored.Events.Count == job.Events.Count, "Stage or event history did not survive reopening the journal");
            }
        }
        finally
        {
            foreach (var toast in toasts.Values.ToArray()) toast.Close();
            if (File.Exists(journalPath)) File.Delete(journalPath);
            if (File.Exists(categoryPath)) File.Delete(categoryPath);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }

    private static Button Button(Form toast, string name) => toast.Controls.OfType<Button>().Single(button => button.AccessibleName == name);
    private static FieldInfo Field(Form1 form, string name) => typeof(Form1).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static object? Invoke(Form1 form, string name, params object[] arguments) =>
        typeof(Form1).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, arguments);
    private static void Await(Task task) { PumpUntil(() => task.IsCompleted); task.GetAwaiter().GetResult(); }
    private static void PumpUntil(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.ElapsedMilliseconds > 5000) throw new Exception("Timed out waiting for toast/workflow transition");
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }
    private static void PumpFor(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        PumpUntil(() => watch.ElapsedMilliseconds >= milliseconds);
    }
    private static void SavePreview(Form form, string filename)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(Path.Combine(AppContext.BaseDirectory, filename), ImageFormat.Png);
    }
    private sealed class PreviewForm : Form1
    {
        protected override void OnLoad(EventArgs e) { }
        protected override void OnShown(EventArgs e) { }
    }
    private sealed class Capture : IMusicCapture
    {
        internal TaskCompletionSource<CapturedSong>? Pending;
        public Task<CapturedSong> CaptureAsync(CancellationToken cancellationToken) =>
            Pending?.Task.WaitAsync(cancellationToken) ?? Task.FromResult(Song);
    }
    private sealed class Classifier : ISongClassifier
    {
        public Task<SongClassification> ClassifyAsync(CapturedSong song, CancellationToken cancellationToken) =>
            Task.FromResult(new SongClassification(SongCategory.Unknown, "Test: dili belirsiz"));
    }
    private sealed class Library : ISpotifyLibrary
    {
        internal bool HasMatch = true;
        internal int AddCalls;
        internal SongCategory LastCategory;
        internal TaskCompletionSource? PendingAdd;
        public Task<SpotifyTrack?> FindAsync(CapturedSong song, CancellationToken cancellationToken) =>
            Task.FromResult<SpotifyTrack?>(HasMatch ? new SpotifyTrack("track", song.Title, [song.Artist], 200) : null);
        public Task<SpotifyPlaylist> GetDestinationAsync(SongCategory category, CancellationToken cancellationToken)
        {
            LastCategory = category;
            return Task.FromResult(new SpotifyPlaylist("list", category == SongCategory.Local ? "Yerli" : "Yabancı"));
        }
        public Task<bool> ContainsAsync(string playlistId, SpotifyTrack track, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task AddAsync(string playlistId, SpotifyTrack track, CancellationToken cancellationToken)
        { AddCalls++; return PendingAdd?.Task ?? Task.CompletedTask; }
    }
}
