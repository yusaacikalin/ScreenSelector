namespace ScreenSelector;

public partial class Form1
{
    private void ConfigureMusicHistory()
    {
        var history = MusicCard("Şarkı günlüğü", 610);
        _journalStatus.SetBounds(28, 53, 625, 20);
        _journalStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _journalStatus.Font = new Font("Segoe UI", 8.5F);
        _journalStatus.ForeColor = Color.FromArgb(100, 110, 130);
        _journalStatus.AutoEllipsis = true;
        _journalStatus.Text = "Spotify'da elle aramak için bir şarkıya çift tıklayın.";
        history.Controls.Add(_journalStatus);

        var layout = new TableLayoutPanel
        {
            Bounds = new Rectangle(28, 84, 625, 502),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 2, RowCount = 2, Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 224));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        history.Controls.Add(layout);

        _musicGrid.Dock = DockStyle.Fill;
        _musicGrid.Margin = new Padding(0, 0, 18, 14);
        _musicGrid.ReadOnly = true;
        _musicGrid.AllowUserToAddRows = false;
        _musicGrid.AllowUserToDeleteRows = false;
        _musicGrid.AllowUserToResizeRows = false;
        _musicGrid.MultiSelect = false;
        _musicGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _musicGrid.RowHeadersVisible = false;
        _musicGrid.BackgroundColor = Color.White;
        _musicGrid.BorderStyle = BorderStyle.None;
        _musicGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _musicGrid.GridColor = Color.FromArgb(232, 235, 242);
        _musicGrid.EnableHeadersVisualStyles = false;
        _musicGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _musicGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 246, 251);
        _musicGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 81, 103);
        _musicGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F);
        _musicGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(244, 246, 251);
        _musicGrid.ColumnHeadersHeight = 36;
        _musicGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _musicGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _musicGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _musicGrid.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
        _musicGrid.DefaultCellStyle.Padding = new Padding(4, 8, 4, 8);
        _musicGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _musicGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(234, 231, 255);
        _musicGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(35, 42, 62);
        foreach (var (name, weight) in new[] { ("Saat", 55F), ("Sanatçı", 85F), ("Şarkı", 110F), ("Tür", 55F), ("Spotify", 65F), ("Liste / durum", 135F) })
        {
            var index = _musicGrid.Columns.Add(name, name);
            _musicGrid.Columns[index].FillWeight = weight;
            _musicGrid.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
        _musicGrid.SelectionChanged += (_, _) => UpdateMusicDetails();
        _musicGrid.CellMouseDoubleClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            // Use the clicked row, even if selection changes while the journal refreshes.
            if (_musicGrid.Rows[e.RowIndex].Tag is Guid id && _musicJobs.TryGetValue(id, out var job))
                SearchMusicJob(job);
        };
        toolTip.SetToolTip(_musicGrid, "Bir şarkıya çift tıklayın: Spotify'da elle ara.");
        layout.Controls.Add(_musicGrid, 0, 0);

        var details = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 18, 0) };
        var detailsTitle = MusicLabel(details, "İŞLEM AYRINTILARI", 0, 0, 250, 24);
        detailsTitle.Dock = DockStyle.Top;
        detailsTitle.Font = new Font("Segoe UI Semibold", 8);
        ConfigureMusicTextBox(_musicDetails, Rectangle.Empty);
        _musicDetails.Multiline = true;
        _musicDetails.ScrollBars = ScrollBars.Vertical;
        _musicDetails.ReadOnly = true;
        _musicDetails.Font = new Font("Segoe UI", 8.5F);
        _musicDetails.Dock = DockStyle.Fill;
        details.Controls.Add(_musicDetails);
        _musicDetails.BringToFront();
        layout.Controls.Add(details, 0, 1);

        var sidebar = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, Margin = Padding.Empty
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.Controls.Add(actions);
        layout.Controls.Add(sidebar, 1, 0);
        layout.SetRowSpan(sidebar, 2);

        var search = MusicActionGroup(actions, "ARAMA", "Satıra çift tıklayarak da\narama açabilirsiniz.");
        HistoryButton(search, _searchMusicRecord, "Elle ara", Color.White, Color.FromArgb(82, 68, 221),
            "Seçili şarkıyı Spotify masaüstü uygulamasında arar.", (_, _) => SearchMusicJob(SelectedMusicJob()));
        HistoryButton(search, _retryMusicRecord, "Yeniden dene", Color.FromArgb(40, 83, 155), Color.FromArgb(230, 238, 252),
            "Seçili şarkıyı yeniden arar ve uygun Spotify listesine eklemeyi dener.", async (_, _) =>
            {
                if (SelectedMusicJob() is { Song: not null } job && !job.IsRunning &&
                    job.Stage is not (MusicStage.Added or MusicStage.AlreadyExists)) await RunMusicJobAsync(job);
            });

        var category = MusicActionGroup(actions, "ŞARKI TÜRÜ", "Türü seçip eklemeyi yeniden dene.");
        HistoryButton(category, _chooseLocalMusic, "Yerli seç", Color.FromArgb(103, 66, 166), Color.FromArgb(240, 233, 252),
            "Şarkıyı Yerli olarak kaydeder ve hedef listeye eklemeyi yeniden dener.",
            async (_, _) => await ChooseSongCategoryAsync(SongCategory.Local));
        HistoryButton(category, _chooseForeignMusic, "Yabancı seç", Color.FromArgb(29, 105, 125), Color.FromArgb(225, 243, 247),
            "Şarkıyı Yabancı olarak kaydeder ve hedef listeye eklemeyi yeniden dener.",
            async (_, _) => await ChooseSongCategoryAsync(SongCategory.Foreign));

        var status = MusicActionGroup(actions, "KAYIT DURUMU", "Yalnızca günlüğü günceller.");
        HistoryButton(status, _holdMusicRecord, "Beklemeye al", Color.FromArgb(136, 88, 16), Color.FromArgb(255, 244, 215),
            "Seçili kaydı beklemeye alır ve yeşil işaretini kaldırır.", (_, _) => SetSelectedMusicStatus(false));
        HistoryButton(status, _markMusicAdded, "Eklendi işaretle", Color.FromArgb(28, 110, 66), Color.FromArgb(225, 244, 232),
            "Seçili kaydı yalnızca günlükte eklendi olarak işaretler. Spotify'a şarkı eklemez.", (_, _) => SetSelectedMusicStatus(true));

        var remove = MusicActionGroup(actions, "GEÇMİŞ", "Spotify listesindeki şarkı korunur.");
        HistoryButton(remove, _deleteMusicRecord, "Seçiliyi sil", Color.FromArgb(174, 47, 62), Color.FromArgb(253, 233, 236),
            "Seçili kaydı yalnızca yerel geçmişten siler. Spotify listesindeki şarkı korunur.", (_, _) => DeleteSelectedMusicRecord());

        UpdateMusicDetails();
        _musicHistoryForm = new MusicHistoryForm(history) { Owner = this, Icon = Icon };
    }

    private static FlowLayoutPanel MusicActionGroup(TableLayoutPanel parent, string title, string description)
    {
        var group = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            ColumnCount = 1, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.FromArgb(247, 248, 252), AccessibleName = title
        };
        group.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        group.Controls.Add(new Label
        {
            Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 8),
            ForeColor = Color.FromArgb(92, 102, 122), Margin = new Padding(0, 0, 0, 10)
        });
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty
        };
        group.Controls.Add(buttons);
        var hint = new Label
        {
            Text = description, AutoSize = true, Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 8), ForeColor = Color.FromArgb(101, 111, 131),
            Margin = new Padding(0, 4, 0, 0)
        };
        group.Controls.Add(hint);
        buttons.SizeChanged += (_, _) =>
        {
            foreach (Control button in buttons.Controls) button.Width = buttons.ClientSize.Width;
        };
        parent.Controls.Add(group);
        return buttons;
    }

    private void HistoryButton(Control parent, Button button, string text, Color foreground, Color background,
        string description, EventHandler handler)
    {
        MusicButton(parent, button, text, new Rectangle(0, 0, parent.ClientSize.Width, 36), handler);
        button.Margin = new Padding(0, 0, 0, 6);
        button.Padding = new Padding(38, 0, 8, 0);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.UseVisualStyleBackColor = false;
        button.ForeColor = foreground;
        button.BackColor = background;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(background, .15F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(background, .06F);
        button.AccessibleName = text;
        button.AccessibleDescription = description;
        toolTip.SetToolTip(button, description);
    }

    private bool SearchMusicJob(MusicJob? job)
    {
        if (job?.Song is not { } song) return false;
        var opened = OpenSpotifySearch(song);
        _musicWorkflow?.Record(job, opened
            ? "Kullanıcı Spotify'da elle aramayı açtı."
            : _journalStatus.Text);
        return opened;
    }
}

internal sealed class MusicHistoryButton(string glyph) : Button
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!Enabled)
        {
            e.Graphics.Clear(Color.FromArgb(237, 240, 245));
            var textBounds = new Rectangle(Padding.Left, 0, Math.Max(0, Width - Padding.Horizontal), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, Color.FromArgb(119, 129, 148),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
        var scale = DeviceDpi / 96F;
        var iconBounds = new Rectangle((int)(12 * scale), 0, (int)(20 * scale), Height);
        using var iconFont = new Font("Segoe MDL2 Assets", 12);
        TextRenderer.DrawText(e.Graphics, glyph, iconFont, iconBounds,
            Enabled ? ForeColor : SystemColors.GrayText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
