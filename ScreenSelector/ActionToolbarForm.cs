namespace ScreenSelector;

public partial class ActionToolbarForm : Form
{
    private const int ToolbarOnlyHeight = 86;
    private const int ToastHeight = 170;
    private Bitmap _capture = new(1, 1);
    private AppSettings _settings = new();
    private Rectangle _selectedScreenArea;
    private Rectangle _captureScreenArea;
    private InlineTranslationSurface? _translationSurface;
    private GlobalMouseClickMonitor? _translationClickMonitor;
    private bool _translationCloseQueued;
    private bool _closing;
    private bool _resourcesDisposed;
    private bool _autoIdentifyMusic;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _busy;
    private bool _keepOpenForChildWindow;

    public ActionToolbarForm()
    {
        InitializeComponent();
        ModernWindowBehavior.EnableDragging(this, panelToolbar);
    }

    public ActionToolbarForm(Bitmap capture, AppSettings settings, Rectangle selectedScreenArea, bool autoIdentifyMusic,
        Point? selectionEnd = null, Rectangle? captureScreenArea = null)
        : this()
    {
        _capture.Dispose();
        _capture = new Bitmap(capture);
        _settings = settings;
        _selectedScreenArea = selectedScreenArea;
        _captureScreenArea = captureScreenArea ?? new Rectangle(
            selectedScreenArea.X + (selectedScreenArea.Width - capture.Width) / 2,
            selectedScreenArea.Y + (selectedScreenArea.Height - capture.Height) / 2,
            capture.Width, capture.Height);
        _autoIdentifyMusic = autoIdentifyMusic;
        CollapseToast();
        if (selectionEnd.HasValue)
            Location = new Point(selectionEnd.Value.X - Width / 2, selectionEnd.Value.Y);
        else
            PositionNearSelection(selectedScreenArea);
    }

    private void PositionNearSelection(Rectangle selectedArea)
    {
        var screen = Screen.FromRectangle(selectedArea).WorkingArea;
        var x = selectedArea.Left + (selectedArea.Width - Width) / 2;
        var y = selectedArea.Bottom + 12;
        if (y + Height > screen.Bottom) y = selectedArea.Top - Height - 12;
        x = Math.Clamp(x, screen.Left + 8, Math.Max(screen.Left + 8, screen.Right - Width - 8));
        y = Math.Clamp(y, screen.Top + 8, Math.Max(screen.Top + 8, screen.Bottom - Height - 8));
        Location = new Point(x, y);
    }

    private async void ActionToolbarForm_Shown(object? sender, EventArgs e)
    {
        if (_autoIdentifyMusic)
            await IdentifyMusicAsync();
        else
            await ExtractTextAndCopyInBackgroundAsync();
    }

    private async Task ExtractTextAndCopyInBackgroundAsync()
    {
        try
        {
            using var capture = new Bitmap(_capture);
            var languageTag = LanguageOption.GetOcrTag(_settings.SourceLanguage);
            var text = await Task.Run(() => OcrService.ExtractTextAsync(capture, languageTag));
            if (!_closing && !IsDisposed && !string.IsNullOrWhiteSpace(text))
                Clipboard.SetDataObject(ToSingleLine(text), true, 5, 100);
        }
        catch
        {
            // Varsayılan metin kopyalama işlemi arka planda ve sessiz çalışır.
        }
    }

