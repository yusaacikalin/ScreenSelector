using System.Diagnostics;

namespace ScreenSelector;

internal sealed class MusicToastForm : Form
{
    private readonly Label _title = new();
    private readonly Label _status = new();
    private readonly Button _stop = new();
    private readonly Button _local = new();
    private readonly Button _foreign = new();
    private readonly Button _search = new();
    private readonly ToolTip _toolTip = new();
    private readonly System.Windows.Forms.Timer _closeTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _fadeTimer = new() { Interval = 16 };
    private readonly Stopwatch _fadeElapsed = new();
    private bool _autoClosing;
    private bool _needsAttention;
    internal event Action? StopRequested;
    internal event Action? BackgroundRequested;
    internal event Action<SongCategory>? CategoryRequested;
    internal event Action? SearchRequested;
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= 0x08000000 | 0x00000080; // NOACTIVATE | TOOLWINDOW
            return parameters;
        }
    }

    internal MusicToastForm()
    {
        AutoScaleDimensions = new SizeF(7, 15);
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9F);
        // About 25% narrower and 80% shorter than the original 486 x 218 toast.
        ClientSize = new Size(365, 44);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(26, 31, 47);
        Padding = new Padding(10, 3, 7, 3);
        Text = "Şarkı işlemi";
        AddLabel(_title, new Rectangle(10, 3, 290, 18), 9, true);
        AddLabel(_status, new Rectangle(10, 22, 290, 17), 8, false);
        _status.ForeColor = Color.FromArgb(180, 189, 211);
        var dismiss = MakeButton("↘", new Rectangle(334, 9, 24, 26));
        dismiss.AccessibleName = "Arka planda sürdür";
        _toolTip.SetToolTip(dismiss, "Arka planda sürdür. Takıldığı aşama ve sonucu şarkı günlüğünde saklanır.");
        dismiss.Click += (_, _) => { BackgroundRequested?.Invoke(); Close(); };
        ConfigureButton(_stop, "■", new Rectangle(306, 9, 24, 26));
        _stop.AccessibleName = "Şarkı işlemini durdur";
        _toolTip.SetToolTip(_stop, "Akışı durdur ve o ana kadarki işlem geçmişini sakla.");
        _stop.Click += (_, _) => StopRequested?.Invoke();
        ConfigureAction(_local, "Yerli", new Rectangle(180, 22, 48, 18));
        ConfigureAction(_foreign, "Yabancı", new Rectangle(232, 22, 68, 18));
        ConfigureAction(_search, "Elle ara", new Rectangle(222, 9, 78, 26));
        _local.Click += (_, _) => CategoryRequested?.Invoke(SongCategory.Local);
        _foreign.Click += (_, _) => CategoryRequested?.Invoke(SongCategory.Foreign);
        _search.Click += (_, _) => SearchRequested?.Invoke();
        _toolTip.SetToolTip(_local, "Yerli olarak seç ve listeye eklemeyi sürdür.");
        _toolTip.SetToolTip(_foreign, "Yabancı olarak seç ve listeye eklemeyi sürdür.");
        _toolTip.SetToolTip(_search, "Şarkıyı Spotify'da elle ara. Otomatik ekleme beklemede kalır.");
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            _fadeElapsed.Restart();
            _fadeTimer.Start();
        };
        _fadeTimer.Tick += (_, _) =>
        {
            Opacity = Math.Max(0, 1 - _fadeElapsed.Elapsed.TotalMilliseconds / 300);
            if (Opacity <= 0) Close();
        };
    }

    private void AddLabel(Label label, Rectangle bounds, float size, bool bold)
    {
        label.Bounds = bounds;
        label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
        label.ForeColor = Color.White;
        label.AutoEllipsis = true;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.UseMnemonic = false;
        Controls.Add(label);
    }
    private Button MakeButton(string text, Rectangle bounds)
    {
        var button = new Button();
        ConfigureButton(button, text, bounds);
        return button;
    }
    private void ConfigureButton(Button button, string text, Rectangle bounds)
    {
        button.Bounds = bounds;
        button.Text = text;
        button.Font = new Font("Segoe UI", 10);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Color.FromArgb(44, 51, 71);
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
        button.TabStop = false;
        Controls.Add(button);
    }

    private void ConfigureAction(Button button, string text, Rectangle bounds)
    {
        ConfigureButton(button, text, bounds);
        button.Font = new Font("Segoe UI", 8);
        button.AccessibleName = text;
        button.Visible = false;
    }

    internal void UpdateJob(MusicJob job)
    {
        _title.Text = job.Song is { } song ? $"{song.Artist} - {song.Title}" : job.IsRunning ? "Şarkı yakalanıyor…"
            : job.Stage == MusicStage.Cancelled ? "İşlem durduruldu" : "Şarkı bulunamadı";
        _toolTip.SetToolTip(_title, _title.Text);
        // Only the current step is shown; later steps appear when the job reaches them.
        _status.Text = job.Stage switch
        {
            MusicStage.Capturing => "1/3 · Windows medya bilgisi / ses tanıma",
            MusicStage.Classifying => "1/3 · Şarkı yakalandı · Dil belirleniyor…",
            MusicStage.Captured => $"1/3 · Şarkı yakalandı ✓ · {job.CategoryLabel}",
            MusicStage.Searching => "2/3 · Spotify'da aranıyor…",
            MusicStage.Found => "2/3 · Spotify'da bulundu ✓",
            MusicStage.Adding => $"3/3 · Listeye ekleniyor… · {job.PlaylistName}",
            MusicStage.Added => $"3/3 · Listeye eklendi ✓ · {job.PlaylistName}",
            MusicStage.AlreadyExists => $"3/3 · Listede zaten var ✓ · {job.PlaylistName}",
            MusicStage.NeedsReview when job.Category == SongCategory.Unknown => "Dil belirsiz · Türü seçin",
            MusicStage.NeedsReview => "Güvenilir eşleşme yok",
            MusicStage.Cancelled => "İşlem durduruldu",
            MusicStage.Uncertain => "Ekleme sonucu belirsiz · Günlükten kontrol edin",
            MusicStage.Interrupted => "İşlem kesildi · Günlükten kontrol edin",
            MusicStage.OnHold => "Beklemeye alındı",
            _ => job.Status
        };
        if (job.StopRequested && job.IsRunning)
            _status.Text = job.Stage == MusicStage.Adding ? "Durdurma istendi · Ekleme sonucu bekleniyor…" : "Akış durduruluyor…";
        _toolTip.SetToolTip(_status, string.IsNullOrWhiteSpace(job.Status) ? _status.Text : job.Status);
        _status.AccessibleDescription = job.Status;
        var chooseCategory = job.Stage == MusicStage.NeedsReview && job.Category == SongCategory.Unknown;
        var searchManually = job.Stage == MusicStage.NeedsReview && job.Category != SongCategory.Unknown;
        _local.Visible = _foreign.Visible = chooseCategory;
        _search.Visible = searchManually;
        _title.Width = (int)Math.Round((searchManually ? 206 : 290) * DeviceDpi / 96F);
        _status.Width = (int)Math.Round((chooseCategory ? 164 : searchManually ? 206 : 290) * DeviceDpi / 96F);
        _needsAttention = job.Stage is MusicStage.NeedsReview or MusicStage.Failed or MusicStage.Uncertain or MusicStage.Interrupted;
        BackColor = _needsAttention ? Color.FromArgb(115, 30, 42) : Color.FromArgb(26, 31, 47);
        foreach (var button in Controls.OfType<Button>())
            button.BackColor = _needsAttention ? Color.FromArgb(153, 47, 61) : Color.FromArgb(44, 51, 71);
        _status.ForeColor = job.Stage switch
        {
            MusicStage.Captured or MusicStage.Found or MusicStage.Added or MusicStage.AlreadyExists => Color.FromArgb(100, 225, 171),
            MusicStage.NeedsReview or MusicStage.Uncertain or MusicStage.Interrupted or MusicStage.Failed => Color.White,
            _ => Color.FromArgb(180, 189, 211)
        };
        _stop.Enabled = job.CanStop;
        _toolTip.SetToolTip(_stop, job.Stage == MusicStage.Adding
            ? "Akışı durdur. Gönderilmiş ekleme isteğinin gerçek sonucu günlüğe kaydedilir."
            : "Akışı durdur ve o ana kadarki işlem geçmişini sakla.");
        var completed = job.Stage is MusicStage.Added or MusicStage.AlreadyExists or MusicStage.Cancelled ||
            (job.StopRequested && !job.IsRunning);
        if (completed && !_autoClosing)
        {
            _autoClosing = true;
            _closeTimer.Start();
        }
        else if (!completed)
        {
            _autoClosing = false;
            _closeTimer.Stop();
            _fadeTimer.Stop();
            _fadeElapsed.Reset();
            Opacity = 1;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(_needsAttention ? Color.FromArgb(233, 91, 109) : Color.FromArgb(91, 76, 230));
        e.Graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _closeTimer.Dispose(); _fadeTimer.Dispose(); _toolTip.Dispose(); }
        base.Dispose(disposing);
    }
}
