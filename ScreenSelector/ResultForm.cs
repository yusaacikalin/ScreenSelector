using System.Diagnostics;

namespace ScreenSelector;

public enum ResultKind { Text, Translation, Music }

public sealed record ResultData(ResultKind Kind, string Primary, string? Secondary = null,
    string? Subtitle = null, string? Link = null)
{
    public static ResultData ForText(string text) => new(ResultKind.Text, text);
    public static ResultData ForTranslation(string source, string translated, string sourceLanguage, string targetLanguage) =>
        new(ResultKind.Translation, source, translated, $"{sourceLanguage.ToUpperInvariant()} → {targetLanguage.ToUpperInvariant()}");
    public static ResultData ForMusic(MusicRecognitionResult music) =>
        new(ResultKind.Music, $"{music.Artist}\r\n{music.Title}", music.Album, "Şarkı eşleşmesi", music.Link);
}

public partial class ResultForm : Form
{
    private string? _link;

    public ResultForm()
    {
        InitializeComponent();
    }

    public ResultForm(Bitmap capture, ResultData data) : this()
    {
        pictureSelection.Image = new Bitmap(capture);
        _link = data.Link;
        txtPrimary.Text = data.Primary;
        txtSecondary.Text = data.Secondary ?? string.Empty;
        lblSubtitle.Text = data.Subtitle ?? "Seçiminiz başarıyla işlendi";

        switch (data.Kind)
        {
            case ResultKind.Text:
                lblTitle.Text = "Metin çıkarıldı";
                lblPrimary.Text = "Algılanan metin";
                HideSecondarySection();
                break;
            case ResultKind.Translation:
                lblTitle.Text = "Çeviri hazır";
                lblPrimary.Text = "Kaynak metin";
                lblSecondary.Text = "Çeviri";
                break;
            case ResultKind.Music:
                lblTitle.Text = "Şarkı bulundu";
                lblPrimary.Text = "Sanatçı ve parça";
                lblSecondary.Text = "Albüm";
                linkResult.Visible = !string.IsNullOrWhiteSpace(_link);
                pictureSelection.Visible = false;
                MoveContentUp();
                break;
        }
    }

    private void HideSecondarySection()
    {
        lblSecondary.Visible = false;
        txtSecondary.Visible = false;
        btnCopySecondary.Visible = false;
        linkResult.Visible = false;
        Height = 520;
    }

    private void MoveContentUp()
    {
        var offset = 165;
        foreach (var control in new Control[] { lblPrimary, txtPrimary, btnCopyPrimary, lblSecondary,
                     txtSecondary, btnCopySecondary, linkResult })
            control.Top -= offset;
        Height = 510;
    }

    private void btnCopyPrimary_Click(object? sender, EventArgs e) => CopyText(txtPrimary.Text, btnCopyPrimary);
    private void btnCopySecondary_Click(object? sender, EventArgs e) => CopyText(txtSecondary.Text, btnCopySecondary);

    private async void CopyText(string text, Button button)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var originalText = button.Text;
        var originalColor = button.BackColor;
        try
        {
            Clipboard.SetText(text);
            button.Text = "Kopyalandı ✓";
        }
        catch
        {
            button.Text = "Kopyalanamadı";
            button.BackColor = Color.FromArgb(255, 226, 230);
            lblSubtitle.Text = "Pano başka bir uygulama tarafından kullanılıyor. Biraz sonra tekrar deneyin.";
        }

        await Task.Delay(2500);
        if (button.IsDisposed) return;
        button.Text = originalText;
        button.BackColor = originalColor;
    }

    private void linkResult_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_link)) return;
        try
        {
            Process.Start(new ProcessStartInfo(_link) { UseShellExecute = true });
        }
        catch
        {
            linkResult.Text = "Bağlantı açılamadı";
            linkResult.LinkColor = Color.FromArgb(210, 65, 82);
            lblSubtitle.Text = "Sonuç bağlantısı varsayılan tarayıcıda açılamadı.";
        }
    }

    private void btnClose_Click(object? sender, EventArgs e) => Close();
}
