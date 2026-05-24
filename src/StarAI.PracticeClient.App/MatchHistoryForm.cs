using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

public sealed class MatchHistoryForm : Form
{
    private readonly string _starCraftRoot;
    private readonly MatchHistoryStore _store = new();
    private readonly DataGridView _sessions = new();
    private readonly DataGridView _replays = new();

    public MatchHistoryForm(string starCraftRoot)
    {
        _starCraftRoot = starCraftRoot;
        Text = "전적 / 리플레이";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1040, 650);
        MinimumSize = new Size(900, 520);
        BackColor = UiPalette.Background;
        ForeColor = UiPalette.Text;
        Font = new Font("Malgun Gothic", 10F);

        BuildUi();
        LoadData();
    }

    private void BuildUi()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Malgun Gothic", 10F),
            Padding = new Point(14, 5)
        };
        Controls.Add(tabs);

        var sessionsTab = new TabPage("스파링 기록") { BackColor = UiPalette.Background, ForeColor = UiPalette.Text };
        var replayTab = new TabPage("리플레이") { BackColor = UiPalette.Background, ForeColor = UiPalette.Text };
        tabs.TabPages.Add(sessionsTab);
        tabs.TabPages.Add(replayTab);

        ConfigureSessionGrid();
        sessionsTab.Controls.Add(_sessions);

        ConfigureReplayGrid();
        _replays.CellDoubleClick += (_, args) =>
        {
            if (args.RowIndex >= 0 && _replays.Rows[args.RowIndex].DataBoundItem is ReplayRow replay)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Path.GetDirectoryName(replay.FullPath)!,
                    UseShellExecute = true
                });
            }
        };
        replayTab.Controls.Add(_replays);
    }

    private void LoadData()
    {
        _sessions.DataSource = _store.Load(_starCraftRoot).Select(SessionRow.From).ToArray();
        _replays.DataSource = _store.GetReplays().Select(ReplayRow.From).ToArray();
    }

    private void ConfigureSessionGrid()
    {
        ConfigureGrid(_sessions);
        _sessions.Columns.Add(TextColumn(nameof(SessionRow.StartedAt), "시작", 145, DataGridViewAutoSizeColumnMode.None));
        _sessions.Columns.Add(TextColumn(nameof(SessionRow.BotName), "상대 봇", 145, DataGridViewAutoSizeColumnMode.None));
        _sessions.Columns.Add(TextColumn(nameof(SessionRow.BotRace), "상대 종족", 86, DataGridViewAutoSizeColumnMode.None));
        _sessions.Columns.Add(TextColumn(nameof(SessionRow.BotElo), "ELO", 70, DataGridViewAutoSizeColumnMode.None));
        _sessions.Columns.Add(TextColumn(nameof(SessionRow.MapName), "맵", 145, DataGridViewAutoSizeColumnMode.None));
        _sessions.Columns.Add(TextColumn(nameof(SessionRow.BuildName), "봇 빌드", 120, DataGridViewAutoSizeColumnMode.None));
        _sessions.Columns.Add(TextColumn(nameof(SessionRow.Result), "결과", 80, DataGridViewAutoSizeColumnMode.None));
        _sessions.Columns.Add(TextColumn(nameof(SessionRow.ReplayRoot), "리플레이 폴더", 260, DataGridViewAutoSizeColumnMode.Fill));
    }

    private void ConfigureReplayGrid()
    {
        ConfigureGrid(_replays);
        _replays.Columns.Add(TextColumn(nameof(ReplayRow.LastWriteTime), "저장 시각", 145, DataGridViewAutoSizeColumnMode.None));
        _replays.Columns.Add(TextColumn(nameof(ReplayRow.FileName), "파일", 330, DataGridViewAutoSizeColumnMode.Fill));
        _replays.Columns.Add(TextColumn(nameof(ReplayRow.LengthText), "크기", 90, DataGridViewAutoSizeColumnMode.None));
        _replays.Columns.Add(TextColumn(nameof(ReplayRow.FullPath), "경로", 360, DataGridViewAutoSizeColumnMode.Fill));
    }

    private static DataGridViewTextBoxColumn TextColumn(
        string propertyName,
        string header,
        int width,
        DataGridViewAutoSizeColumnMode mode)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = header,
            Width = width,
            AutoSizeMode = mode,
            MinimumWidth = 60,
            SortMode = DataGridViewColumnSortMode.Automatic
        };
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        var background = Color.FromArgb(10, 12, 10);
        var row = Color.FromArgb(18, 22, 18);
        var alt = Color.FromArgb(24, 29, 24);
        var selected = Color.FromArgb(35, 86, 55);
        var header = Color.FromArgb(28, 34, 28);
        var text = Color.FromArgb(220, 238, 214);
        var dim = Color.FromArgb(150, 185, 145);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoGenerateColumns = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.BackgroundColor = background;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Color.FromArgb(42, 52, 42);
        grid.RowHeadersVisible = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.EnableHeadersVisualStyles = false;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.RowTemplate.Height = 28;

        grid.ColumnHeadersDefaultCellStyle.BackColor = header;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = header;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Malgun Gothic", 9.5F, FontStyle.Bold);

        grid.DefaultCellStyle.BackColor = row;
        grid.DefaultCellStyle.ForeColor = text;
        grid.DefaultCellStyle.SelectionBackColor = selected;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Font = new Font("Malgun Gothic", 9.5F);
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);

        grid.AlternatingRowsDefaultCellStyle.BackColor = alt;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = dim;
    }

    private sealed record SessionRow(
        string StartedAt,
        string BotName,
        string BotRace,
        string BotElo,
        string MapName,
        string BuildName,
        string Result,
        string ReplayRoot)
    {
        public static SessionRow From(MatchRecord record) => new(
            record.StartedAt.ToString("yyyy-MM-dd HH:mm"),
            record.BotName,
            RaceKo(record.BotRace),
            record.BotElo?.ToString() ?? "-",
            record.MapName,
            record.BuildName,
            record.Result,
            record.ReplayRoot);
    }

    private sealed record ReplayRow(string LastWriteTime, string FileName, string LengthText, string FullPath)
    {
        public static ReplayRow From(ReplayRecord replay) => new(
            replay.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
            replay.FileName,
            FormatBytes(replay.Length),
            replay.FullPath);
    }

    private static string RaceKo(Race race) => race switch
    {
        Race.Terran => "테란",
        Race.Protoss => "프로토스",
        Race.Zerg => "저그",
        Race.Random => "랜덤",
        _ => race.ToString()
    };

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:0.0} KB";
        }

        return $"{bytes / 1024.0 / 1024.0:0.0} MB";
    }
}
