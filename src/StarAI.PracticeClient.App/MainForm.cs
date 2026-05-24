using System.Diagnostics;
using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

public sealed class MainForm : Form
{
    private readonly PracticeConfigurator _configurator = new();
    private readonly PracticeLauncher _launcher = new();
    private readonly MatchHistoryStore _history = new();

    private TextBox _rootBox = null!;
    private ComboBox _playerRaceBox = null!;
    private ComboBox _enemyRaceBox = null!;
    private ComboBox _tierBox = null!;
    private ComboBox _sortBox = null!;
    private ComboBox _buildFilterBox = null!;
    private TextBox _searchBox = null!;
    private ListBox _botList = null!;
    private ComboBox _mapBox = null!;
    private ComboBox _buildBox = null!;
    private ComboBox _speedBox = null!;
    private TextBox _gameNameBox = null!;
    private CheckBox _windowedBox = null!;
    private CheckBox _coachBox = null!;
    private ComboBox _coachBuildBox = null!;
    private Button _startButton = null!;
    private TextBox _detailsBox = null!;
    private TextBox _statusBox = null!;

    private IReadOnlyList<BotProfile> _allBots = Array.Empty<BotProfile>();
    private IReadOnlyList<MapProfile> _allMaps = Array.Empty<MapProfile>();
    private Race _selectedPlayerRace = Race.Terran;
    private bool _loadedInitialData;

    public MainForm()
    {
        Text = "StarAI 연습 클라이언트";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 740);
        Size = new Size(1260, 800);
        BackColor = UiPalette.Background;
        ForeColor = UiPalette.Text;
        Font = new Font("Malgun Gothic", 10F);

