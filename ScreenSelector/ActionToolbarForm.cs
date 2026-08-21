namespace ScreenSelector;

public partial class ActionToolbarForm : Form
{
    private const int ToolbarOnlyHeight = 86;
    private const int ToastHeight = 170;
    private Bitmap _capture = new(1, 1);
    private AppSettings _settings = new();
    private bool _autoIdentifyMusic;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _busy;
    private bool _keepOpenForChildWindow;

    public ActionToolbarForm()
    {
        InitializeComponent();
    }

    public ActionToolbarForm(Bitmap capture, AppSettings settings, Rectangle selectedScreenArea, bool autoIdentifyMusic)
        : this()
    {
        _capture.Dispose();
        _capture = new Bitmap(capture);
        _settings = settings;
        _autoIdentifyMusic = autoIdentifyMusic;
        CollapseToast();
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
        if (_autoIdentifyMusic) await IdentifyMusicAsync();
    }

    private void SetBusy(bool busy, string status)
    {
        _busy = busy;
        btnExtractText.Enabled = !busy;
        btnTranslate.Enabled = !busy;
        btnMusic.Enabled = !busy;
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
        SetBusy(true, "Metin okunuyor ve çevriliyor…");
        try
        {
            var text = await OcrService.ExtractTextAsync(_capture, LanguageOption.GetOcrTag(_settings.SourceLanguage));
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Seçili alanda okunabilir bir metin bulunamadı.");
            var translated = await TranslationService.TranslateAsync(text, _settings.SourceLanguage,
                _settings.TargetLanguage, _cancellation.Token);
            ShowResult(ResultData.ForTranslation(text, translated.Text, translated.DetectedSourceLanguage,
                _settings.TargetLanguage));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ShowOperationError("Çeviri tamamlanamadı", ex.Message);
        }
        finally { if (!IsDisposed) SetBusy(false, "Bir işlem seçin"); }
    }

    private async void btnMusic_Click(object? sender, EventArgs e) => await IdentifyMusicAsync();

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
        if (e.KeyCode == Keys.Escape && !_busy) Close();
    }

    private void ActionToolbarForm_Deactivate(object? sender, EventArgs e)
    {
        if (_keepOpenForChildWindow || _busy || IsDisposed) return;
        BeginInvoke(Close);
    }
}
