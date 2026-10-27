using System.Diagnostics;

namespace ScreenSelector;

public partial class Form1
{
    private const int MusicHotkeyId = 0x5345;
    private bool _musicHotkeyRegistered;
    private readonly TextBox _musicShortcut = new();
    private readonly Label _musicShortcutStatus = new();
    private readonly Button _changeMusicShortcut = new();
    private readonly TextBox _spotifyClientId = new();
    private readonly Label _spotifyStatus = new();
    private readonly ComboBox _localPlaylist = new();
    private readonly ComboBox _foreignPlaylist = new();
    private readonly Button _connectSpotify = new();
    private readonly Button _refreshPlaylists = new();
    private readonly Button _disconnectSpotify = new();
    private readonly DataGridView _musicGrid = new();
    private readonly Button _deleteMusicRecord = new MusicHistoryButton("\uE74D");
    private readonly Button _holdMusicRecord = new MusicHistoryButton("\uE769");
    private readonly Button _markMusicAdded = new MusicHistoryButton("\uE73E");
    private readonly Button _searchMusicRecord = new MusicHistoryButton("\uE721");
    private readonly Button _retryMusicRecord = new MusicHistoryButton("\uE72C");
    private readonly Button _chooseLocalMusic = new MusicHistoryButton("\uE707");
    private readonly Button _chooseForeignMusic = new MusicHistoryButton("\uE774");
    private readonly TextBox _musicDetails = new();
    private readonly Label _journalStatus = new();
    private MusicHistoryForm? _musicHistoryForm;
    private readonly Dictionary<Guid, MusicJob> _musicJobs = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _musicOperations = new();
    private readonly Dictionary<Guid, MusicToastForm> _musicToasts = new();
    private readonly CancellationTokenSource _musicLifetime = new();
    private CancellationTokenSource? _spotifyLoginCancellation;
    private bool _refreshingPlaylists;
    private readonly MusicJournal _musicJournal = new(MusicJournal.DefaultPath);
    private SongLanguageService? _songClassifier;
    private SpotifyConnection? _spotifyConnection;
    private SpotifyLibraryService? _spotifyLibrary;
    private MusicWorkflow? _musicWorkflow;