        BuildUi();
        LoadData(PracticeCatalog.DefaultRoot);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = UiPalette.Background,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(10)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 24));
        Controls.Add(root);

        root.Controls.Add(BuildLeftPanel(), 0, 0);
        root.Controls.Add(BuildCenterPanel(), 1, 0);
        root.Controls.Add(BuildRightPanel(), 2, 0);

        _statusBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.Black,
            ForeColor = UiPalette.Text,
            BorderStyle = BorderStyle.FixedSingle
        };
        root.Controls.Add(_statusBox, 0, 1);
        root.SetColumnSpan(_statusBox, 3);
    }

    private Control BuildLeftPanel()
    {
        var panel = Panel("상대 선택");

        panel.Controls.Add(Label("내 종족"));
        _playerRaceBox = Combo(new object[]
        {
            new RaceChoice("테란", Race.Terran),
            new RaceChoice("프로토스", Race.Protoss),
            new RaceChoice("저그", Race.Zerg),
            new RaceChoice("랜덤", Race.Random)
        });
        _playerRaceBox.SelectedIndexChanged += (_, _) =>
        {
            if (_playerRaceBox.SelectedItem is RaceChoice choice && choice.Race is { } race)
            {
                _selectedPlayerRace = race;
            }

            RefreshBots();
        };
        panel.Controls.Add(_playerRaceBox);

        panel.Controls.Add(Label("상대 종족"));
        _enemyRaceBox = Combo(new object[]
        {
            new RaceChoice("전체", null),
            new RaceChoice("테란", Race.Terran),
            new RaceChoice("프로토스", Race.Protoss),
            new RaceChoice("저그", Race.Zerg),
            new RaceChoice("랜덤", Race.Random)
        });
        _enemyRaceBox.SelectedIndexChanged += (_, _) =>
        {
            RefreshBuildFilters();
            RefreshBots();
        };
        panel.Controls.Add(_enemyRaceBox);

        panel.Controls.Add(Label("난이도"));
        _tierBox = Combo(new object[]
        {
            new TierChoice("전체", null),
            new TierChoice("복구", DifficultyTier.Recovery),
            new TierChoice("메인", DifficultyTier.Main),
            new TierChoice("도전", DifficultyTier.Challenge),
            new TierChoice("드릴", DifficultyTier.Drill),
            new TierChoice("실험", DifficultyTier.Experimental)
        });
        _tierBox.SelectedIndexChanged += (_, _) => RefreshBots();
        panel.Controls.Add(_tierBox);

        panel.Controls.Add(Label("빌드 필터"));
        _buildFilterBox = Combo(Array.Empty<object>());
        _buildFilterBox.SelectedIndexChanged += (_, _) => RefreshBots();
        panel.Controls.Add(_buildFilterBox);

        panel.Controls.Add(Label("정렬"));
        _sortBox = Combo(new object[]
        {
            new SortChoice("추천순", "recommended"),
            new SortChoice("ELO 낮은순", "elo-asc"),
            new SortChoice("ELO 높은순", "elo-desc"),
            new SortChoice("이름순", "name")
        });
        _sortBox.SelectedIndexChanged += (_, _) => RefreshBots();
        panel.Controls.Add(_sortBox);

        panel.Controls.Add(Label("검색"));
        _searchBox = TextBox("봇 이름, 빌드, 메모 검색");
        _searchBox.TextChanged += (_, _) => RefreshBots();
        panel.Controls.Add(_searchBox);

        _botList = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            ForeColor = UiPalette.Text,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false
        };
        _botList.SelectedIndexChanged += (_, _) => OnBotSelected();
        panel.Controls.Add(_botList);
        MakeRowFill(panel, _botList);

        return panel;
    }

    private Control BuildCenterPanel()
    {
        var panel = Panel("연습 설정");

        panel.Controls.Add(Label("맵"));
        _mapBox = Combo(Array.Empty<object>());
        panel.Controls.Add(_mapBox);

        panel.Controls.Add(Label("봇 빌드 / 행동"));
        _buildBox = Combo(Array.Empty<object>());
        _buildBox.SelectedIndexChanged += (_, _) => UpdateDetails();
        panel.Controls.Add(_buildBox);

        panel.Controls.Add(Label("봇 메모"));
        _detailsBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.Black,
            ForeColor = UiPalette.Text,
            BorderStyle = BorderStyle.FixedSingle
        };
        panel.Controls.Add(_detailsBox);
        MakeRowFill(panel, _detailsBox);

        return panel;
    }

    private Control BuildRightPanel()
    {
        var panel = Panel("실행");

        panel.Controls.Add(Label("StarCraft 1.16.1 폴더"));
        var rootRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
        rootRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        _rootBox = TextBox(null);
        _rootBox.TextChanged += (_, _) => LoadData(_rootBox.Text.Trim(), keepSelection: true);
        rootRow.Controls.Add(_rootBox, 0, 0);
        rootRow.Controls.Add(Button("찾기", (_, _) => BrowseRoot()), 1, 0);
        panel.Controls.Add(rootRow);

        panel.Controls.Add(Label("방 이름"));
        _gameNameBox = TextBox(null);
        _gameNameBox.Text = "AIPractice";
        panel.Controls.Add(_gameNameBox);

        panel.Controls.Add(Label("게임 속도"));
        _speedBox = Combo(new object[]
        {
            new SpeedChoice("기본 속도", null),
            new SpeedChoice("Fastest 고정 (42 ms/frame)", 42),
            new SpeedChoice("조금 빠르게 (24 ms/frame)", 24)
        });
        _speedBox.SelectedIndex = 1;
        panel.Controls.Add(_speedBox);

        _windowedBox = Check("창모드 / W-MODE", true);
        panel.Controls.Add(_windowedBox);

        _coachBox = Check("CoachAI 오버레이", true);
        panel.Controls.Add(_coachBox);

        panel.Controls.Add(Label("CoachAI 빌드표"));
        _coachBuildBox = Combo(new object[]
        {
            new CoachBuildChoice("내 종족 기본", "auto"),
            new CoachBuildChoice(CoachAiBuildPresets.KeepExisting.Name, CoachAiBuildPresets.KeepExisting.Id)
        }.Concat(CoachAiBuildPresets.All.Select(preset => new CoachBuildChoice(preset.Name, preset.Id))));
        panel.Controls.Add(_coachBuildBox);

        _startButton = Button("스파링 시작", async (_, _) => await StartSparringAsync());
        panel.Controls.Add(_startButton);
        panel.Controls.Add(Button("핫키 편집", (_, _) => OpenHotkeyEditor()));
        panel.Controls.Add(Button("전적 / 리플레이", (_, _) => OpenHistory()));
        panel.Controls.Add(Button("스타 폴더 열기", (_, _) => OpenStarCraftFolder()));

        var help = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = UiPalette.Dim,
            BorderStyle = BorderStyle.FixedSingle,
            Text =
                "흐름:\r\n" +
                "1. 봇/맵/빌드를 고른 뒤 스파링 시작을 누릅니다.\r\n" +
                "2. 내 클라이언트가 먼저 켜져 설정한 맵으로 Local PC 방을 만듭니다.\r\n" +
                "3. 잠시 뒤 AI 클라이언트가 같은 방에 자동 참가합니다.\r\n\r\n" +
                @"리플레이는 D:\OneDrive\Documents\StarCraft\Maps\Replays\ai 아래 날짜별 폴더에 저장됩니다."
        };
        panel.Controls.Add(help);
        MakeRowFill(panel, help);

        return panel;
    }

    private void LoadData(string starCraftRoot, bool keepSelection = false)
    {
        if (string.IsNullOrWhiteSpace(starCraftRoot))
        {
            return;
        }

        var currentBot = SelectedBot()?.Id;
        var currentMap = SelectedMap()?.RelativePath;

        if (_rootBox is not null && !string.Equals(_rootBox.Text, starCraftRoot, StringComparison.OrdinalIgnoreCase))
        {
            _rootBox.Text = starCraftRoot;
        }

        _allBots = PracticeCatalog.GetAvailableBots(starCraftRoot);
        _allMaps = PracticeCatalog.GetMaps(starCraftRoot);

        _mapBox.Items.Clear();
        foreach (var map in _allMaps)
        {
            _mapBox.Items.Add(map);
        }

        if (_mapBox.Items.Count > 0)
        {
            var selectedIndex = 0;
            if (keepSelection && currentMap is not null)
            {
                selectedIndex = _allMaps.ToList().FindIndex(map => string.Equals(map.RelativePath, currentMap, StringComparison.OrdinalIgnoreCase));
            }

            if (selectedIndex < 0)
            {
                selectedIndex = _allMaps.ToList().FindIndex(map => map.Name.Contains("Fighting Spirit", StringComparison.OrdinalIgnoreCase));
            }

            _mapBox.SelectedIndex = Math.Max(0, selectedIndex);
        }

        if (!_loadedInitialData)
        {
            SelectRace(_playerRaceBox, Race.Terran);
            _loadedInitialData = true;
        }
        RefreshBuildFilters();
        RefreshBots(currentBot);
        UpdateCoachStatus();
        Log($"{starCraftRoot}에서 사용 가능한 봇 {_allBots.Count}개, 맵 {_allMaps.Count}개를 불러왔습니다.");
    }

    private void RefreshBots(string? preferredBotId = null)
    {
        if (_botList is null)
        {
            return;
        }

        preferredBotId ??= SelectedBot()?.Id;
        var enemyRace = SelectedEnemyRace();
        var tier = SelectedTier();
        var buildFilter = _buildFilterBox.SelectedItem?.ToString() ?? "전체";
        var search = _searchBox.Text.Trim();

        IEnumerable<BotProfile> bots = _allBots;
        if (enemyRace is not null)
        {
            bots = bots.Where(bot => bot.Race == enemyRace);
        }

        if (tier is not null)
        {
            bots = bots.Where(bot => bot.Tier == tier);
        }

        bots = bots.Where(bot => MatchesBuildFilter(bot, buildFilter));

        if (!string.IsNullOrWhiteSpace(search))
        {
            bots = bots.Where(bot => SearchText(bot).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        bots = SortBots(bots);
        var items = bots.Select(bot => new BotListItem(bot)).ToArray();
        _botList.BeginUpdate();
        _botList.DataSource = null;
        _botList.DataSource = items;
        _botList.EndUpdate();

        if (items.Length > 0)
        {
            var index = preferredBotId is null
                ? 0
                : Array.FindIndex(items, item => string.Equals(item.Bot.Id, preferredBotId, StringComparison.OrdinalIgnoreCase));
            _botList.SelectedIndex = Math.Max(0, index);
        }
        else
        {
            _buildBox.Items.Clear();
            _detailsBox.Text = "조건에 맞는 봇이 없습니다.";
        }
    }

    private IEnumerable<BotProfile> SortBots(IEnumerable<BotProfile> bots)
    {
        var mode = (_sortBox.SelectedItem as SortChoice)?.Id ?? "recommended";
        return mode switch
        {
            "elo-asc" => bots.OrderBy(bot => bot.Elo ?? int.MaxValue).ThenBy(bot => bot.Name),
            "elo-desc" => bots.OrderByDescending(bot => bot.Elo ?? int.MinValue).ThenBy(bot => bot.Name),
            "name" => bots.OrderBy(bot => bot.Name),
            _ => bots.OrderBy(bot => bot.Tier).ThenBy(bot => bot.Elo ?? int.MaxValue).ThenBy(bot => bot.Name)
        };
    }

    private void RefreshBuildFilters()
    {
        if (_buildFilterBox is null)
        {
            return;
        }

        var selected = _buildFilterBox.SelectedItem?.ToString();
        var enemyRace = SelectedEnemyRace();
        var bots = enemyRace is null
            ? _allBots
            : _allBots.Where(bot => bot.Race == enemyRace).ToArray();

        var filters = new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { "전체", "기본" };
        foreach (var bot in bots)
        {
            foreach (var tag in bot.SearchTags)
            {
                if (!IsGenericTag(tag))
                {
                    filters.Add(TagKo(tag));
                }
            }

            foreach (var build in bot.BuildOptions.Where(option => !string.Equals(option.Id, "default", StringComparison.OrdinalIgnoreCase)))
            {
                filters.Add(build.Name);
            }
        }

        _buildFilterBox.Items.Clear();
        foreach (var filter in filters)
        {
            _buildFilterBox.Items.Add(filter);
        }

        var index = selected is null ? -1 : _buildFilterBox.Items.IndexOf(selected);
        _buildFilterBox.SelectedIndex = index >= 0 ? index : 0;
    }

    private void OnBotSelected()
    {
        _buildBox.Items.Clear();
        if (SelectedBot() is not { } bot)
        {
            UpdateDetails();
            return;
        }

        foreach (var build in bot.BuildOptions.DefaultIfEmpty(new BuildOption("default", "기본", "기본 행동")))
        {
            _buildBox.Items.Add(new BuildListItem(build));
        }

        _buildBox.SelectedIndex = _buildBox.Items.Count > 0 ? 0 : -1;
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        if (SelectedBot() is not { } bot)
        {
            _detailsBox.Text = "봇을 선택하세요.";
            return;
        }

        var build = SelectedBuild();
        var elo = bot.Elo?.ToString() ?? "미상";
        var tags = bot.SearchTags.Count == 0
            ? "없음"
            : string.Join(", ", bot.SearchTags.Select(TagKo).Distinct(StringComparer.OrdinalIgnoreCase));

        _detailsBox.Text =
            $"{bot.Name} ({RaceKo(bot.Race)}, {TierKo(bot.Tier)}, ELO {elo})\r\n\r\n" +
            $"성향: {KoreanStyle(bot)}\r\n\r\n" +
            $"빌드 힌트: {KoreanBuildHint(bot)}\r\n\r\n" +
            $"마이크로 위험도: {KoreanMicroRisk(bot)}\r\n\r\n" +
            $"선택 빌드: {BuildNameKo(build)}\r\n" +
            $"빌드 설명: {BuildDescriptionKo(build, bot)}\r\n\r\n" +
            $"태그: {tags}\r\n\r\n" +
            $"DLL: {bot.RelativeDllPath}";
    }

    private PracticeSettings CurrentSettings()
    {
        var root = _rootBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("StarCraft 1.16.1 폴더를 지정하세요.");
        }

        var bot = SelectedBot() ?? throw new InvalidOperationException("상대 봇을 선택하세요.");
        var map = SelectedMap() ?? throw new InvalidOperationException("맵을 선택하세요.");
        var playerRace = SelectedPlayerRace();
        var gameName = string.IsNullOrWhiteSpace(_gameNameBox.Text) ? "AIPractice" : _gameNameBox.Text.Trim();

        return new PracticeSettings(
            root,
            bot,
            map,
            playerRace,
            gameName,
            _windowedBox.Checked,
            (_speedBox.SelectedItem as SpeedChoice)?.SpeedOverrideMs,
            SelectedBuild(),
            _coachBox.Checked);
    }

    private async Task StartSparringAsync()
    {
        _startButton.Enabled = false;

        try
        {
            var settings = CurrentSettings();
            var hotkeys = HotkeyImporter.ImportBestAvailable(settings.StarCraftRoot);
            Log(hotkeys.Message);

            Log("같은 StarCraft 폴더에서 1번은 내 클라용 INI, 2번은 봇용 INI로 순차 실행합니다.");
            var botSettings = settings with { EnableCoachAi = false };

            var issues = _configurator.Validate(settings).ToArray();
            var errors = issues.Where(issue => issue.IsError).ToArray();
            if (errors.Length > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(issue => issue.Message)));
            }

            foreach (var issue in issues.Where(issue => !issue.IsError))
            {
                Log("참고: " + issue.Message);
            }

            var coachDll = settings.EnableCoachAi
                ? CoachAiLocator.FindCoachAiDll(settings.StarCraftRoot) ?? throw new InvalidOperationException("CoachAI DLL을 찾지 못했습니다.")
                : null;

            if (settings.EnableCoachAi)
            {
                PracticeConfigurator.ApplyCoachAiBuildPreset(settings.StarCraftRoot, SelectedCoachBuildPreset(settings.PlayerRace));
            }

            var playerIni = _configurator.ApplyPlayerHost(settings, coachDll);
            Log($"선택값 확인: 내 종족 {RaceKo(settings.PlayerRace)}, 상대 봇 {settings.Bot.Name}({RaceKo(settings.Bot.Race)}), 맵 {settings.Map.Name}");
            Log($"내 클라이언트 설정 완료: {RaceKo(settings.PlayerRace)} / {settings.Map.Name} / {settings.GameName}. INI: {playerIni}");

            _launcher.LaunchChaos(settings.StarCraftRoot, ChaosLaunchMode.Bot, clickStart: true, closeLauncherAfterStart: false);
            Log("내 클라이언트를 실행했습니다. 자동으로 Local PC 방 생성을 시도합니다.");

            await Task.Delay(TimeSpan.FromSeconds(25));

            var botIni = _configurator.ApplyBotJoin(botSettings);
            Log($"AI 참가 설정 완료: {settings.Bot.Name} / {RaceKo(settings.Bot.Race)} / 소리 OFF. INI: {botIni}");

            _launcher.LaunchChaos(settings.StarCraftRoot, ChaosLaunchMode.Bot, clickStart: true, closeLauncherAfterStart: false);
            Log("AI 클라이언트를 실행했습니다. 같은 방에 자동 참가를 시도합니다.");

            _history.Add(settings.StarCraftRoot, new MatchRecord(
                DateTime.Now,
                settings.Bot.Name,
                settings.Bot.Race,
                settings.Bot.Elo,
                settings.Map.Name,
                settings.BuildOption?.Name ?? "기본",
                PracticeConfigurator.DefaultReplayRoot));

            Log(@"리플레이 저장 위치: D:\OneDrive\Documents\StarCraft\Maps\Replays\ai\날짜별 폴더");
        }
        catch (Exception ex)
        {
            Log("오류: " + ex.Message);
            MessageBox.Show(this, ex.Message, "StarAI 연습 클라이언트", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _startButton.Enabled = true;
        }
    }

    private void BrowseRoot()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "StarCraft 1.16.1 폴더를 선택하세요.",
            SelectedPath = Directory.Exists(_rootBox.Text) ? _rootBox.Text : PracticeCatalog.DefaultRoot
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _rootBox.Text = dialog.SelectedPath;
            LoadData(dialog.SelectedPath);
        }
    }

    private void OpenHotkeyEditor()
    {
        using var form = new HotkeyEditorForm(_rootBox.Text.Trim());
        form.ShowDialog(this);
    }

    private void OpenHistory()
    {
        using var form = new MatchHistoryForm(_rootBox.Text.Trim());
        form.ShowDialog(this);
    }

    private void OpenStarCraftFolder()
    {
        var root = _rootBox.Text.Trim();
        if (!Directory.Exists(root))
        {
            MessageBox.Show(this, "StarCraft 폴더가 존재하지 않습니다.", "StarAI 연습 클라이언트");
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = root, UseShellExecute = true });
    }

    private void UpdateCoachStatus()
    {
        if (_coachBox is null || _rootBox is null)
        {
            return;
        }

        var hasCoach = CoachAiLocator.FindCoachAiDll(_rootBox.Text.Trim()) is not null;
        _coachBox.Enabled = hasCoach;
        _coachBox.Checked = hasCoach;
        _coachBox.ForeColor = hasCoach ? UiPalette.Text : UiPalette.Dim;
        if (_coachBuildBox is not null)
        {
            _coachBuildBox.Enabled = hasCoach;
        }
    }

    private BotProfile? SelectedBot() => (_botList.SelectedItem as BotListItem)?.Bot;
    private MapProfile? SelectedMap() => _mapBox.SelectedItem as MapProfile;
    private BuildOption? SelectedBuild() => (_buildBox.SelectedItem as BuildListItem)?.Build;

    private CoachAiBuildPreset SelectedCoachBuildPreset(Race playerRace)
    {
        var choice = _coachBuildBox.SelectedItem as CoachBuildChoice;
        if (choice is null || choice.Id == "auto")
        {
            return CoachAiBuildPresets.DefaultForRace(playerRace);
        }

        if (choice.Id == CoachAiBuildPresets.KeepExisting.Id)
        {
            return CoachAiBuildPresets.KeepExisting;
        }

        return CoachAiBuildPresets.All.FirstOrDefault(preset => preset.Id == choice.Id)
            ?? CoachAiBuildPresets.DefaultForRace(playerRace);
    }

    private Race SelectedPlayerRace()
    {
        if (_playerRaceBox.SelectedItem is RaceChoice choice && choice.Race is { } race)
        {
            _selectedPlayerRace = race;
        }

        return _selectedPlayerRace;
    }
    private Race? SelectedEnemyRace() => (_enemyRaceBox.SelectedItem as RaceChoice)?.Race;
    private DifficultyTier? SelectedTier() => (_tierBox.SelectedItem as TierChoice)?.Tier;

    private void SelectRace(ComboBox combo, Race race)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is RaceChoice choice && choice.Race == race)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private static bool MatchesBuildFilter(BotProfile bot, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || filter == "전체")
        {
            return true;
        }

        if (filter == "기본")
        {
            return true;
        }

        return SearchText(bot).Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               bot.SearchTags.Select(TagKo).Any(tag => tag.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    private static string SearchText(BotProfile bot)
    {
        return string.Join(" ", new[]
        {
            bot.Name,
            bot.Race.ToString(),
            bot.Tier.ToString(),
            bot.Style,
            bot.BuildHints,
            bot.MicroRisk,
            string.Join(" ", bot.SearchTags),
            string.Join(" ", bot.BuildOptions.SelectMany(option => new[] { option.Name, option.Description }))
        });
    }

    private static bool IsGenericTag(string tag)
    {
        return tag.Equals("terran", StringComparison.OrdinalIgnoreCase) ||
               tag.Equals("protoss", StringComparison.OrdinalIgnoreCase) ||
               tag.Equals("zerg", StringComparison.OrdinalIgnoreCase) ||
               tag.Equals("random", StringComparison.OrdinalIgnoreCase) ||
               tag.Equals("main", StringComparison.OrdinalIgnoreCase) ||
               tag.Equals("challenge", StringComparison.OrdinalIgnoreCase) ||
               tag.Equals("warmup", StringComparison.OrdinalIgnoreCase) ||
               tag.Equals("low", StringComparison.OrdinalIgnoreCase);
    }

    private static string KoreanStyle(BotProfile bot)
    {
        if (bot.Tier == DifficultyTier.Drill)
        {
            return $"{RaceKo(bot.Race)} 날빌/방어 드릴용 봇입니다. 메인 스파링보다는 특정 상황 연습에 맞습니다.";
        }

        if (bot.Tier == DifficultyTier.Recovery)
        {
            return $"{RaceKo(bot.Race)} 감각 복구용 저강도 봇입니다. 빌드 순서, 일꾼 생산, 첫 정찰을 다시 손에 붙이기 좋습니다.";
        }

        if (bot.Tier == DifficultyTier.Challenge)
        {
            return $"강한 {RaceKo(bot.Race)} 봇입니다. 매일 상대하기보다는 빌드가 안정된 뒤 체크용으로 쓰는 쪽이 좋습니다.";
        }

        if (bot.Race == Race.Random)
        {
            return "Steamhammer 계열 랜덤 종족 봇입니다. 상대 종족/오프닝 예측을 줄이고 싶을 때 쓰는 확인용입니다.";
        }

        return $"{RaceKo(bot.Race)} 메인 스파링 후보입니다. 너무 낮은 압박은 아니지만 최상위 봇처럼 과하게 몰아붙이는 타입은 아닙니다.";
    }

    private static string KoreanBuildHint(BotProfile bot)
    {
        var buildNames = bot.BuildOptions
            .Where(option => !string.Equals(option.Id, "default", StringComparison.OrdinalIgnoreCase))
            .Select(option => option.Name)
            .Take(6)
            .ToArray();

        if (buildNames.Length > 0)
        {
            return "선택 가능한 빌드: " + string.Join(", ", buildNames) + ". 특정 빌드를 고르면 가능한 경우 봇 설정 파일의 전략 가중치를 조정합니다.";
        }

        return bot.Tier switch
        {
            DifficultyTier.Recovery => "빌드 강제 옵션은 없지만, 초반 빌드 복구와 첫 멀티 타이밍 확인용으로 쓰기 좋습니다.",
            DifficultyTier.Main => "기본 행동으로 돌려보고, 리플레이 느낌이 괜찮으면 메인 스파링 풀에 남겨두세요.",
            DifficultyTier.Challenge => "상위 체크용입니다. 승패보다 내 빌드가 무너지지 않는지 보는 용도로 두세요.",
            DifficultyTier.Drill => "일반 운영이 아니라 특정 초반 공격/방어 드릴용입니다.",
            _ => "실험 후보입니다. 첫 판 리플레이를 보고 계속 쓸지 결정하세요."
        };
    }

    private static string KoreanMicroRisk(BotProfile bot)
    {
        if (bot.MicroRisk.Contains("High", StringComparison.OrdinalIgnoreCase) || bot.Tier == DifficultyTier.Challenge)
        {
            return "높음. 컨트롤이 봇스럽게 느껴질 수 있어 체크용으로 권장합니다.";
        }

        if (bot.MicroRisk.Contains("Low", StringComparison.OrdinalIgnoreCase) || bot.Tier == DifficultyTier.Recovery)
        {
            return "낮음~중간. 빌드 복구를 크게 방해하지 않는 편입니다.";
        }

        return "중간. 첫 2~3판 리플레이를 보고 너무 봇스럽다면 제외하세요.";
    }

    private static string BuildNameKo(BuildOption? build)
    {
        if (build is null)
        {
            return "기본";
        }

        return build.Name switch
        {
            "Default" or "Default mix" => "기본",
            _ => build.Name
        };
    }

    private static string BuildDescriptionKo(BuildOption? build, BotProfile bot)
    {
        if (build is null || build.Id == "default")
        {
            return "봇 기본 전략 풀을 그대로 사용합니다.";
        }

        if (build.Patch is null)
        {
            return "설명용 빌드입니다. 현재 봇 설정 파일을 직접 바꾸지는 않습니다.";
        }

        return $"{build.Name} 계열을 우선하도록 {bot.Name} 설정 파일을 조정합니다.";
    }

    private static string RaceKo(Race race) => race switch
    {
        Race.Terran => "테란",
        Race.Protoss => "프로토스",
        Race.Zerg => "저그",
        Race.Random => "랜덤",
        _ => race.ToString()
    };

    private static string TierKo(DifficultyTier tier) => tier switch
    {
        DifficultyTier.Recovery => "복구",
        DifficultyTier.Main => "메인",
        DifficultyTier.Challenge => "도전",
        DifficultyTier.Drill => "드릴",
        DifficultyTier.Experimental => "실험",
        _ => tier.ToString()
    };

    private static string TagKo(string tag) => tag.ToLowerInvariant() switch
    {
        "warmup" => "워밍업",
        "recovery" => "복구",
        "bio" => "바이오",
        "macro" => "운영",
        "wall" => "입구막기",
        "heuristic" => "전략예측",
        "defensive" => "수비형",
        "vulture" => "벌처",
        "harass" => "견제",
        "experimental" => "실험",
        "rush" => "러시",
        "reaver" => "리버",
        "drop" => "드랍",
        "fe" => "더블",
        "dt" => "다크",
        "goon" => "드라군",
        "carrier" => "캐리어",
        "mix" => "혼합",
        "replay-derived" => "리플레이 기반",
        "probe rush" => "프로브 러시",
        "zealot rush" => "질럿 러시",
        "ling" => "저글링",
        "hydra" => "히드라",
        "muta" => "뮤탈",
        "adaptive" => "적응형",
        "overkill" => "오버킬",
        "pool" => "풀",
        "human-like" => "사람식",
        "5 pool" => "5풀",
        "top" => "최상위",
        "main" => "메인",
        "challenge" => "도전",
        "terran" => "테란",
        "protoss" => "프로토스",
        "zerg" => "저그",
        "random" => "랜덤",
        _ => tag
    };

    private void Log(string message)
    {
        _statusBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private static TableLayoutPanel Panel(string title)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = UiPalette.Panel,
            ForeColor = UiPalette.Text,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 1
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = UiPalette.Text,
            Font = new Font("Malgun Gothic", 14F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        });
        return panel;
    }

    private static void MakeRowFill(TableLayoutPanel panel, Control control)
    {
        var row = panel.GetPositionFromControl(control).Row;
        while (panel.RowStyles.Count <= row)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        for (var i = 0; i < panel.RowStyles.Count; i++)
        {
            panel.RowStyles[i] = new RowStyle(i == row ? SizeType.Percent : SizeType.AutoSize, i == row ? 100 : 0);
        }
    }

    private static Label Label(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = UiPalette.Dim,
            Margin = new Padding(0, 4, 0, 2)
        };
    }

    private static TextBox TextBox(string? placeholder)
    {
        return new TextBox
        {
            Dock = DockStyle.Top,
            BackColor = Color.Black,
            ForeColor = UiPalette.Text,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = placeholder ?? string.Empty,
            Margin = new Padding(0, 0, 0, 6)
        };
    }

    private static CheckBox Check(string text, bool isChecked)
    {
        return new CheckBox
        {
            Text = text,
            Dock = DockStyle.Top,
            Checked = isChecked,
            ForeColor = UiPalette.Text,
            Margin = new Padding(0, 4, 0, 4)
        };
    }

    private static ComboBox Combo(IEnumerable<object> items)
    {
        var combo = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.Black,
            ForeColor = UiPalette.Text,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, 6)
        };

        foreach (var item in items)
        {
            combo.Items.Add(item);
        }

        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }

        return combo;
    }

    private static Button Button(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Black,
            ForeColor = UiPalette.Text,
            Height = 36,
            Margin = new Padding(0, 4, 0, 4)
        };
        button.FlatAppearance.BorderColor = UiPalette.Border;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 60, 28);
        button.Click += onClick;
        return button;
    }

    private sealed record RaceChoice(string Label, Race? Race)
    {
        public override string ToString() => Label;
    }

    private sealed record TierChoice(string Label, DifficultyTier? Tier)
    {
        public override string ToString() => Label;
    }

    private sealed record SortChoice(string Label, string Id)
    {
        public override string ToString() => Label;
    }

    private sealed record SpeedChoice(string Label, int? SpeedOverrideMs)
    {
        public override string ToString() => Label;
    }

    private sealed record CoachBuildChoice(string Label, string Id)
    {
        public override string ToString() => Label;
    }

    private sealed record BuildListItem(BuildOption Build)
    {
        public override string ToString() => BuildNameKo(Build);
    }

    private sealed record BotListItem(BotProfile Bot)
    {
        public override string ToString()
        {
            var elo = Bot.Elo is null ? "ELO ?" : $"ELO {Bot.Elo}";
            return $"{Bot.Name}    {RaceKo(Bot.Race)}  {TierKo(Bot.Tier)}  {elo}";
        }
    }
}
