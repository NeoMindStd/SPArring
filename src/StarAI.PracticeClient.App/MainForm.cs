using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

public sealed class MainForm : Form
{
    private readonly PracticePaths _paths = PracticePaths.Defaults();
    private readonly PracticeClientSettingsStore _settingsStore = PracticeClientSettingsStore.Default();
    private readonly PracticeSessionHistoryStore _historyStore = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "StarAI.PracticeClient",
        "history.json"));
    private readonly Random _random = new();
    private PracticeClientSettings _settings = PracticeClientSettings.Defaults();
    private PracticeCatalog? _catalog;
    private ListBox _botList = null!;
    private ListBox _mapList = null!;
    private TextBox _searchBox = null!;
    private ComboBox _modeCombo = null!;
    private ComboBox _enemyRaceFilter = null!;
    private ComboBox _sortCombo = null!;
    private ComboBox _playerRaceCombo = null!;
    private ComboBox _buildCombo = null!;
    private Label _statusLabel = null!;
    private TextBox _detailsText = null!;
    private Label _difficultyLabel = null!;
    private Label _botListLabel = null!;
    private Button _launchButton = null!;
    private readonly HotkeyCsvStore _hotkeyStore = new();
    private readonly BindingSource _historySource = new();
    private DataGridView _historyGrid = null!;
    private ComboBox _hotkeyRaceFilter = null!;
    private ComboBox _hotkeyCategoryFilter = null!;
    private ListBox _hotkeyObjectList = null!;
    private FlowLayoutPanel _hotkeyCommandPanel = null!;
    private TextBox _hotkeySearch = null!;
    private TextBox _hotkeyKeyText = null!;
    private Label _hotkeyCommandTitle = null!;
    private Label _hotkeyCommandMeta = null!;
    private Label _hotkeyDefaultText = null!;
    private Label _hotkeyCountLabel = null!;
    private TextBox _replayRootText = null!;
    private TextBox _userMapRootText = null!;
    private readonly List<Button> _hotkeyRaceButtons = [];
    private readonly List<Button> _hotkeyCategoryButtons = [];
    private IReadOnlyList<HotkeyEntry> _hotkeyEntries = [];
    private HotkeyEntry? _selectedHotkeyEntry;
    private PracticeOverlayForm? _practiceOverlay;
    private GlobalInputActionHook? _inputActionHook;
    private StarCraftBorderlessKeeper? _borderlessKeeper;
    private StarCraftWindowMinimizeKeeper? _aiMinimizeKeeper;
    private System.Windows.Forms.Timer? _sessionHistoryTimer;
    private PracticeSessionRecord? _activeSessionRecord;
    private ActionRateCounter? _activeActionCounter;
    private DateTime _activeSessionStartedAtUtc;
    private bool _updatingSelections;

    public MainForm()
    {
        _settings = _settingsStore.Load();
        Text = "StarAI Practice Client";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 820);
        BackColor = Color.FromArgb(5, 7, 5);
        ForeColor = Color.FromArgb(128, 218, 93);

        BuildLayout();
        LoadCatalog();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "StarAI 연습 런처",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(166, 255, 126),
            Location = new Point(18, 18)
        };

        var subtitle = new Label
        {
            Text = "SCHNAIL 봇/맵을 읽어 로컬 1.16.1 + BWAPI 스파링을 준비합니다.",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 10),
            ForeColor = Color.FromArgb(128, 218, 93),
            Location = new Point(20, 58)
        };

        var tabs = new TabControl
        {
            Location = new Point(12, 92),
            Size = new Size(1128, 638),
            Appearance = TabAppearance.Normal
        };
        tabs.TabPages.Add(BuildPracticeTab());
        tabs.TabPages.Add(BuildSettingsTab());
        tabs.TabPages.Add(BuildHotkeyTab());
        tabs.TabPages.Add(BuildHistoryTab());

        _statusLabel = new Label
        {
            AutoSize = true,
            Location = new Point(18, 742),
            ForeColor = Color.FromArgb(128, 218, 93)
        };

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(tabs);
        Controls.Add(_statusLabel);
    }

    private TabPage BuildPracticeTab()
    {
        var page = CreateTabPage("게임");

        _modeCombo = CreateCombo(84, 22, 132);
        _modeCombo.DataSource = new[] { "스파링", "래더" };
        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            UpdateModeControls();
            ApplyBotFilters();
        };

        _enemyRaceFilter = CreateCombo(84, 66, 132);
        _enemyRaceFilter.DataSource = new[] { "모두", "테란", "저그", "프로토스", "랜덤" };
        _enemyRaceFilter.SelectedIndexChanged += (_, _) => ApplyBotFilters();

        _sortCombo = CreateCombo(84, 110, 132);
        _sortCombo.DataSource = new[] { "ELO 높은순", "ELO 낮은순", "이름순" };
        _sortCombo.SelectedIndexChanged += (_, _) => ApplyBotFilters();

        _searchBox = CreateTextBox(236, 66, 244, 28);
        _searchBox.PlaceholderText = "상대 이름 검색";
        _searchBox.TextChanged += (_, _) => ApplyBotFilters();

        _botList = CreateListBox(16, 156, 332, 216);
        _botList.SelectedIndexChanged += (_, _) => OnBotChanged();

        _mapList = CreateListBox(16, 424, 332, 156);
        _mapList.SelectedIndexChanged += (_, _) => UpdateDetails();

        _playerRaceCombo = CreateCombo(32, 218, 180);
        _playerRaceCombo.DataSource = new[]
        {
            StarCraftRace.Terran,
            StarCraftRace.Protoss,
            StarCraftRace.Zerg,
            StarCraftRace.Random
        };

        _playerRaceCombo.Location = new Point(780, 22);
        _playerRaceCombo.Size = new Size(132, 28);
        _playerRaceCombo.SelectedIndexChanged += (_, _) => UpdateDetails();

        _buildCombo = CreateCombo(780, 66, 240);
        _buildCombo.DataSource = new[] { "봇 기본 빌드 (자동)", "랜덤 빌드 (지원 봇만)" };
        _buildCombo.Enabled = false;

        var refreshButton = CreateButton("새로고침", 1014, 20, 88, 32);
        refreshButton.Click += (_, _) => LoadCatalog();

        _launchButton = CreateButton("스파링 시작", 922, 528, 180, 42);
        _launchButton.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        _launchButton.Click += async (_, _) => await LaunchCurrentPlanAsync();

        _difficultyLabel = new Label
        {
            AutoSize = false,
            Location = new Point(372, 124),
            Size = new Size(282, 74),
            ForeColor = Color.FromArgb(166, 255, 126),
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle
        };

        _detailsText = CreateTextBox(372, 214, 730, 296);
        _detailsText.Multiline = true;
        _detailsText.ReadOnly = true;
        _detailsText.ScrollBars = ScrollBars.Vertical;

        var runtimeText = CreateTextBox(672, 124, 430, 74);
        runtimeText.Multiline = true;
        runtimeText.ReadOnly = true;
        runtimeText.Text = string.Join(Environment.NewLine, new[]
        {
            $"사람: {_paths.PlayerRuntimeRoot}",
            $"AI: {_paths.AiRuntimeRoot}",
            "원본 SCHNAIL은 읽기 전용"
        });

        page.Controls.Add(CreateLabel("모드", 16, 26));
        page.Controls.Add(_modeCombo);
        page.Controls.Add(CreateLabel("상대 종족", 16, 70));
        page.Controls.Add(_enemyRaceFilter);
        page.Controls.Add(CreateLabel("정렬", 16, 114));
        page.Controls.Add(_sortCombo);
        page.Controls.Add(_searchBox);
        _botListLabel = CreateLabel("상대 선택", 16, 132);
        page.Controls.Add(_botListLabel);
        page.Controls.Add(_botList);
        page.Controls.Add(CreateLabel("맵 선택", 16, 400));
        page.Controls.Add(_mapList);
        page.Controls.Add(CreateLabel("내 종족", 724, 26));
        page.Controls.Add(_playerRaceCombo);
        page.Controls.Add(CreateLabel("빌드", 724, 70));
        page.Controls.Add(_buildCombo);
        page.Controls.Add(refreshButton);
        page.Controls.Add(_difficultyLabel);
        page.Controls.Add(runtimeText);
        page.Controls.Add(_detailsText);
        page.Controls.Add(_launchButton);
        UpdateModeControls();

        return page;
    }

    private TabPage BuildSettingsTab()
    {
        var page = CreateTabPage("설정");
        page.Controls.Add(CreateLabel("리플레이 저장 루트", 18, 28));
        _replayRootText = CreateTextBox(160, 24, 760, 28);
        _replayRootText.Text = _settings.ReplayRoot;
        var browseReplayButton = CreateButton("찾기", 934, 22, 72, 32);
        browseReplayButton.Click += (_, _) => BrowseFolderInto(_replayRootText);

        page.Controls.Add(CreateLabel("사용자 맵 폴더", 18, 78));
        _userMapRootText = CreateTextBox(160, 74, 760, 28);
        _userMapRootText.Text = _settings.UserMapRoot;
        var browseMapButton = CreateButton("찾기", 934, 72, 72, 32);
        browseMapButton.Click += (_, _) => BrowseFolderInto(_userMapRootText);

        var saveButton = CreateButton("설정 저장", 160, 126, 120, 34);
        saveButton.Click += (_, _) => SaveSettingsFromUi();

        page.Controls.Add(_replayRootText);
        page.Controls.Add(browseReplayButton);
        page.Controls.Add(_userMapRootText);
        page.Controls.Add(browseMapButton);
        page.Controls.Add(saveButton);
        page.Controls.Add(CreateReadOnlyBlock(
            18,
            188,
            1040,
            138,
            "사람 StarCraft는 W-MODE 기반 테두리 없는 전체 창모드로 실행합니다.\r\nAI 클라이언트는 창모드, 음소거, 커서 클립 OFF로 별도 실행합니다.\r\n사용자 맵은 .scm/.scx 파일을 읽어 StarAI 런타임 maps\\StarAI 폴더로 복사합니다."));
        return page;
    }

    private TabPage BuildHotkeyTab()
    {
        var page = CreateTabPage("Hotkeys");

        page.Controls.Add(CreateLabel("검색", 18, 26));
        _hotkeySearch = CreateTextBox(62, 22, 220, 28);
        _hotkeySearch.PlaceholderText = "예: probe, storm, 뮤탈";
        _hotkeySearch.TextChanged += (_, _) => RefreshHotkeyObjects();
        page.Controls.Add(_hotkeySearch);

        page.Controls.Add(CreateLabel("종족", 298, 26));
        _hotkeyRaceFilter = CreateCombo(-1000, -1000, 110);
        _hotkeyRaceFilter.Items.AddRange(["Terran", "Zerg", "Protoss", "Common", "전체"]);
        _hotkeyRaceFilter.SelectedIndex = 0;
        _hotkeyRaceFilter.SelectedIndexChanged += (_, _) =>
        {
            RefreshHotkeyFilterButtons();
            RefreshHotkeyObjects();
        };
        AddHotkeyFilterButtons(page, _hotkeyRaceFilter, _hotkeyRaceButtons, 342, 16, 74, 36);

        page.Controls.Add(CreateLabel("분류", 298, 68));
        _hotkeyCategoryFilter = CreateCombo(-1000, -1000, 128);
        _hotkeyCategoryFilter.Items.AddRange(["유닛", "건물", "일반", "연구", "업그레이드", "기술", "변태", "전체"]);
        _hotkeyCategoryFilter.SelectedIndex = 0;
        _hotkeyCategoryFilter.SelectedIndexChanged += (_, _) =>
        {
            RefreshHotkeyFilterButtons();
            RefreshHotkeyObjects();
        };
        AddHotkeyFilterButtons(page, _hotkeyCategoryFilter, _hotkeyCategoryButtons, 342, 58, 82, 30);

        var importButton = CreateButton("SCHNAIL 원본", 752, 18, 118, 32);
        importButton.Click += (_, _) => ImportHotkeys();
        var saveButton = CreateButton("CSV 저장", 880, 18, 96, 32);
        saveButton.Click += (_, _) => SaveHotkeys(applyMpq: false);
        var applyButton = CreateButton("런타임 반영", 986, 18, 118, 32);
        applyButton.Click += (_, _) => SaveHotkeys(applyMpq: true);
        page.Controls.Add(importButton);
        page.Controls.Add(saveButton);
        page.Controls.Add(applyButton);

        _hotkeyCountLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 96),
            Size = new Size(680, 20),
            ForeColor = Color.FromArgb(128, 218, 93),
            Text = "명령 0개"
        };
        page.Controls.Add(_hotkeyCountLabel);

        page.Controls.Add(CreateLabel("선택 항목", 18, 120));
        _hotkeyObjectList = CreateListBox(18, 142, 246, 428);
        _hotkeyObjectList.Font = new Font("Segoe UI", 10);
        _hotkeyObjectList.SelectedIndexChanged += (_, _) => RefreshHotkeyTiles();
        page.Controls.Add(_hotkeyObjectList);

        page.Controls.Add(CreateLabel("명령", 282, 120));
        _hotkeyCommandPanel = new FlowLayoutPanel
        {
            Location = new Point(282, 142),
            Size = new Size(426, 428),
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };
        page.Controls.Add(_hotkeyCommandPanel);

        _hotkeyCommandTitle = new Label
        {
            AutoSize = false,
            Location = new Point(730, 120),
            Size = new Size(366, 60),
            ForeColor = Color.FromArgb(166, 255, 126),
            Font = new Font(Font.FontFamily, 15, FontStyle.Bold)
        };
        _hotkeyCommandMeta = new Label
        {
            AutoSize = false,
            Location = new Point(730, 184),
            Size = new Size(366, 54),
            ForeColor = Color.FromArgb(128, 218, 93)
        };
        _hotkeyDefaultText = new Label
        {
            AutoSize = false,
            Location = new Point(730, 240),
            Size = new Size(366, 56),
            ForeColor = Color.FromArgb(128, 218, 93)
        };
        page.Controls.Add(_hotkeyCommandTitle);
        page.Controls.Add(_hotkeyCommandMeta);
        page.Controls.Add(_hotkeyDefaultText);

        page.Controls.Add(CreateLabel("현재 키", 730, 316));
        _hotkeyKeyText = CreateTextBox(794, 312, 84, 30);
        _hotkeyKeyText.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplySelectedHotkey(showStatus: true);
                e.SuppressKeyPress = true;
            }
        };
        var applyKeyButton = CreateButton("키 적용", 892, 308, 90, 36);
        applyKeyButton.Click += (_, _) => ApplySelectedHotkey(showStatus: true);
        page.Controls.Add(_hotkeyKeyText);
        page.Controls.Add(applyKeyButton);

        page.Controls.Add(CreateReadOnlyBlock(
            730,
            370,
            366,
            142,
            "타일의 왼쪽 큰 문자가 현재 핫키입니다.\r\nSCHNAIL 원본 버튼은 원본 CSV를 사람 런타임으로 복사합니다.\r\nCSV 저장은 작업 파일만 갱신합니다.\r\n런타임 반영은 사람 런타임 patch_rt.mpq에만 적용합니다.\r\n원본 SCHNAIL 설치 폴더는 수정하지 않습니다."));
        page.Controls.Add(CreateReadOnlyBlock(
            18,
            580,
            1078,
            38,
            "봇/AI 런타임에는 사람 핫키를 반영하지 않습니다. 사람 런타임만 커스텀 핫키를 사용합니다."));
        LoadHotkeys();
        return page;
    }

    private TabPage BuildHistoryTab()
    {
        var page = CreateTabPage("전적");
        var refreshButton = CreateButton("새로고침", 18, 18, 96, 32);
        refreshButton.Click += (_, _) => RefreshHistory();
        _historyGrid = new DataGridView
        {
            Location = new Point(18, 64),
            Size = new Size(1078, 500),
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            BackgroundColor = Color.Black,
            GridColor = Color.FromArgb(60, 60, 60),
            DataSource = _historySource,
            RowHeadersVisible = false
        };
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PracticeSessionRecord.StartedAtUtc),
            HeaderText = "시작",
            Width = 170
        });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PracticeSessionRecord.BotName),
            HeaderText = "봇",
            Width = 170
        });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PracticeSessionRecord.MapName),
            HeaderText = "맵",
            Width = 180
        });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PracticeSessionRecord.PlayerRace),
            HeaderText = "내 종족",
            Width = 90
        });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PracticeSessionRecord.BotRace),
            HeaderText = "상대",
            Width = 90
        });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PracticeSessionRecord.ActionsPerMinute),
            HeaderText = "APM",
            Width = 70
        });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PracticeSessionRecord.ActionCount),
            HeaderText = "액션",
            Width = 80
        });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PracticeSessionRecord.DurationSeconds),
            HeaderText = "초",
            Width = 70
        });

        page.Controls.Add(refreshButton);
        page.Controls.Add(_historyGrid);
        RefreshHistory();
        return page;
    }

    private static TabPage CreateTabPage(string title)
    {
        return new TabPage(title)
        {
            BackColor = Color.FromArgb(5, 7, 5),
            ForeColor = Color.FromArgb(128, 218, 93)
        };
    }

    private void LoadCatalog()
    {
        try
        {
            var schnailCatalog = SchnailCatalogReader.Read(_paths.SchnailRoot);
            var userMaps = UserMapCatalogReader.ReadDirectory(_settings.UserMapRoot);
            _catalog = UserMapCatalogReader.Merge(schnailCatalog, userMaps);
            _statusLabel.Text = $"SCHNAIL 카탈로그 로드 완료: 봇 {_catalog.Bots.Count}개, 맵 {_catalog.Maps.Count}개 (사용자 맵 {userMaps.Count}개)";
            ApplyBotFilters();
        }
        catch (Exception ex)
        {
            _catalog = null;
            _botList.DataSource = Array.Empty<BotItem>();
            _mapList.DataSource = Array.Empty<MapItem>();
            _detailsText.Text = ex.Message;
            _statusLabel.Text = "카탈로그 로드 실패";
        }
    }

    private void BrowseFolderInto(TextBox target)
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = Directory.Exists(target.Text) ? target.Text : string.Empty,
            UseDescriptionForTitle = true,
            Description = "폴더 선택"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private void SaveSettingsFromUi()
    {
        _settings = new PracticeClientSettings(
            ReplayRoot: string.IsNullOrWhiteSpace(_replayRootText.Text)
                ? PracticeRuntimeOptions.Defaults().ReplayRoot
                : _replayRootText.Text.Trim(),
            UserMapRoot: _userMapRootText.Text.Trim());
        _settingsStore.Save(_settings);
        LoadCatalog();
        MessageBox.Show(this, "설정을 저장했습니다.", "설정");
    }

    private PracticeRuntimeOptions CurrentRuntimeOptions()
    {
        return new PracticeRuntimeOptions(_settings.ReplayRoot);
    }

    private void RefreshHistory()
    {
        _historySource.DataSource = _historyStore.Load().Take(100).ToList();
    }

    private void LoadHotkeys()
    {
        var workingCsv = Path.Combine(_paths.PlayerRuntimeRoot, HotkeyCsvStore.RelativeWorkingCsvPath);
        var sourceCsv = File.Exists(workingCsv)
            ? workingCsv
            : Path.Combine(_paths.SchnailRoot, "res", "sc_hotkeys.csv");
        var messages = Path.Combine(_paths.SchnailRoot, "res", "messages_kr.properties");
        _hotkeyEntries = _hotkeyStore.Load(sourceCsv, messages);
        RefreshHotkeyObjects();
    }

    private void RefreshHotkeyObjects()
    {
        if (_hotkeyObjectList is null)
        {
            return;
        }

        var previous = (_hotkeyObjectList.SelectedItem as HotkeyObjectItem)?.Key;
        var objects = FilterHotkeyEntries()
            .GroupBy(entry => HotkeyObjectInfo.From(entry))
            .OrderBy(group => group.Key.CategoryRank)
            .ThenBy(group => group.Key.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new HotkeyObjectItem(group.Key, group.Count()))
            .ToList();

        _hotkeyObjectList.DataSource = null;
        _hotkeyObjectList.DataSource = objects;
        var selected = objects.FirstOrDefault(item => item.Key == previous) ?? objects.FirstOrDefault();
        if (selected is not null)
        {
            _hotkeyObjectList.SelectedItem = selected;
        }

        RefreshHotkeyTiles();
    }

    private void RefreshHotkeyTiles()
    {
        if (_hotkeyCommandPanel is null)
        {
            return;
        }

        var previousCommandId = _selectedHotkeyEntry?.CommandId;
        var selectedObject = (_hotkeyObjectList?.SelectedItem as HotkeyObjectItem)?.Info;
        var entries = FilterHotkeyEntries()
            .Where(entry => selectedObject is null || HotkeyObjectInfo.From(entry).Key == selectedObject.Key)
            .OrderBy(entry => HotkeyCommandRank(entry))
            .ThenBy(entry => entry.Description, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _hotkeyCommandPanel.SuspendLayout();
        _hotkeyCommandPanel.Controls.Clear();
        foreach (var entry in entries)
        {
            _hotkeyCommandPanel.Controls.Add(CreateHotkeyTile(entry));
        }

        _hotkeyCommandPanel.ResumeLayout();

        if (_hotkeyCountLabel is not null)
        {
            var objectCount = _hotkeyObjectList?.Items.Count ?? 0;
            _hotkeyCountLabel.Text = $"항목 {objectCount}개 / 명령 {entries.Count}개";
        }

        var selected = entries.FirstOrDefault(entry => entry.CommandId == previousCommandId) ?? entries.FirstOrDefault();
        SelectHotkey(selected);
    }

    private IEnumerable<HotkeyEntry> FilterHotkeyEntries()
    {
        var query = _hotkeySearch?.Text.Trim() ?? string.Empty;
        var race = SelectedComboText(_hotkeyRaceFilter, "전체");
        var category = SelectedComboText(_hotkeyCategoryFilter, "전체");

        return _hotkeyEntries
            .Where(entry => HotkeyMatchesQuery(entry, query))
            .Where(entry => race == "전체" || HotkeyRaceName(entry) == race)
            .Where(entry => category == "전체" || HotkeyDepthCategoryName(entry) == category);
    }

    private void AddHotkeyFilterButtons(
        Control parent,
        ComboBox source,
        List<Button> buttons,
        int x,
        int y,
        int buttonWidth,
        int buttonHeight)
    {
        buttons.Clear();
        for (var index = 0; index < source.Items.Count; index++)
        {
            var value = source.Items[index]?.ToString() ?? string.Empty;
            var button = CreateButton(value, x + index * (buttonWidth + 6), y, buttonWidth, buttonHeight);
            button.Tag = value;
            button.Click += (_, _) =>
            {
                source.SelectedItem = value;
                RefreshHotkeyFilterButtons();
                RefreshHotkeyObjects();
            };
            buttons.Add(button);
            parent.Controls.Add(button);
        }

        RefreshHotkeyFilterButtons();
    }

    private void RefreshHotkeyFilterButtons()
    {
        RefreshHotkeyFilterButtonGroup(_hotkeyRaceButtons, SelectedComboText(_hotkeyRaceFilter, "전체"));
        RefreshHotkeyFilterButtonGroup(_hotkeyCategoryButtons, SelectedComboText(_hotkeyCategoryFilter, "전체"));
    }

    private static void RefreshHotkeyFilterButtonGroup(IEnumerable<Button> buttons, string selectedValue)
    {
        foreach (var button in buttons)
        {
            var selected = string.Equals(button.Tag?.ToString(), selectedValue, StringComparison.OrdinalIgnoreCase);
            button.BackColor = selected ? Color.FromArgb(24, 42, 18) : Color.Black;
            button.ForeColor = selected ? Color.FromArgb(190, 255, 144) : Color.FromArgb(128, 218, 93);
            button.FlatAppearance.BorderColor = selected ? Color.FromArgb(220, 48, 48) : Color.FromArgb(96, 190, 82);
            button.FlatAppearance.BorderSize = selected ? 2 : 1;
        }
    }

    private static string SelectedComboText(ComboBox? combo, string fallback)
    {
        if (combo is null)
        {
            return fallback;
        }

        if (!string.IsNullOrWhiteSpace(combo.Text))
        {
            return combo.Text;
        }

        if (combo.SelectedItem is not null)
        {
            return combo.SelectedItem.ToString() ?? fallback;
        }

        return combo.SelectedIndex >= 0 && combo.SelectedIndex < combo.Items.Count
            ? combo.Items[combo.SelectedIndex]?.ToString() ?? fallback
            : fallback;
    }

    private Button CreateHotkeyTile(HotkeyEntry entry)
    {
        var key = string.IsNullOrWhiteSpace(entry.Hotkey) ? "-" : entry.Hotkey.ToUpperInvariant();
        var category = HotkeyCategoryName(entry);
        var race = HotkeyRaceName(entry);
        var tile = new Button
        {
            Tag = entry,
            Text = $"{key}    {entry.Description}\r\n{race} / {category}",
            TextAlign = ContentAlignment.MiddleLeft,
            Size = new Size(202, 58),
            Margin = new Padding(4),
            BackColor = Color.FromArgb(8, 18, 8),
            ForeColor = Color.FromArgb(166, 255, 126),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9)
        };
        tile.FlatAppearance.BorderColor = Color.FromArgb(96, 190, 82);
        tile.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 44, 18);
        tile.Click += (_, _) => SelectHotkey(entry);
        return tile;
    }

    private void SelectHotkey(HotkeyEntry? entry)
    {
        _selectedHotkeyEntry = entry;
        if (_hotkeyCommandTitle is null ||
            _hotkeyCommandMeta is null ||
            _hotkeyDefaultText is null ||
            _hotkeyKeyText is null)
        {
            return;
        }

        if (entry is null)
        {
            _hotkeyCommandTitle.Text = "명령을 선택하세요";
            _hotkeyCommandMeta.Text = string.Empty;
            _hotkeyDefaultText.Text = string.Empty;
            _hotkeyKeyText.Text = string.Empty;
            _hotkeyKeyText.Enabled = false;
            return;
        }

        _hotkeyKeyText.Enabled = true;
        _hotkeyCommandTitle.Text = entry.Description;
        _hotkeyCommandMeta.Text =
            $"{HotkeyRaceName(entry)} / {HotkeyCategoryName(entry)}\r\nID: {entry.CommandId}\r\nTBL: {entry.StringId}";
        _hotkeyDefaultText.Text = $"원본: {StripStarCraftMarkup(entry.DefaultText)}";
        _hotkeyKeyText.Text = entry.Hotkey;
    }

    private void ImportHotkeys()
    {
        try
        {
            _hotkeyStore.ImportFromSchnail(_paths, _paths.PlayerRuntimeRoot);
            LoadHotkeys();
            MessageBox.Show(this, "SCHNAIL 핫키 CSV를 작업 런타임으로 가져왔습니다.", "핫키");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "핫키 가져오기 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveHotkeys(bool applyMpq)
    {
        try
        {
            ApplySelectedHotkey(showStatus: false);
            var result = new HotkeyPatchApplier().SaveAndApply(_paths, _paths.PlayerRuntimeRoot, _hotkeyEntries, applyMpq);
            MessageBox.Show(this, result.Message, "핫키");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "핫키 저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplySelectedHotkey(bool showStatus)
    {
        if (_selectedHotkeyEntry is null || _hotkeyKeyText is null)
        {
            return;
        }

        _selectedHotkeyEntry.Hotkey = _hotkeyKeyText.Text.Trim();
        var commandId = _selectedHotkeyEntry.CommandId;
        RefreshHotkeyTiles();
        SelectHotkey(_hotkeyEntries.FirstOrDefault(entry => entry.CommandId == commandId));

        if (showStatus)
        {
            _statusLabel.Text = $"핫키 변경 대기: {_selectedHotkeyEntry.Description} -> {_selectedHotkeyEntry.Hotkey}";
        }
    }

    private void ApplyBotFilters()
    {
        if (_catalog is null || _updatingSelections)
        {
            return;
        }

        _updatingSelections = true;
        try
        {
            var query = _searchBox.Text.Trim();
            var raceFilter = SelectedEnemyRace();
            var bots = _catalog.Bots.AsEnumerable();

            if (!IsLadderMode && !string.IsNullOrWhiteSpace(query))
            {
                bots = bots.Where(bot => bot.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            bots = raceFilter switch
            {
                StarCraftRace.Terran => bots.Where(bot => bot.Race == StarCraftRace.Terran),
                StarCraftRace.Zerg => bots.Where(bot => bot.Race == StarCraftRace.Zerg),
                StarCraftRace.Protoss => bots.Where(bot => bot.Race == StarCraftRace.Protoss),
                StarCraftRace.Random => bots.Where(bot => bot.Race == StarCraftRace.Random),
                _ => bots
            };

            bots = IsLadderMode
                ? bots.OrderByDescending(bot => bot.Elo ?? int.MinValue).ThenBy(bot => bot.Name)
                : (_sortCombo.SelectedItem?.ToString() ?? "ELO 높은순") switch
            {
                "ELO 낮은순" => bots.OrderBy(bot => bot.Elo ?? int.MaxValue).ThenBy(bot => bot.Name),
                "이름순" => bots.OrderBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase),
                _ => bots.OrderByDescending(bot => bot.Elo ?? int.MinValue).ThenBy(bot => bot.Name)
            };

            _botList.DataSource = bots.Select(bot => new BotItem(bot)).ToList();
        }
        finally
        {
            _updatingSelections = false;
        }

        if (IsLadderMode)
        {
            ApplyLadderMapFilters();
        }
        else
        {
            OnBotChanged();
        }
    }

    private void OnBotChanged()
    {
        if (IsLadderMode)
        {
            UpdateDetails();
            return;
        }

        if (_catalog is null || _botList.SelectedItem is not BotItem botItem || _updatingSelections)
        {
            return;
        }

        _updatingSelections = true;
        try
        {
            var maps = PracticeCatalogCompatibility.MapsForBot(_catalog, botItem.Bot.Id)
                .Select(map => new MapItem(map))
                .ToList();
            _mapList.DataSource = maps;
        }
        finally
        {
            _updatingSelections = false;
        }

        UpdateDetails();
    }

    private void UpdateDetails()
    {
        if (_catalog is null)
        {
            return;
        }

        var mapItem = _mapList.SelectedItem as MapItem;
        if (IsLadderMode)
        {
            UpdateLadderDetails(mapItem?.Map);
            return;
        }

        if (_botList.SelectedItem is not BotItem botItem)
        {
            return;
        }

        var bot = botItem.Bot;
        var map = mapItem?.Map;
        var compatibleMapCount = PracticeCatalogCompatibility.MapsForBot(_catalog, bot.Id).Count;
        var difficulty = LadderDifficultyEstimator.EstimateFromSchnailElo(bot.Elo);
        _difficultyLabel.Text = difficulty is null
            ? "난이도\r\nSCR 환산 미확인"
            : $"난이도\r\n{difficulty.Label}";

        _detailsText.Text = string.Join(Environment.NewLine, new[]
        {
            $"봇: {bot.Name}",
            $"종족: {bot.Race}",
            $"ELO: {bot.Elo?.ToString() ?? "알 수 없음"}",
            $"실행 형식: {bot.ExecutableKind} / {bot.ExecutableName}",
            $"BWAPI: {bot.BwapiVersion}",
            $"SCR 래더 참고 환산: {difficulty?.Label ?? "미확인"}",
            $"환산 주의: {difficulty?.Disclaimer ?? "ELO 정보가 없어 환산하지 않았습니다."}",
            $"호환 맵: {compatibleMapCount}개",
            $"선택 맵: {(map is null ? "없음" : $"{map.Name} ({map.FileName})")}",
            $"맵 종류: {(map?.IsUserMap == true ? "사용자 추가 맵 (호환성 제약 미확인)" : "SCHNAIL 맵")}",
            "빌드: 봇 기본 빌드 (공통 BWAPI 빌드 선택 규격이 없어 아직 봇별 강제 선택은 보류)",
            $"봇 원본 폴더: {bot.SourceDirectory ?? "미확인"}",
            $"맵 원본 파일: {map?.SourcePath ?? "미확인"}",
            "",
            bot.Description ?? ""
        });
    }

    private void ApplyLadderMapFilters()
    {
        if (_catalog is null || _updatingSelections)
        {
            return;
        }

        _updatingSelections = true;
        try
        {
            var enemyRace = SelectedEnemyRace();
            var maps = _catalog.Maps
                .Where(map => map.Enabled)
                .Where(map => LadderBotSelector.CandidatesForMap(_catalog, map.Id, enemyRace).Count > 0)
                .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
                .Select(map => new MapItem(map))
                .ToList();
            _mapList.DataSource = maps;
        }
        finally
        {
            _updatingSelections = false;
        }

        UpdateDetails();
    }

    private void UpdateLadderDetails(PracticeMap? map)
    {
        if (_catalog is null)
        {
            return;
        }

        var enemyRace = SelectedEnemyRace();
        IReadOnlyList<PracticeBot> candidates = map is null
            ? Array.Empty<PracticeBot>()
            : LadderBotSelector.CandidatesForMap(_catalog, map.Id, enemyRace);
        _difficultyLabel.Text = candidates.Count == 0
            ? "래더\r\n후보 없음"
            : $"래더\r\n후보 {candidates.Count}개";

        var difficultyRange = FormatDifficultyRange(candidates);
        var preview = candidates.Take(12).Select(bot =>
        {
            var difficulty = LadderDifficultyEstimator.EstimateFromSchnailElo(bot.Elo);
            return $"- {bot.Name} / {bot.Race} / {difficulty?.Label ?? $"ELO {bot.Elo?.ToString() ?? "?"}"}";
        });

        _detailsText.Text = string.Join(Environment.NewLine, new[]
        {
            "모드: 래더",
            $"내 종족: {(_playerRaceCombo.SelectedItem is StarCraftRace race ? race : StarCraftRace.Terran)}",
            $"상대 종족: {RaceFilterLabel(enemyRace)}",
            $"선택 맵: {(map is null ? "없음" : $"{map.Name} ({map.FileName})")}",
            $"랜덤 후보: {candidates.Count}개",
            $"난이도 범위: {difficultyRange}",
            "빌드: 선택된 봇의 기본 빌드 (봇별 빌드 선택 API 조사/구현 전)",
            $"맵 원본 파일: {map?.SourcePath ?? "미확인"}",
            "",
            "후보 미리보기",
            string.Join(Environment.NewLine, preview)
        });
    }

    private async Task LaunchCurrentPlanAsync()
    {
        SetLaunchButtonsEnabled(false);
        try
        {
            var plan = PrepareCurrentPlan();
            var runtimeOptions = CurrentRuntimeOptions();
            PracticeRuntimeConfigurator.Apply(plan, runtimeOptions);
            _statusLabel.Text = $"{CurrentModeLabel()} 시작 중: StarCraft 사람 클라이언트와 AI 클라이언트를 실행합니다.";
            var screenBounds = Screen.FromControl(this).Bounds;
            var cncDdrawHandlesPlayerDisplay = plan.Player.CncDdrawMode == CncDdrawMode.BorderlessFullscreen;
            var sessionStartedAt = DateTime.UtcNow;
            var actionCounter = new ActionRateCounter();
            DisposePracticeOverlay();
            var existingStarCraftProcesses = StarCraftBorderlessWindow.CurrentStarCraftProcessIds();
            _inputActionHook = new GlobalInputActionHook(actionCounter, existingStarCraftProcesses);

            var report = await Task.Run(() => new PracticeSessionLauncher().Launch(
                plan,
                runtimeOptions,
                PracticeSessionLaunchOptions.Defaults()));
            var borderlessApplied = false;

            if (report.Player.StarCraftProcessId is not null)
            {
                borderlessApplied = cncDdrawHandlesPlayerDisplay ||
                    await Task.Run(() => StarCraftBorderlessWindow.ApplyToProcessWhenReady(
                        report.Player.StarCraftProcessId.Value,
                        screenBounds,
                        TimeSpan.FromSeconds(8)).Applied);
            }

            _practiceOverlay = new PracticeOverlayForm();
            _practiceOverlay.StartSession(screenBounds, sessionStartedAt, actionCounter);
            StartSessionHistory(plan, runtimeOptions, sessionStartedAt, actionCounter);
            if (report.Ai.StarCraftProcessId is not null)
            {
                _aiMinimizeKeeper = new StarCraftWindowMinimizeKeeper(
                    report.Ai.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(24),
                    TimeSpan.FromSeconds(45));
            }

            if (report.Player.StarCraftProcessId is not null && !cncDdrawHandlesPlayerDisplay)
            {
                _borderlessKeeper = new StarCraftBorderlessKeeper(
                    report.Player.StarCraftProcessId.Value,
                    screenBounds,
                    TimeSpan.FromSeconds(8));
            }

            _statusLabel.Text =
                $"{CurrentModeLabel()} 실행 완료 | 이전 프로세스 {report.StoppedLocalProcesses}개 정리 | " +
                $"사람 전체 창모드 {(borderlessApplied ? "적용" : "확인 실패")} | AI 창 최소화 | 타이머/APM ON | APMAlert OFF";
            WindowState = FormWindowState.Minimized;
        }
        catch (Exception ex)
        {
            DisposePracticeOverlay();
            _statusLabel.Text = $"{CurrentModeLabel()} 실행 실패";
            MessageBox.Show(this, ex.Message, $"{CurrentModeLabel()} 시작 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetLaunchButtonsEnabled(true);
        }
    }

    private PracticeLaunchPlan PrepareCurrentPlan()
    {
        if (_catalog is null || _mapList.SelectedItem is not MapItem mapItem)
        {
            throw new InvalidOperationException(IsLadderMode
                ? "래더 모드에서는 맵을 먼저 선택해주세요."
                : "봇과 맵을 먼저 선택해주세요.");
        }

        var bot = IsLadderMode
            ? LadderBotSelector.PickRandom(_catalog, mapItem.Map.Id, SelectedEnemyRace(), _random)
            : _botList.SelectedItem is BotItem botItem
                ? botItem.Bot
                : throw new InvalidOperationException("스파링 모드에서는 봇과 맵을 먼저 선택해주세요.");
        var aiRoot = RuntimeProvisioner.EnsureAiRoot(_paths.PlayerRuntimeRoot);
        var paths = _paths with { AiRuntimeRoot = aiRoot };
        var race = _playerRaceCombo.SelectedItem is StarCraftRace selectedRace ? selectedRace : StarCraftRace.Terran;
        var selection = new PracticeSelection(
            bot.Id,
            mapItem.Map.Id,
            race,
            IsLadderMode ? "StarAI Ladder" : "StarAI Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false);
        var plan = PracticeLaunchPlanBuilder.Build(_catalog, paths, selection);
        return RuntimeProvisioner.PrepareRuntimeAssets(plan);
    }

    private void SetLaunchButtonsEnabled(bool enabled)
    {
        _launchButton.Enabled = enabled;
    }

    private bool IsLadderMode => _modeCombo?.SelectedItem?.ToString() == "래더";

    private StarCraftRace? SelectedEnemyRace()
    {
        return _enemyRaceFilter?.SelectedItem?.ToString() switch
        {
            "테란" => StarCraftRace.Terran,
            "저그" => StarCraftRace.Zerg,
            "프로토스" => StarCraftRace.Protoss,
            "랜덤" => StarCraftRace.Random,
            _ => null
        };
    }

    private void UpdateModeControls()
    {
        if (_searchBox is null ||
            _sortCombo is null ||
            _botList is null ||
            _botListLabel is null ||
            _launchButton is null ||
            _buildCombo is null)
        {
            return;
        }

        var ladder = IsLadderMode;
        _searchBox.Enabled = !ladder;
        _sortCombo.Enabled = !ladder;
        _botList.Enabled = !ladder;
        _botListLabel.Text = ladder ? "래더 후보" : "상대 선택";
        _launchButton.Text = ladder ? "래더 시작" : "스파링 시작";
        _buildCombo.Enabled = false;
        if (_buildCombo.Items.Count > 0)
        {
            _buildCombo.SelectedIndex = 0;
        }

        UpdateDetails();
    }

    private string CurrentModeLabel()
    {
        return IsLadderMode ? "래더" : "스파링";
    }

    private static string RaceFilterLabel(StarCraftRace? race)
    {
        return race switch
        {
            StarCraftRace.Terran => "테란",
            StarCraftRace.Zerg => "저그",
            StarCraftRace.Protoss => "프로토스",
            StarCraftRace.Random => "랜덤",
            _ => "모두"
        };
    }

    private static string FormatDifficultyRange(IReadOnlyList<PracticeBot> candidates)
    {
        var labels = candidates
            .Select(bot => LadderDifficultyEstimator.EstimateFromSchnailElo(bot.Elo)?.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Cast<string>()
            .Distinct()
            .ToList();

        return labels.Count switch
        {
            0 => "미확인",
            1 => labels[0],
            _ => $"{labels.Last()} ~ {labels.First()}"
        };
    }

    private void StartSessionHistory(
        PracticeLaunchPlan plan,
        PracticeRuntimeOptions runtimeOptions,
        DateTime startedAtUtc,
        ActionRateCounter actionCounter)
    {
        _sessionHistoryTimer?.Stop();
        _sessionHistoryTimer?.Dispose();

        _activeActionCounter = actionCounter;
        _activeSessionStartedAtUtc = startedAtUtc;
        _activeSessionRecord = new PracticeSessionRecord(
            Id: Guid.NewGuid(),
            StartedAtUtc: startedAtUtc,
            LastUpdatedAtUtc: startedAtUtc,
            BotName: plan.Bot.Name,
            BotRace: plan.Bot.Race,
            MapName: plan.Map.Name,
            MapFileName: plan.Map.FileName,
            PlayerRace: plan.Player.Race,
            ReplayRoot: runtimeOptions.ReplayRoot,
            ActionCount: 0,
            ActionsPerMinute: 0,
            DurationSeconds: 0);

        UpdateActiveSessionHistory();
        _sessionHistoryTimer = new System.Windows.Forms.Timer
        {
            Interval = 10000
        };
        _sessionHistoryTimer.Tick += (_, _) => UpdateActiveSessionHistory();
        _sessionHistoryTimer.Start();
    }

    private void UpdateActiveSessionHistory()
    {
        if (_activeSessionRecord is null || _activeActionCounter is null)
        {
            return;
        }

        var elapsed = DateTime.UtcNow - _activeSessionStartedAtUtc;
        var record = _activeSessionRecord with
        {
            LastUpdatedAtUtc = DateTime.UtcNow,
            ActionCount = _activeActionCounter.ActionCount,
            ActionsPerMinute = _activeActionCounter.ActionsPerMinute(elapsed),
            DurationSeconds = Math.Round(Math.Max(0, elapsed.TotalSeconds), 1)
        };
        _activeSessionRecord = record;
        _historyStore.Upsert(record);
        if (_historyGrid is { IsDisposed: false })
        {
            RefreshHistory();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        DisposePracticeOverlay();
        base.OnFormClosed(e);
    }

    private void DisposePracticeOverlay()
    {
        UpdateActiveSessionHistory();
        _sessionHistoryTimer?.Stop();
        _sessionHistoryTimer?.Dispose();
        _sessionHistoryTimer = null;
        _activeSessionRecord = null;
        _activeActionCounter = null;

        _inputActionHook?.Dispose();
        _inputActionHook = null;

        _practiceOverlay?.Close();
        _practiceOverlay?.Dispose();
        _practiceOverlay = null;

        _borderlessKeeper?.Dispose();
        _borderlessKeeper = null;
        _aiMinimizeKeeper?.Dispose();
        _aiMinimizeKeeper = null;
    }

    private static Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.FromArgb(128, 218, 93),
            Location = new Point(x, y)
        };
    }

    private static ComboBox CreateCombo(int x, int y, int width)
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(x, y),
            Size = new Size(width, 28),
            BackColor = Color.Black,
            ForeColor = Color.FromArgb(166, 255, 126),
            FlatStyle = FlatStyle.Flat
        };
    }

    private static ListBox CreateListBox(int x, int y, int width, int height)
    {
        return new ListBox
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Color.Black,
            ForeColor = Color.FromArgb(166, 255, 126),
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 11)
        };
    }

    private static TextBox CreateTextBox(int x, int y, int width, int height)
    {
        return new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Color.Black,
            ForeColor = Color.FromArgb(166, 255, 126),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static Button CreateButton(string text, int x, int y, int width, int height)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Color.Black,
            ForeColor = Color.FromArgb(166, 255, 126),
            FlatStyle = FlatStyle.Flat
        };
    }

    private static TextBox CreateReadOnlyBlock(int x, int y, int width, int height, string text)
    {
        var textBox = CreateTextBox(x, y, width, height);
        textBox.Multiline = true;
        textBox.ReadOnly = true;
        textBox.Text = text;
        return textBox;
    }

    private static bool HotkeyMatchesQuery(HotkeyEntry entry, string query)
    {
        return string.IsNullOrWhiteSpace(query) ||
               entry.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               entry.CommandId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               entry.StringId.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
               entry.Hotkey.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string HotkeyRaceName(HotkeyEntry entry)
    {
        var id = entry.CommandId.ToLowerInvariant();
        return id switch
        {
            var value when value.StartsWith("terran_", StringComparison.Ordinal) => "Terran",
            var value when value.StartsWith("protoss_", StringComparison.Ordinal) => "Protoss",
            var value when value.StartsWith("zerg_", StringComparison.Ordinal) => "Zerg",
            _ => "Common"
        };
    }

    private static string HotkeyCategoryName(HotkeyEntry entry)
    {
        var id = entry.CommandId.ToLowerInvariant();
        return id switch
        {
            var value when value.StartsWith("general_", StringComparison.Ordinal) => "일반",
            var value when value.Contains("_train_", StringComparison.Ordinal) => "생산",
            var value when value.Contains("_build_", StringComparison.Ordinal) => "건설",
            var value when value.Contains("_res_", StringComparison.Ordinal) => "연구",
            var value when value.Contains("_upg_", StringComparison.Ordinal) => "업그레이드",
            var value when value.Contains("_spell_", StringComparison.Ordinal) => "기술",
            var value when value.Contains("_morph_", StringComparison.Ordinal) => "변태",
            _ => "기타"
        };
    }

    private static string HotkeyDepthCategoryName(HotkeyEntry entry)
    {
        return HotkeyCategoryName(entry) switch
        {
            "생산" => "유닛",
            "건설" => "건물",
            var category => category
        };
    }

    private static int HotkeyCommandRank(HotkeyEntry entry)
    {
        return HotkeyCategoryName(entry) switch
        {
            "생산" => 0,
            "건설" => 0,
            "기술" => 1,
            "연구" => 2,
            "업그레이드" => 3,
            _ => 9
        };
    }

    private static string HotkeyObjectDisplayName(HotkeyEntry entry)
    {
        var description = entry.Description.Trim();
        var suffixes = new[]
        {
            " 생산",
            " 소환",
            " 건설",
            " 개발",
            " 업그레이드",
            " 사용",
            " 연구"
        };

        foreach (var suffix in suffixes)
        {
            if (description.EndsWith(suffix, StringComparison.Ordinal))
            {
                return description[..^suffix.Length].Trim();
            }
        }

        return description;
    }

    private static string StripStarCraftMarkup(string value)
    {
        return value
            .Replace("<0>", string.Empty, StringComparison.Ordinal)
            .Replace("<1>", string.Empty, StringComparison.Ordinal)
            .Replace("<2>", string.Empty, StringComparison.Ordinal)
            .Replace("<3>", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private sealed record BotItem(PracticeBot Bot)
    {
        public override string ToString()
        {
            var elo = Bot.Elo is null ? "ELO ?" : $"ELO {Bot.Elo}";
            return $"{Bot.Name} / {Bot.Race} / {elo}";
        }
    }

    private sealed record MapItem(PracticeMap Map)
    {
        public override string ToString() => Map.Name;
    }

    private sealed record HotkeyObjectInfo(
        string Key,
        string Race,
        string Category,
        string DisplayName,
        int CategoryRank)
    {
        public static HotkeyObjectInfo From(HotkeyEntry entry)
        {
            var race = HotkeyRaceName(entry);
            var category = HotkeyDepthCategoryName(entry);
            var display = HotkeyObjectDisplayName(entry);
            var rank = category switch
            {
                "일반" => 0,
                "유닛" => 1,
                "건물" => 2,
                "기술" => 3,
                "연구" => 4,
                "업그레이드" => 5,
                "변태" => 6,
                _ => 9
            };
            var key = $"{race}|{category}|{display}".ToLowerInvariant();
            return new HotkeyObjectInfo(key, race, category, display, rank);
        }
    }

    private sealed record HotkeyObjectItem(HotkeyObjectInfo Info, int Count)
    {
        public string Key => Info.Key;

        public override string ToString() => $"{Info.Category}  {Info.DisplayName}  ({Count})";
    }

}
