namespace ScreenSelector;

internal sealed class MusicHistoryForm : Form
{
    internal MusicHistoryForm(Panel history)
    {
        Text = "Şarkı günlüğü · ScreenSelector";
        AutoScaleDimensions = new SizeF(7, 15);
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9);
        Size = new Size(915, 1000);
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 246, 252);
        Padding = new Padding(12);
        history.Dock = DockStyle.Fill;
        Controls.Add(history);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Keep the live journal controls while the modeless window is closed by the user.
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }
}
