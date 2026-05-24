using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

public sealed class HotkeyEditorForm : Form
{
    private readonly string _starCraftRoot;
    private readonly HotkeyCsvStore _store = new();
    private readonly BindingSource _source = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private IReadOnlyList<HotkeyEntry> _allEntries = Array.Empty<HotkeyEntry>();

    public HotkeyEditorForm(string starCraftRoot)
    {
        _starCraftRoot = starCraftRoot;
        Text = "핫키 편집";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(820, 620);
        BackColor = UiPalette.Background;
        ForeColor = UiPalette.Text;
        Font = new Font("Malgun Gothic", 10F);

        BuildUi();
        LoadEntries();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10),
            BackColor = UiPalette.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "SCHNAIL/1.16.1 핫키 CSV 편집",
            AutoSize = true,
            ForeColor = UiPalette.Text,
            Font = new Font("Malgun Gothic", 14F, FontStyle.Bold)
        });

        _search.Dock = DockStyle.Top;
        _search.PlaceholderText = "명령 이름 검색";
        _search.BackColor = Color.Black;
        _search.ForeColor = UiPalette.Text;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.TextChanged += (_, _) => ApplyFilter();
        root.Controls.Add(_search);

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.BackgroundColor = Color.Black;
        _grid.GridColor = Color.FromArgb(60, 60, 60);
        _grid.DataSource = _source;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HotkeyEntry.Hotkey), HeaderText = "키", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HotkeyEntry.Description), HeaderText = "명령", Width = 340, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(HotkeyEntry.CommandId), HeaderText = "ID", Width = 280, ReadOnly = true });
        root.Controls.Add(_grid);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        buttons.Controls.Add(MakeButton("닫기", (_, _) => Close()));
        buttons.Controls.Add(MakeButton("CSV 저장", (_, _) => SaveCsv()));
        buttons.Controls.Add(MakeButton("현재 SCHNAIL 핫키 가져오기", (_, _) => ImportPatch()));
        root.Controls.Add(buttons);
    }

    private void LoadEntries()
    {
        var workingCsv = Path.Combine(_starCraftRoot, "bwapi-data", "read", "sc_hotkeys.csv");
        _allEntries = _store.Load(File.Exists(workingCsv) ? workingCsv : null);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = _search.Text.Trim();
        _source.DataSource = _allEntries
            .Where(entry => string.IsNullOrEmpty(search) ||
                            entry.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            entry.CommandId.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private void SaveCsv()
    {
        _grid.EndEdit();
        var edited = _source.List.Cast<HotkeyEntry>().ToDictionary(entry => entry.CommandId, StringComparer.OrdinalIgnoreCase);
        _allEntries = _allEntries.Select(entry => edited.TryGetValue(entry.CommandId, out var current) ? current : entry).ToArray();
        _store.SaveWorkingCopy(_starCraftRoot, _allEntries);
        MessageBox.Show(
            this,
            "작업용 CSV를 저장했습니다. 실제 1.16.1 핫키는 SCHNAIL이 만든 patch_rt.mpq를 가져와 적용합니다.",
            "핫키 편집");
    }

    private void ImportPatch()
    {
        var result = HotkeyImporter.ImportBestAvailable(_starCraftRoot);
        LoadEntries();
        MessageBox.Show(this, result.Message, "핫키 가져오기");
    }

    private static Button MakeButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Width = 220,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Black,
            ForeColor = UiPalette.Text
        };
        button.FlatAppearance.BorderColor = UiPalette.Border;
        button.Click += onClick;
        return button;
    }
}