    private void ConfigureMusicPage()
    {
        var shortcut = MusicCard("Şarkı yakalama kısayolu", 140);
        MusicLabel(shortcut, "Kısayola basın; şarkı yakalansın ve uygun Spotify listesine eklensin.", 28, 51, 625, 22);
        ConfigureMusicTextBox(_musicShortcut, new Rectangle(28, 82, 185, 29));
        _musicShortcut.ReadOnly = true;
        _musicShortcut.TextAlign = HorizontalAlignment.Center;
        shortcut.Controls.Add(_musicShortcut);
        MusicButton(shortcut, _changeMusicShortcut, "Kısayolu değiştir", new Rectangle(228, 78, 166, 36), (_, _) =>
        {
            EndShortcutCapture();
            _capturingShortcut = ShortcutCapture.Music;
            _musicShortcut.Text = "Yeni kısayola basın…";
            _musicShortcut.BackColor = Color.FromArgb(255, 250, 224);
            _musicShortcutStatus.Text = "Bir tuş veya tuş birleşimine basın. Esc: iptal";
            _changeMusicShortcut.Text = "Dinliyor…";
            Focus();
        });
        MusicButton(shortcut, new Button(), "Şimdi yakala", new Rectangle(474, 78, 177, 36), (_, _) => StartMusicCapture());
        _musicShortcutStatus.SetBounds(28, 117, 625, 20);
        _musicShortcutStatus.Font = new Font("Segoe UI", 8);
        shortcut.Controls.Add(_musicShortcutStatus);

        ConfigureMusicHistory();

        var historyLauncher = MusicCard("Şarkı günlüğü", 112);
        MusicLabel(historyLauncher, "Yakalanan şarkıları ve işlem geçmişini ayrı pencerede görüntüleyin.", 28, 55, 390, 42);
        MusicButton(historyLauncher, new Button(), "Şarkı günlüğünü aç", new Rectangle(443, 51, 210, 36), (_, _) => OpenMusicHistory());

        var spotify = MusicCard("Spotify bağlantısı ve hedef listeler", 300);
        MusicLabel(spotify, "Client ID", 28, 59, 120, 20);
        ConfigureMusicTextBox(_spotifyClientId, new Rectangle(28, 83, 315, 28));
        _spotifyClientId.PlaceholderText = "Spotify geliştirici uygulamasının Client ID değeri";
        _spotifyClientId.TextChanged += (_, _) =>
        {
            if (_loadingSettings) return;
            _settings.SpotifyClientId = _spotifyClientId.Text.Trim();
            _settings.SpotifyLocalPlaylistId = "";
            _settings.SpotifyForeignPlaylistId = "";
            _spotifyLibrary?.ResetAccount();
            _spotifyStatus.Text = "Client ID değişti. Spotify hesabınızı yeniden bağlayın.";
            SaveSettings();
        };
        spotify.Controls.Add(_spotifyClientId);
        MusicButton(spotify, _connectSpotify, "Spotify'a bağlan", new Rectangle(358, 79, 148, 36), async (_, _) => await ConnectSpotifyAsync());
        MusicButton(spotify, _disconnectSpotify, "Bağlantıyı kes", new Rectangle(515, 79, 138, 36), async (_, _) =>
        {
            if (_spotifyConnection is null) return;
            if (_musicOperations.Count > 0) { _spotifyStatus.Text = "Önce çalışan şarkı işlemlerinin bitmesini bekleyin veya durdurun."; return; }
            try
            {
                await _spotifyConnection.DisconnectAsync();
                _spotifyLibrary?.ResetAccount();
                _spotifyStatus.Text = "Spotify bağlantısı kesildi.";
            }
            catch (Exception ex) { _spotifyStatus.Text = ex.Message; }
        });
        _spotifyStatus.SetBounds(28, 123, 625, 39);
        _spotifyStatus.Font = new Font("Segoe UI", 8.5F);
        _spotifyStatus.Text = "Spotify hesabınızı bağlayın. Mevcut Yerli / Yabancı listeleri kullanılacak.";
        spotify.Controls.Add(_spotifyStatus);
        MusicLabel(spotify, "Yerli şarkılar", 28, 166, 295, 22);
        MusicLabel(spotify, "Yabancı şarkılar", 358, 166, 295, 22);
        foreach (var (combo, x) in new[] { (_localPlaylist, 28), (_foreignPlaylist, 358) })
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.SetBounds(x, 191, 295, 28);
            combo.DisplayMember = nameof(SpotifyPlaylist.Name);
            combo.MouseWheel += (_, e) =>
            {
                if (!combo.DroppedDown && e is HandledMouseEventArgs wheel)
                    wheel.Handled = true;
            };
            combo.SelectedIndexChanged += (_, _) => SavePlaylistSelection();
            spotify.Controls.Add(combo);
        }
        MusicButton(spotify, _refreshPlaylists, "Listeleri yenile", new Rectangle(28, 232, 160, 32), async (_, _) => await RefreshPlaylistsAsync());
        MusicButton(spotify, new Button(), "Dönüş adresini kopyala", new Rectangle(203, 232, 220, 32), (_, _) =>
        {
            try { Clipboard.SetText(SpotifyConnection.RedirectUri); _spotifyStatus.Text = "Dönüş adresi kopyalandı: " + SpotifyConnection.RedirectUri; }
            catch { _spotifyStatus.Text = "Pano kullanılamadı. Dönüş adresi: " + SpotifyConnection.RedirectUri; }
        });
        MusicLabel(spotify, "Dil için LRCLIB şarkı sözleri ve çeviri servisi kullanılır; belirsiz kayıtlar bekletilir.", 28, 274, 625, 20).Font = new Font("Segoe UI", 8);
        pageMusic.Controls.Add(shortcut);
        pageMusic.Controls.Add(historyLauncher);
        pageMusic.Controls.Add(spotify);
        pageMusic.Controls.SetChildIndex(shortcut, 0);
        pageMusic.Controls.SetChildIndex(historyLauncher, 1);
        pageMusic.Controls.SetChildIndex(spotify, 2);
        shortcut.TabIndex = 0;
        historyLauncher.TabIndex = 1;
        spotify.TabIndex = 2;
        cardMusic.TabIndex = 3;
        lblMusicDescription.Text = "Windows medya bilgisi alınamazsa AudD ile 8 saniyelik ses örneği tanınır.";
        lblMusicTitle.Text = "Yedek ses tanıma";
    }

    private static Panel MusicCard(string title, int height)
    {
        var card = new Panel { BackColor = Color.White, Size = new Size(682, height), Margin = new Padding(3, 3, 3, 14) };
        var label = new Label();
        StyleCardTitle(label, title, 28, 18);
        card.Controls.Add(label);
        return card;
    }
    private static Label MusicLabel(Control parent, string text, int x, int y, int width, int height)
    {
        var label = new Label { Text = text, Bounds = new Rectangle(x, y, width, height), Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(94, 103, 124) };
        parent.Controls.Add(label);
        return label;
    }
    private static void ConfigureMusicTextBox(TextBox textBox, Rectangle bounds)
    {
        textBox.Bounds = bounds;
        textBox.Font = new Font("Segoe UI", 10);
        textBox.BackColor = Color.FromArgb(247, 248, 252);
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }
    private static void MusicButton(Control parent, Button button, string text, Rectangle bounds, EventHandler handler)
    {
        button.Text = text;
        button.Bounds = bounds;
        button.BackColor = Color.FromArgb(238, 236, 255);
        button.ForeColor = Color.FromArgb(82, 68, 221);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI Semibold", 9);
        button.Cursor = Cursors.Hand;
        button.Click += handler;
        parent.Controls.Add(button);
    }

    private void LoadMusicSettings()
    {
        _spotifyClientId.Text = _settings.SpotifyClientId;
        ResetMusicShortcutDisplay();
        _spotifyConnection = new SpotifyConnection(() => _settings.SpotifyClientId);
        _spotifyLibrary = new SpotifyLibraryService(_spotifyConnection, () => _settings);
        _songClassifier = new SongLanguageService();
        _musicWorkflow = new MusicWorkflow(new CurrentSongService(() => _settings.AudDToken), _songClassifier, _spotifyLibrary);
        _musicWorkflow.Changed += MusicJobChanged;
        try
        {
            foreach (var job in _musicJournal.Load().Reverse())
            { _musicJobs[job.Id] = job; UpdateMusicRow(job); }
        }
        catch (Exception ex) { _journalStatus.Text = "Geçmiş günlük okunamadı: " + ex.Message; }
        if (_spotifyConnection.HasSavedConnection) _spotifyStatus.Text = "Kaydedilmiş Spotify bağlantısı var. Listeleri yenileyerek kontrol edin.";
    }

    private bool RegisterMusicHotkey()
    {
        if (IsHandleCreated) NativeMethods.UnregisterHotKey(Handle, MusicHotkeyId);
        _musicHotkeyRegistered = NativeMethods.RegisterHotKey(Handle, MusicHotkeyId,
            _settings.MusicHotkeyModifiers | HotkeyModifiers.NoRepeat, _settings.MusicHotkeyKey);
        _musicShortcutStatus.Text = _musicHotkeyRegistered ? "Kısayol arka planda dinleniyor. Bildirimler sessizdir."
            : "Bu kısayol kullanımda. Başka bir tuş birleşimi seçin.";
        UpdateHotkeyStatus();
        return _musicHotkeyRegistered;
    }

    private void ResetMusicShortcutDisplay()
    {
        _musicShortcut.Text = FormatShortcut(_settings.MusicHotkeyModifiers, _settings.MusicHotkeyKey);
        _musicShortcut.BackColor = Color.FromArgb(247, 248, 252);
        _changeMusicShortcut.Text = "Kısayolu değiştir";
        _musicShortcutStatus.Text = _musicHotkeyRegistered ? "Kısayol arka planda dinleniyor. Bildirimler sessizdir." : "Tek tuş veya tuş birleşimi atayabilirsiniz.";
    }

    private async void StartMusicCapture()
    {
        if (_musicWorkflow is null || _musicLifetime.IsCancellationRequested) return;
        if (_spotifyLoginCancellation is not null)
        { _journalStatus.Text = "Önce Spotify bağlantısını tamamlayın veya iptal edin."; return; }
        // Keep the toast stack within the screen while allowing independent jobs.
        if (_musicOperations.Count >= 3) { _journalStatus.Text = "Üç şarkı işlemi zaten sürüyor; birinin tamamlanmasını bekleyin."; return; }
        var job = new MusicJob();
        _musicJobs[job.Id] = job;
        await RunMusicJobAsync(job);
    }

    private async Task RunMusicJobAsync(MusicJob job)
    {
        if (_musicWorkflow is null || _musicOperations.ContainsKey(job.Id) || _musicLifetime.IsCancellationRequested) return;
        job.SpotifyMatch = null;
        job.PlaylistName = "";
        job.Stage = job.Song is null ? MusicStage.Capturing : MusicStage.Captured;
        job.LastActiveStage = job.Stage;
        job.StopRequested = false;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_musicLifetime.Token);
        _musicOperations.Add(job.Id, cancellation);
        SetSpotifyConfigurationEnabled(false);
        if (!_musicToasts.TryGetValue(job.Id, out var toast) || toast.IsDisposed)
        {
            toast = new MusicToastForm();
            toast.StopRequested += () => CancelMusic(job.Id);
            toast.BackgroundRequested += () => _musicWorkflow.Record(job,
                "Bildirim arka plana alındı. " + (job.IsRunning
                    ? "Akış tamamlanana veya kullanıcı müdahalesi gerekene kadar sürdürülecek."
                    : "Son durum günlükte korundu: " + job.Status));
            toast.CategoryRequested += async category => await ChooseMusicJobCategoryAsync(job, category);
            var currentToast = toast;
            toast.SearchRequested += () =>
            {
                if (!SearchMusicJob(job)) return;
                _musicWorkflow.Update(job, MusicStage.Added, "Elle eklendi");
                currentToast.Close();
            };
            toast.FormClosed += (_, _) => { _musicToasts.Remove(job.Id); ArrangeMusicToasts(); };
            _musicToasts[job.Id] = toast;
        }
        toast.UpdateJob(job);
        ArrangeMusicToasts();
        if (!toast.Visible) toast.Show();
        try { await _musicWorkflow.RunAsync(job, cancellation.Token); }
        finally
        {
            _musicOperations.Remove(job.Id);
            if (!IsDisposed && _musicOperations.Count == 0) SetSpotifyConfigurationEnabled(true);
        }
    }

    private void CancelMusic(Guid id)
    {
        if (!_musicJobs.TryGetValue(id, out var job) || !job.CanStop || _musicWorkflow is null) return;
        _musicWorkflow.RequestStop(job);
        if (_musicOperations.TryGetValue(id, out var cancellation)) cancellation.Cancel();
    }

    private void ArrangeMusicToasts()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        var top = area.Top + 14;
        foreach (var toast in _musicToasts.Values.Reverse().ToArray())
        {
            if (toast.IsDisposed) continue;
            if (top + toast.Height > area.Bottom) { toast.Hide(); continue; }
            toast.Location = new Point(area.Left + 14, top);
            if (toast.IsHandleCreated && !toast.Visible) toast.Show();
            top += toast.Height + 10;
        }
    }

    private void MusicJobChanged(MusicJob job)
    {
        // Do not add a transient capture placeholder, but retain every stopped or failed attempt.
        var keepInHistory = job.Song is not null || !job.IsRunning;
        if (keepInHistory)
        {
            try { _musicJournal.Append(job); }
            catch (Exception ex) { if (!IsDisposed) _journalStatus.Text = "Günlük dosyasına yazılamadı: " + ex.Message; }
        }
        if (IsDisposed || _musicLifetime.IsCancellationRequested) return;
        if (keepInHistory) UpdateMusicRow(job);
        if (job.Song is null && !job.IsRunning) _journalStatus.Text = job.Status;
        if (_musicToasts.TryGetValue(job.Id, out var toast) && !toast.IsDisposed) toast.UpdateJob(job);
    }

    private void OpenMusicHistory()
    {
        if (_musicHistoryForm is null || _musicHistoryForm.IsDisposed) return;
        EndShortcutCapture();
        if (_musicHistoryForm.WindowState == FormWindowState.Minimized)
            _musicHistoryForm.WindowState = FormWindowState.Normal;
        if (!_musicHistoryForm.Visible) _musicHistoryForm.Show(this);
        _musicHistoryForm.Activate();
    }

    private void UpdateMusicRow(MusicJob job)
    {
        if (job.Song is null && job.IsRunning) return;
        var row = _musicGrid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Tag is Guid id && id == job.Id);
        if (row is null)
        {
            _musicGrid.Rows.Insert(0, 1);
            row = _musicGrid.Rows[0];
            row.Tag = job.Id;
        }
        row.SetValues(job.StartedAt.ToString("dd.MM HH:mm"), job.Song?.Artist ?? "—", job.Song?.Title ?? "Şarkı belirlenemedi",
            job.CategoryLabel, job.SpotifyMatch is null ? (job.Stage == MusicStage.Searching ? "Aranıyor" : "—") : "Bulundu",
            job.Stage == MusicStage.Added && !string.IsNullOrWhiteSpace(job.PlaylistName)
                ? "Eklendi · " + job.PlaylistName : job.Status);
        var added = job.Stage is MusicStage.Added or MusicStage.AlreadyExists;
        row.DefaultCellStyle.BackColor = added ? Color.FromArgb(229, 246, 233) : Color.Empty;
        row.DefaultCellStyle.ForeColor = added ? Color.FromArgb(30, 96, 56) : Color.Empty;
        row.DefaultCellStyle.SelectionBackColor = added ? Color.FromArgb(192, 230, 202) : Color.Empty;
        row.DefaultCellStyle.SelectionForeColor = added ? Color.FromArgb(21, 77, 42) : Color.Empty;
        UpdateMusicDetails();
    }

    private MusicJob? SelectedMusicJob() => _musicGrid.SelectedRows.Count > 0 &&
        _musicGrid.SelectedRows[0].Tag is Guid id ? _musicJobs.GetValueOrDefault(id) : null;
    private void UpdateMusicDetails()
    {
        var job = SelectedMusicJob();
        _deleteMusicRecord.Enabled = job is not null && !job.IsRunning;
        _holdMusicRecord.Enabled = _markMusicAdded.Enabled = job?.Song is not null && !job.IsRunning;
        _searchMusicRecord.Enabled = job?.Song is not null;
        _retryMusicRecord.Enabled = _chooseLocalMusic.Enabled = _chooseForeignMusic.Enabled =
            job?.Song is not null && !job.IsRunning && job.Stage is not (MusicStage.Added or MusicStage.AlreadyExists);
        if (job is null) { _musicDetails.Clear(); return; }
        _musicDetails.Text = (job.Song is null ? "" : $"Kaynak: {job.Song.Source}\r\n") +
            string.Join("\r\n", job.Events.Select(e => $"{e.Time:HH:mm:ss} · {e.Message}"));
        // Updating the journal must not scroll the entire music page away from its hotkey control.
    }

    private void SetSelectedMusicStatus(bool added)
    {
        if (SelectedMusicJob() is not { Song: not null } job || job.IsRunning) return;
        job.Stage = added ? MusicStage.Added : MusicStage.OnHold;
        job.Status = added ? "Eklendi olarak işaretlendi." : "Beklemeye alındı.";
        job.Events.Add(new MusicLogEvent(DateTimeOffset.Now, job.Status));
        MusicJobChanged(job);
    }

    private void DeleteSelectedMusicRecord()
    {
        if (SelectedMusicJob() is not { } job) return;
        if (job.IsRunning)
        { _journalStatus.Text = "Çalışan işlem tamamlandıktan veya bildirimden durdurulduktan sonra kaydı silebilirsiniz."; return; }
        try { _musicJournal.Delete(job.Id); }
        catch (Exception ex) { _journalStatus.Text = "Kayıt yerel geçmişten silinemedi: " + ex.Message; return; }
        _musicJobs.Remove(job.Id);
        var row = _musicGrid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Tag is Guid id && id == job.Id);
        if (row is not null) _musicGrid.Rows.Remove(row);
        if (_musicToasts.Remove(job.Id, out var toast)) toast.Close();
        UpdateMusicDetails();
        _journalStatus.Text = "Kayıt yerel geçmişten silindi. Spotify listesindeki şarkı korundu.";
    }

    private Task ChooseSongCategoryAsync(SongCategory category) => ChooseMusicJobCategoryAsync(SelectedMusicJob(), category);

    private async Task ChooseMusicJobCategoryAsync(MusicJob? job, SongCategory category)
    {
        if (job?.Song is null || _songClassifier is null || _musicWorkflow is null || category == SongCategory.Unknown) return;
        if (job.IsRunning || _musicOperations.ContainsKey(job.Id) || job.Stage is MusicStage.Added or MusicStage.AlreadyExists)
        { _journalStatus.Text = "Tür seçimi, henüz eklenmemiş ve bekleyen kayıtlar için kullanılabilir."; return; }
        job.Category = category;
        job.CategoryReason = "Kullanıcının bu şarkı için seçimi.";
        try { _songClassifier.Remember(job.Song, category); }
        catch (Exception ex)
        {
            _journalStatus.Text = "Tür tercihi sonraki kullanımlar için kaydedilemedi: " + ex.Message;
            _musicWorkflow.Record(job, _journalStatus.Text);
        }
        _musicWorkflow.Record(job, $"Kullanıcı {job.CategoryLabel} seçti; ekleme akışı sürdürülüyor.");
        await RunMusicJobAsync(job);
    }

    private async Task ConnectSpotifyAsync()
    {
        if (_spotifyConnection is null) return;
        if (_spotifyLoginCancellation is not null) { _spotifyLoginCancellation.Cancel(); return; }
        if (_musicOperations.Count > 0) { _spotifyStatus.Text = "Hesabı değiştirmeden önce çalışan şarkı işlemlerini tamamlayın."; return; }
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_musicLifetime.Token);
        _spotifyLoginCancellation = cancellation;
        _connectSpotify.Text = "Girişi iptal et";
        _spotifyClientId.Enabled = false;
        _disconnectSpotify.Enabled = false;
        _refreshPlaylists.Enabled = false;
        try
        {
            _spotifyStatus.Text = "Spotify giriş ve izin ekranı bekleniyor…";
            await _spotifyConnection.ConnectAsync(uri => Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }), cancellation.Token);
            _settings.SpotifyLocalPlaylistId = "";
            _settings.SpotifyForeignPlaylistId = "";
            SaveSettings();
            _spotifyLibrary?.ResetAccount();
            await RefreshPlaylistsAsync();
        }
        catch (OperationCanceledException) { if (!IsDisposed) _spotifyStatus.Text = "Spotify girişi iptal edildi veya bekleme süresi doldu."; }
        catch (Exception ex) { if (!IsDisposed) _spotifyStatus.Text = ex.Message; }
        finally
        {
            _spotifyLoginCancellation = null;
            if (!IsDisposed)
            {
                _connectSpotify.Text = "Spotify'a bağlan";
                _spotifyClientId.Enabled = _disconnectSpotify.Enabled = _refreshPlaylists.Enabled = true;
            }
        }
    }

    private async Task RefreshPlaylistsAsync()
    {
        if (_spotifyLibrary is null || _refreshingPlaylists) return;
        _refreshingPlaylists = true;
        _refreshPlaylists.Enabled = false;
        var loaded = false;
        try
        {
            _spotifyStatus.Text = "Spotify hesabı ve mevcut listeler okunuyor…";
            var lists = await _spotifyLibrary.GetOwnedPlaylistsAsync(_musicLifetime.Token);
            FillPlaylistCombo(_localPlaylist, lists, _settings.SpotifyLocalPlaylistId, "Yerli");
            FillPlaylistCombo(_foreignPlaylist, lists, _settings.SpotifyForeignPlaylistId, "Yabancı");
            loaded = true;
            _spotifyStatus.Text = $"Bağlı: {_spotifyLibrary.AccountName}. " +
                (_localPlaylist.SelectedItem is SpotifyPlaylist && _foreignPlaylist.SelectedItem is SpotifyPlaylist
                    ? "Yerli / Yabancı hedefleri hazır." : "Eksik veya aynı isimli hedefler için aşağıdan liste seçin.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!IsDisposed) _spotifyStatus.Text = ex.Message; }
        finally
        {
            _refreshingPlaylists = false;
            if (!IsDisposed)
            {
                _refreshPlaylists.Enabled = _musicOperations.Count == 0;
                if (loaded) SavePlaylistSelection();
            }
        }
    }
    private static void FillPlaylistCombo(ComboBox combo, IReadOnlyList<SpotifyPlaylist> lists, string selectedId, string defaultName)
    {
        combo.DataSource = lists.ToArray();
        combo.SelectedIndex = -1;
        var selected = SpotifyLibraryService.ChooseDefaultPlaylist(lists, selectedId, defaultName);
        if (selected is not null) combo.SelectedItem = selected;
    }
    private void SavePlaylistSelection()
    {
        if (_loadingSettings || _refreshingPlaylists) return;
        if (_localPlaylist.SelectedItem is SpotifyPlaylist local && _foreignPlaylist.SelectedItem is SpotifyPlaylist foreign && local.Id == foreign.Id)
        { _spotifyStatus.Text = "Yerli ve Yabancı için farklı listeler seçin."; return; }
        _settings.SpotifyLocalPlaylistId = (_localPlaylist.SelectedItem as SpotifyPlaylist)?.Id ?? "";
        _settings.SpotifyForeignPlaylistId = (_foreignPlaylist.SelectedItem as SpotifyPlaylist)?.Id ?? "";
        SaveSettings();
    }
    private bool OpenSpotifySearch(CapturedSong song)
    {
        try
        {
            var uri = "spotify:search:" + Uri.EscapeDataString(song.Artist + " " + song.Title);
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            _journalStatus.Text = $"Spotify'da arama açıldı: {song.Artist} — {song.Title}";
            return true;
        }
        catch
        {
            _journalStatus.Text = "Spotify masaüstü uygulaması açılamadı. Spotify'ın bilgisayarda kurulu olduğunu kontrol edin.";
            return false;
        }
    }

    private void CloseMusicServices()
    {
        _musicLifetime.Cancel();
        _musicHistoryForm?.Dispose();
        foreach (var toast in _musicToasts.Values.ToArray()) toast.Close();
        if (IsHandleCreated) NativeMethods.UnregisterHotKey(Handle, MusicHotkeyId);
    }

    private void SetSpotifyConfigurationEnabled(bool enabled)
    {
        _spotifyClientId.Enabled = enabled && _spotifyLoginCancellation is null;
        _localPlaylist.Enabled = _foreignPlaylist.Enabled = enabled;
        _connectSpotify.Enabled = _disconnectSpotify.Enabled = enabled;
        _refreshPlaylists.Enabled = enabled && !_refreshingPlaylists;
    }
}