    private static string ToSingleLine(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private void SetBusy(bool busy, string status)
    {
        _busy = busy;
        btnExtractText.Enabled = !busy;
        btnTranslate.Enabled = !busy;
        btnMusic.Enabled = !busy;
        btnRecord.Enabled = !busy && !_autoIdentifyMusic;
        progressBusy.Visible = busy;
        lblStatus.Visible = true;
        lblStatus.Text = status;
        if (busy) Text = status;
    }

    private async void btnExtractText_Click(object? sender, EventArgs e)
    {
        SetBusy(true, "Metin okunuyor…");
        try
        {
            var text = await OcrService.ExtractTextAsync(_capture, LanguageOption.GetOcrTag(_settings.SourceLanguage));
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Seçili alanda okunabilir bir metin bulunamadı.");
            ShowResult(ResultData.ForText(text));
        }
        catch (Exception ex)
        {
            ShowOperationError("Metin çıkarılamadı", ex.Message);
        }
        finally { if (!IsDisposed) SetBusy(false, "Bir işlem seçin"); }
    }

    private async void btnTranslate_Click(object? sender, EventArgs e)
    {
        if (_busy) return;
        SetBusy(true, "Metin okunuyor ve çevriliyor…");
        var cancellationToken = _cancellation.Token;
        try
        {
            _translationClickMonitor = new GlobalMouseClickMonitor(QueueTranslationClose);
            // The form may close during OCR. This operation owns its bitmap
            // until the worker finishes, independently of the form's lifetime.
            using var capture = new Bitmap(_capture);
            var languageTag = LanguageOption.GetOcrTag(_settings.SourceLanguage);
            var lines = await Task.Run(() => OcrService.ExtractLinesAsync(capture, languageTag, cancellationToken),
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (lines.Count == 0)
                throw new InvalidOperationException("Seçili alanda okunabilir bir metin bulunamadı.");
            var translated = await TranslationService.TranslateLinesAsync(lines, _settings.SourceLanguage,
                _settings.TargetLanguage, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!_closing && !IsDisposed) ShowInlineTranslation(translated);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ShowOperationError("Çeviri tamamlanamadı", ex.Message);
        }
        finally
        {
            if (_translationSurface == null)
            {
                _translationClickMonitor?.Dispose();
                _translationClickMonitor = null;
            }
            if (!_closing && !IsDisposed) SetBusy(false, "Bir işlem seçin");
        }
    }

    internal void ShowInlineTranslation(IReadOnlyList<TranslatedTextLine> lines)
    {
        var surface = new InlineTranslationSurface(_capture, lines);
        if (surface.Layouts.Count == 0)
        {
            surface.Dispose();
            Close();
            return;
        }
        try { _translationClickMonitor ??= new GlobalMouseClickMonitor(QueueTranslationClose); }
        catch
        {
            surface.Dispose();
            throw;
        }

        _translationSurface = surface;
        // Reuse this existing window. Its region consists only of translated
        // lines; the rest of the desktop remains live and visible.
        _keepOpenForChildWindow = true;
        toastTimer.Stop();
        Hide();
        panelToolbar.Visible = false;
        panelToast.Visible = false;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        Bounds = _captureScreenArea;
        Region = surface.VisibleRegion.Clone();
        Show();
        Invalidate();
    }

    private void QueueTranslationClose()
    {
        if (_translationCloseQueued || _closing || IsDisposed || !IsHandleCreated) return;
        _translationCloseQueued = true;
        try { BeginInvoke((Action)Close); }
        catch (InvalidOperationException) { }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (_translationSurface == null) base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_translationSurface != null)
            e.Graphics.DrawImageUnscaled(_translationSurface.Image, Point.Empty);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_translationSurface != null) QueueTranslationClose();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel) return;
        _closing = true;
        _cancellation.Cancel();
        _translationClickMonitor?.Dispose();
    }

    private async void btnMusic_Click(object? sender, EventArgs e) => await IdentifyMusicAsync();

    private async void btnRecord_Click(object? sender, EventArgs e)
    {
        if (_busy) return;

        SetBusy(true, "Ekran kaydı hazırlanıyor…");
        _keepOpenForChildWindow = true;
        Hide();
        NativeMethods.DwmFlush();

        string? outputPath = null;
        var saved = false;
        try
        {
            using (var countdown = new RecordingCountdownForm(_selectedScreenArea))
            {
                if (countdown.ShowDialog() != DialogResult.OK) return;
            }

            NativeMethods.DwmFlush();
            outputPath = ScreenRecordingService.CreateOutputPath();
            using var recorder = new ScreenRecordingService(_selectedScreenArea, outputPath);
            await recorder.StartAsync(_cancellation.Token);

            using (var controls = new RecordingControlForm(recorder.CaptureArea))
            {
                _ = recorder.Completion.ContinueWith(_ =>
                {
                    if (controls.IsDisposed || !controls.IsHandleCreated) return;
                    try { controls.BeginInvoke(controls.Close); }
                    catch (InvalidOperationException) { }
                }, TaskScheduler.Default);
                controls.ShowDialog();
            }

            await recorder.StopAsync();
            saved = true;
            try
            {
                CopyRecordingToClipboard(outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ekran kaydı kaydedildi ancak panoya kopyalanamadı.\n\n{ex.Message}\n\n{outputPath}",
                    "Panoya kopyalanamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show($"Ekran kaydı tamamlandı ve panoya kopyalandı. Ctrl+V ile yapıştırabilirsiniz.\n\n{outputPath}",
                "Kayıt tamamlandı",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            // Geri sayım veya uygulama kapanışı kaydı sessizce iptal eder.
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ekran videosu kaydedilemedi.\n\n{ex.Message}", "Kayıt hatası",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!saved && outputPath is not null)
            {
                try
                {
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
                catch
                {
                    // Başarısız kodlayıcının bıraktığı dosya kilitliyse asıl hatayı koru.
                }
            }
            Close();
        }
    }

    private static void CopyRecordingToClipboard(string outputPath)
    {
        var files = new System.Collections.Specialized.StringCollection { outputPath };
        var clipboardData = new DataObject();
        clipboardData.SetFileDropList(files);
        Clipboard.SetDataObject(clipboardData, true, 5, 100);
    }

    private async Task IdentifyMusicAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.AudDToken))
        {
            ShowToast("API anahtarı gerekli",
                "Şarkı tanıma için ana ekrandaki Şarkı tanıma bölümüne AudD API anahtarınızı girin.");
            return;
        }

        SetBusy(true, "Bilgisayar sesi 8 saniye dinleniyor…");
        try
        {
            var result = await MusicRecognitionService.IdentifyCurrentAudioAsync(_settings.AudDToken,
                TimeSpan.FromSeconds(8), _cancellation.Token);
            ShowResult(ResultData.ForMusic(result));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ShowOperationError("Şarkı bulunamadı", ex.Message);
        }
        finally { if (!IsDisposed) SetBusy(false, "Bir işlem seçin"); }
    }

    private void ShowResult(ResultData data)
    {
        _keepOpenForChildWindow = true;
        Hide();
        using var result = new ResultForm(_capture, data);
        result.ShowDialog();
        Close();
    }

    private void ShowOperationError(string title, string message) => ShowToast(title, message);

    private void ShowToast(string title, string message)
    {
        if (IsDisposed) return;
        lblToastTitle.Text = title;
        lblToastMessage.Text = message;
        panelToast.Visible = true;
        ClientSize = new Size(ClientSize.Width, ToastHeight);

        var workingArea = Screen.FromRectangle(Bounds).WorkingArea;
        if (Bottom > workingArea.Bottom - 8)
            Top = Math.Max(workingArea.Top + 8, workingArea.Bottom - Height - 8);

        toastTimer.Stop();
        toastTimer.Start();
    }

    private void CollapseToast()
    {
        toastTimer.Stop();
        if (_translationSurface != null) return;
        panelToast.Visible = false;
        ClientSize = new Size(ClientSize.Width, ToolbarOnlyHeight);
    }

    private void toastTimer_Tick(object? sender, EventArgs e) => CollapseToast();

    private void btnClose_Click(object? sender, EventArgs e)
    {
        _cancellation.Cancel();
        Close();
    }

    private void ActionToolbarForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && (!_busy || _translationClickMonitor != null)) Close();
    }

    private void ActionToolbarForm_Deactivate(object? sender, EventArgs e)
    {
        if (_keepOpenForChildWindow || _busy || _closing || IsDisposed) return;
        BeginInvoke(Close);
    }
}
