namespace ScreenSelector;

internal sealed class MusicToastForm : Form
{
    private readonly Label _title = new();
    private readonly Label _song = new();
    private readonly Label _found = new();
    private readonly Label _added = new();
    private readonly Label _status = new();
    private readonly Button _stop = new();
    private readonly ToolTip _toolTip = new();
    private readonly System.Windows.Forms.Timer _closeTimer = new() { Interval = 10000 };
    private bool _finished;
    internal event Action? StopRequested;
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
        ClientSize = new Size(486, 218);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(26, 31, 47);
        Padding = new Padding(18);
        Text = "Şarkı işlemi";
        AddLabel(_title, new Rectangle(18, 16, 394, 26), 10, true);
        AddLabel(_song, new Rectangle(18, 44, 394, 54), 10, false);
        AddLabel(_found, new Rectangle(18, 104, 394, 24), 9, false);
        AddLabel(_added, new Rectangle(18, 134, 394, 24), 9, false);
        AddLabel(_status, new Rectangle(18, 169, 394, 38), 8, false);
        _status.ForeColor = Color.FromArgb(180, 189, 211);
        var dismiss = MakeButton("×", new Rectangle(430, 16, 38, 34));
        dismiss.AccessibleName = "Bu şarkının bildirimlerini kapat";
        _toolTip.SetToolTip(dismiss, "Bu şarkının bildirimlerini kapat. İşlem arka planda sürer.");
        dismiss.Click += (_, _) => Close();
        ConfigureButton(_stop, "■", new Rectangle(430, 62, 38, 34));
        _stop.AccessibleName = "Şarkı işlemini durdur";
        _toolTip.SetToolTip(_stop, "İşlemi durdur; şarkıyı kendim bulacağım.");
        _stop.Click += (_, _) => StopRequested?.Invoke();
        _closeTimer.Tick += (_, _) => Close();
    }

    private void AddLabel(Label label, Rectangle bounds, float size, bool bold)
    {
        label.Bounds = bounds;
        label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
        label.ForeColor = Color.White;
        label.AutoEllipsis = true;
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
        button.Font = new Font("Segoe UI", 13);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Color.FromArgb(44, 51, 71);
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
        button.TabStop = false;
        Controls.Add(button);
    }

    internal void UpdateJob(MusicJob job)
    {
        var emptyResult = job.Song is null && !job.IsRunning;
        _title.Text = job.Song is not null ? "1. Şarkı Yakalandı:" : job.IsRunning ? "Şarkı yakalanıyor…"
            : job.Stage == MusicStage.Cancelled ? "İşlem durduruldu" : "Şarkı bulunamadı";
        _song.Text = emptyResult ? job.Status : job.Song is null ? "Windows medya bilgisi / yedek ses tanıma" : job.Description;
        _toolTip.SetToolTip(_song, _song.Text);
        _found.Text = job.SpotifyMatch is null ? "2. Spotify'da Bulundu   ·   bekleniyor" : "2. Spotify'da Bulundu  ✓";
        _added.Text = job.Stage switch
        {
            MusicStage.Added => $"3. Listeye Eklendi  ✓  {job.PlaylistName}",
            MusicStage.AlreadyExists => $"3. Listede Zaten Var  ·  {job.PlaylistName}",
            MusicStage.Adding => "3. Listeye ekleniyor…",
            _ => "3. Listeye Eklendi   ·   bekleniyor"
        };
        _found.Visible = _added.Visible = job.Song is not null;
        _status.Text = emptyResult || job.Stage is MusicStage.Captured or MusicStage.Found ? "" : job.Status;
        _toolTip.SetToolTip(_status, job.Status);
        _stop.Enabled = job.CanCancel;
        _toolTip.SetToolTip(_stop, job.Stage == MusicStage.Adding
            ? "Ekleme isteği Spotify'a gönderildi; sonucu bekleniyor."
            : "İşlemi durdur; şarkıyı kendim bulacağım.");
        _found.ForeColor = job.SpotifyMatch is null ? Color.FromArgb(160, 169, 192) : Color.FromArgb(100, 225, 171);
        _added.ForeColor = job.Stage is MusicStage.Added or MusicStage.AlreadyExists
            ? Color.FromArgb(100, 225, 171) : Color.FromArgb(160, 169, 192);
        if (!job.IsRunning && !_finished)
        {
            _finished = true;
            _closeTimer.Interval = emptyResult ? 2500 : 10000;
            _closeTimer.Start();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(Color.FromArgb(91, 76, 230));
        e.Graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _closeTimer.Dispose(); _toolTip.Dispose(); }
        base.Dispose(disposing);
    }
}
