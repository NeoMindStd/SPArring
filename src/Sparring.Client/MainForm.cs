using Sparring.Core;

namespace Sparring.Client;

public sealed class MainForm : Form
{
    private readonly PracticePaths _paths = PracticePaths.Defaults();
    private readonly SparringSettingsStore _settingsStore = SparringSettingsStore.Default();
    private readonly PracticeSessionHistoryStore _historyStore = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Sparring",
        "history.json"));
    private readonly PracticeLadderRatingStore _ratingStore = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Sparring",
        "ladder-rating.json"));
    private readonly Random _random = new();
    private readonly Dictionary<string, Image> _hotkeyIconCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ToolTip _hotkeyToolTip = new();
    private SparringSettings _settings = SparringSettings.Defaults();
    private PracticeCatalog? _catalog;
    private ListBox _botList = null!;
    private ListBox _mapList = null!;
    private PictureBox _mapPreviewBox = null!;
    private Label _mapPreviewLabel = null!;
    private TextBox _searchBox = null!;
    private ComboBox _modeCombo = null!;
    private ComboBox _enemyRaceFilter = null!;
    private ComboBox _sortCombo = null!;
    private ComboBox _playerRaceCombo = null!;
    private ComboBox _buildCombo = null!;
    private TabControl _tabs = null!;
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
    private ComboBox _hotkeyPageFilter = null!;
    private ListBox _hotkeyObjectList = null!;
    private TableLayoutPanel _hotkeyCommandPanel = null!;
    private TextBox _hotkeySearch = null!;
    private TextBox _hotkeyKeyText = null!;
    private Label _hotkeyCommandTitle = null!;
    private Label _hotkeyCommandMeta = null!;
    private Label _hotkeyDefaultText = null!;
    private Label _hotkeyCountLabel = null!;
    private TextBox _hotkeyHelpText = null!;
    private TextBox _hotkeyRuntimeHelpText = null!;
    private TextBox _replayRootText = null!;
    private TextBox _userMapRootText = null!;
    private TextBox _ladderMapRootText = null!;
    private TextBox _ladderRatingText = null!;
    private CheckBox _hideAiNameCheck = null!;
    private ComboBox _gameSpeedCombo = null!;
    private NumericUpDown _mouseScrollSpeedInput = null!;
    private NumericUpDown _keyboardScrollSpeedInput = null!;
    private Label _ladderRatingLabel = null!;
    private Button _applyRatingButton = null!;
    private Button _resetRatingButton = null!;
    private readonly List<Button> _hotkeyRaceButtons = [];
    private readonly List<Button> _hotkeyCategoryButtons = [];
    private readonly List<Button> _hotkeyPageButtons = [];
    private IReadOnlyList<HotkeyEntry> _hotkeyEntries = [];
    private HotkeyEntry? _selectedHotkeyEntry;
    private PracticeOverlayForm? _practiceOverlay;
    private GlobalInputActionHook? _inputActionHook;
    private StarCraftBorderlessKeeper? _borderlessKeeper;
    private StarCraftWindowMinimizeOnceWhenReady? _aiMinimizeKeeper;
    private System.Windows.Forms.Timer? _sessionHistoryTimer;
    private System.Windows.Forms.Timer? _sessionLifecycleTimer;
    private PracticeSessionRecord? _activeSessionRecord;
    private ActionRateCounter? _activeActionCounter;
    private ActivePracticeSession? _activeSession;
    private DateTime _activeSessionStartedAtUtc;
    private int _notInGameSamples;
    private bool _finalizingSession;
    private bool _playerExitShortcutRequested;
    private bool _updatingSelections;
    private Image? _mapPreviewImage;

    public MainForm()
    {
        _settings = _settingsStore.Load();
        Text = "Sparring";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = LauncherWindowSizing.MinimumSize;
        Size = InitialWindowSize();
        BackColor = Color.FromArgb(5, 7, 5);
        ForeColor = Color.FromArgb(128, 218, 93);

        BuildLayout();
        LoadCatalog();
    }

    internal async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            var currentVersion = CurrentVersion();
            var checker = new GitHubReleaseUpdateChecker();
            var update = await checker.CheckLatestAsync(currentVersion, _settings.SkippedUpdateVersion);
            if (update is null)
            {
                return;
            }

            var setupAsset = update.FindSetupAsset();
            if (setupAsset is null)
            {
                return;
            }

            var choice = ShowUpdatePrompt(update);
            if (choice == DialogResult.Cancel)
            {
                _settings = _settings with { SkippedUpdateVersion = update.TagName };
                _settingsStore.Save(_settings);
                return;
            }

            if (choice != DialogResult.Yes)
            {
                return;
            }

            _statusLabel.Text = $"Sparring {update.TagName} 설치 파일을 다운로드하는 중입니다...";
            var installerPath = await DownloadUpdateInstallerAsync(setupAsset);
            _statusLabel.Text = "업데이트 설치 파일을 실행합니다.";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });
            Close();
        }
        catch
        {
            // Update checks must never block normal play.
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mapPreviewImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildLayout()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14, 14, 14, 10)
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var title = new Label
        {
            Text = "Sparring 연습 런처",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(166, 255, 126),
            Margin = new Padding(4, 0, 4, 2)
        };

        var subtitle = new Label
        {
            Text = "Sparring 내장 봇/맵으로 로컬 1.16.1 + BWAPI 스파링을 준비합니다.",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 10),
            ForeColor = Color.FromArgb(128, 218, 93),
            Margin = new Padding(6, 0, 4, 12)
        };

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.Normal,
            Margin = new Padding(0)
        };
        _tabs.TabPages.Add(BuildPracticeTab());
        _tabs.TabPages.Add(BuildSettingsTab());
        _tabs.TabPages.Add(BuildHotkeyTab());
        _tabs.TabPages.Add(BuildHistoryTab());

        _statusLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(128, 218, 93),
            Margin = new Padding(4, 6, 4, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };

        shell.Controls.Add(title, 0, 0);
        shell.Controls.Add(subtitle, 0, 1);
        shell.Controls.Add(_tabs, 0, 2);
        shell.Controls.Add(_statusLabel, 0, 3);
        Controls.Add(shell);
    }

    private static Size InitialWindowSize()
    {
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        return LauncherWindowSizing.InitialSize(workingArea);
    }

    private string CurrentVersion()
    {
        var versionPath = Path.Combine(_paths.RepositoryRoot, "VERSION");
        return File.Exists(versionPath)
            ? File.ReadAllText(versionPath).Trim()
            : Application.ProductVersion;
    }

    private DialogResult ShowUpdatePrompt(ReleaseUpdateInfo update)
    {
        return MessageBox.Show(
            this,
            $"새 버전 {update.TagName}이 있습니다.\r\n\r\n" +
            "예: 지금 다운로드 후 설치\r\n" +
            "아니요: 나중에\r\n" +
            "취소: 이번 버전 건너뛰기",
            "Sparring 업데이트",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Information);
    }

    private static async Task<string> DownloadUpdateInstallerAsync(ReleaseAssetInfo asset)
    {
        var updateDirectory = Path.Combine(Path.GetTempPath(), "Sparring", "updates");
        Directory.CreateDirectory(updateDirectory);
        var installerPath = Path.Combine(updateDirectory, asset.Name);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Sparring/1.0");
        await using var source = await client.GetStreamAsync(asset.DownloadUrl);
        await using var target = File.Create(installerPath);
        await source.CopyToAsync(target);
        return installerPath;
    }

    private void LayoutShell()
    {
        // The shell is table-driven. This method remains for older smoke/tests that call it via resize.
    }

    private TabPage BuildPracticeTab()
    {
        var page = CreateTabPage("게임");
        page.AutoScroll = true;

        _modeCombo = CreateCombo(84, 22, 132);
        _modeCombo.DataSource = new[] { "스파링", "래더" };
        SelectComboValue(_modeCombo, _settings.LastMode ?? "스파링");
        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            UpdateModeControls();
            ApplyBotFilters();
            SaveLastGameSelections();
        };

        _enemyRaceFilter = CreateCombo(84, 66, 132);
        _enemyRaceFilter.DataSource = new[] { "모두", "테란", "저그", "프로토스", "랜덤" };
        SelectComboValue(_enemyRaceFilter, _settings.LastEnemyRace ?? "모두");
        _enemyRaceFilter.SelectedIndexChanged += (_, _) =>
        {
            ApplyBotFilters();
            SaveLastGameSelections();
        };

        _sortCombo = CreateCombo(84, 110, 132);
        _sortCombo.DataSource = new[] { "ELO 높은순", "ELO 낮은순", "이름순" };
        SelectComboValue(_sortCombo, _settings.LastSort ?? "ELO 높은순");
        _sortCombo.SelectedIndexChanged += (_, _) =>
        {
            ApplyBotFilters();
            SaveLastGameSelections();
        };

        _searchBox = CreateTextBox(236, 66, 244, 28);
        _searchBox.PlaceholderText = "상대 이름 검색";
        _searchBox.TextChanged += (_, _) => ApplyBotFilters();

        _botList = CreateListBox(16, 156, 332, 216);
        _botList.Name = "BotList";
        _botList.SelectedIndexChanged += (_, _) =>
        {
            OnBotChanged();
            SaveLastGameSelections();
        };

        _mapList = CreateListBox(16, 424, 332, 156);
        _mapList.Name = "MapList";
        _mapList.SelectedIndexChanged += (_, _) =>
        {
            if (!_updatingSelections && !IsLadderMode)
            {
                ApplyBotFilters();
            }

            UpdateDetails();
            SaveLastGameSelections();
        };

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
        _playerRaceCombo.SelectedItem = _settings.LastPlayerRace ?? StarCraftRace.Protoss;
        _playerRaceCombo.SelectedIndexChanged += (_, _) =>
        {
            UpdateDetails();
            SaveLastGameSelections();
        };

        _buildCombo = CreateCombo(780, 66, 240);
        _buildCombo.DataSource = new[] { "봇 기본 빌드 (자동)", "랜덤 빌드 (지원 봇만)" };
        _buildCombo.Enabled = false;

        var rating = _ratingStore.Load();
        _ladderRatingLabel = CreateLabel($"현재 MMR: {rating.PlayerRating}", 724, 114);
        _ladderRatingLabel.AutoSize = false;
        _ladderRatingLabel.Size = new Size(124, 28);
        _ladderRatingText = CreateTextBox(852, 108, 70, 28);
        _ladderRatingText.Text = rating.PlayerRating.ToString();
        _applyRatingButton = CreateButton("적용", 932, 106, 62, 32);
        _applyRatingButton.Click += (_, _) => ApplyRatingFromUi();
        _resetRatingButton = CreateButton("1500 초기화", 1004, 106, 98, 32);
        _resetRatingButton.Click += (_, _) => ResetRatingFromUi();

        var refreshButton = CreateButton("새로고침", 1014, 20, 88, 32);
        refreshButton.Click += (_, _) => LoadCatalog();

        _launchButton = CreateButton("스파링 시작", 922, 528, 220, 42);
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

        _mapPreviewLabel = CreateLabel("맵 미리보기", 372, 202);
        _mapPreviewBox = new PictureBox
        {
            Name = "MapPreviewBox",
            Location = new Point(372, 224),
            Size = new Size(260, 260),
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        _detailsText = CreateTextBox(650, 214, 452, 296);
        _detailsText.Multiline = true;
        _detailsText.ReadOnly = true;
        _detailsText.ScrollBars = ScrollBars.Vertical;

        var runtimeText = CreateTextBox(672, 150, 430, 48);
        runtimeText.Multiline = true;
        runtimeText.ReadOnly = true;
        runtimeText.Text = string.Join(Environment.NewLine, new[]
        {
            $"사람: {_paths.PlayerRuntimeRoot}",
            $"AI: {_paths.AiRuntimeRoot}",
            "Sparring 내장 자산 사용"
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
        var playerRaceLabel = CreateLabel("내 종족", 724, 26);
        page.Controls.Add(playerRaceLabel);
        page.Controls.Add(_playerRaceCombo);
        var buildLabel = CreateLabel("빌드", 724, 70);
        page.Controls.Add(buildLabel);
        page.Controls.Add(_buildCombo);
        page.Controls.Add(_ladderRatingLabel);
        page.Controls.Add(_ladderRatingText);
        page.Controls.Add(_applyRatingButton);
        page.Controls.Add(_resetRatingButton);
        page.Controls.Add(refreshButton);
        page.Controls.Add(_difficultyLabel);
        page.Controls.Add(runtimeText);
        page.Controls.Add(_mapPreviewLabel);
        page.Controls.Add(_mapPreviewBox);
        page.Controls.Add(_detailsText);
        page.Controls.Add(_launchButton);
        refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        playerRaceLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buildLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _playerRaceCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _buildCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _ladderRatingLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _ladderRatingText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _applyRatingButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _resetRatingButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        runtimeText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _detailsText.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _launchButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        UpdateModeControls();
        page.Resize += (_, _) => LayoutPracticeTab(page);
        LayoutPracticeTab(page);

        return page;
    }

    private void LayoutPracticeTab(TabPage page)
    {
        if (_botList is null || _mapList is null || _detailsText is null || page.ClientSize.Width <= 0)
        {
            return;
        }

        var pad = 16;
        var gap = 16;
        var width = Math.Max(900, page.ClientSize.Width - pad);
        var height = Math.Max(560, page.ClientSize.Height);
        var compact = width < 1100;

        var leftWidth = compact ? 318 : 340;
        var middleWidth = compact ? Math.Min(248, Math.Max(206, (width - leftWidth - (pad * 2) - (gap * 2)) / 2)) : 260;
        var rightX = compact
            ? pad + leftWidth + gap
            : pad + leftWidth + gap + middleWidth + gap;
        var rightWidth = Math.Max(320, width - rightX - pad);
        var middleX = compact ? rightX : pad + leftWidth + gap;

        var modeLabel = FindLabel(page, "모드");
        var enemyLabel = FindLabel(page, "상대 종족");
        var sortLabel = FindLabel(page, "정렬");
        var botLabel = _botListLabel;
        var mapLabel = FindLabel(page, "맵 선택");
        var raceLabel = FindLabel(page, "내 종족");
        var buildLabel = FindLabel(page, "빌드");
        var refreshButton = FindButton(page, "새로고침");
        var runtimeText = FindRuntimeTextBox(page);

        modeLabel?.SetBounds(pad, 24, 64, 24);
        _modeCombo.SetBounds(pad + 84, 18, Math.Min(132, leftWidth - 90), 30);
        enemyLabel?.SetBounds(pad, 68, 74, 24);
        _enemyRaceFilter.SetBounds(pad + 84, 62, Math.Min(132, leftWidth - 90), 30);
        _searchBox.SetBounds(pad + 84, 98, leftWidth - 84, 30);
        sortLabel?.SetBounds(pad, 148, 64, 24);
        _sortCombo.SetBounds(pad + 84, 142, Math.Min(132, leftWidth - 90), 30);

        botLabel?.SetBounds(pad, 178, leftWidth, 24);
        var botHeight = compact ? Math.Max(160, Math.Min(260, height / 3)) : Math.Max(210, Math.Min(300, height - 500));
        _botList.SetBounds(pad, 204, leftWidth, botHeight);
        mapLabel?.SetBounds(pad, _botList.Bottom + 22, leftWidth, 24);
        _mapList.SetBounds(pad, _botList.Bottom + 48, leftWidth, Math.Max(142, height - _botList.Bottom - 96));

        var topControlX = rightX;
        var topControlWidth = Math.Max(280, rightWidth - 104);
        raceLabel?.SetBounds(topControlX, 24, 76, 24);
        _playerRaceCombo.SetBounds(topControlX + 86, 18, Math.Min(150, topControlWidth - 92), 30);
        buildLabel?.SetBounds(topControlX, 68, 76, 24);
        _buildCombo.SetBounds(topControlX + 86, 62, Math.Min(260, topControlWidth - 92), 30);
        refreshButton?.SetBounds(width - pad - 124, 18, 124, 34);

        _ladderRatingLabel.SetBounds(topControlX, 112, 118, 28);
        _ladderRatingText.SetBounds(topControlX + 126, 106, 70, 30);
        _applyRatingButton.SetBounds(topControlX + 206, 104, 80, 34);
        _resetRatingButton.SetBounds(topControlX + 296, 104, 160, 34);
        runtimeText?.SetBounds(topControlX, 150, Math.Max(320, rightWidth), 52);

        _difficultyLabel.SetBounds(middleX, compact ? 212 : 112, middleWidth, 74);
        _mapPreviewLabel.SetBounds(middleX, _difficultyLabel.Bottom + 10, middleWidth, 24);
        var previewSize = Math.Min(compact ? 232 : 260, Math.Max(170, middleWidth));
        _mapPreviewBox.SetBounds(middleX, _mapPreviewLabel.Bottom + 4, previewSize, previewSize);

        var detailsX = compact ? middleX + previewSize + gap : rightX;
        var detailsY = compact ? _mapPreviewBox.Top : 212;
        var detailsWidth = compact
            ? width - detailsX - pad
            : rightWidth;
        if (compact && detailsWidth < 300)
        {
            detailsX = middleX;
            detailsY = _mapPreviewBox.Bottom + gap;
            detailsWidth = rightWidth;
        }

        _detailsText.SetBounds(detailsX, detailsY, Math.Max(300, detailsWidth), Math.Max(170, height - detailsY - 72));
        const int launchButtonWidth = 220;
        _launchButton.SetBounds(width - pad - launchButtonWidth, height - 58, launchButtonWidth, 44);
        page.AutoScrollMinSize = new Size(Math.Max(900, _detailsText.Right + pad), Math.Max(620, _launchButton.Bottom + 8));
    }

    private TabPage BuildSettingsTab()
    {
        var page = CreateTabPage("설정");
        page.AutoScroll = true;
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

        page.Controls.Add(CreateLabel("래더맵 폴더", 18, 128));
        _ladderMapRootText = CreateTextBox(160, 124, 760, 28);
        _ladderMapRootText.Text = string.IsNullOrWhiteSpace(_settings.LadderMapRoot)
            ? RemasteredLadderMapCatalogReader.DefaultDirectory()
            : _settings.LadderMapRoot;
        var browseLadderMapButton = CreateButton("찾기", 934, 122, 72, 32);
        browseLadderMapButton.Click += (_, _) => BrowseFolderInto(_ladderMapRootText);

        _hideAiNameCheck = new CheckBox
        {
            Text = "AI 이름 가리기",
            Location = new Point(160, 170),
            Size = new Size(180, 28),
            Checked = _settings.EffectiveHideAiName,
            BackColor = Color.FromArgb(5, 7, 5),
            ForeColor = Color.FromArgb(166, 255, 126)
        };

        page.Controls.Add(CreateLabel("게임 속도", 18, 218));
        _gameSpeedCombo = CreateCombo(160, 212, 220);
        _gameSpeedCombo.DataSource = GameSpeedOptions;
        _gameSpeedCombo.DisplayMember = nameof(GameSpeedOption.Label);
        SelectGameSpeedOption(_settings.GameSpeedOverrideMs);

        page.Controls.Add(CreateLabel("마우스 스크롤", 18, 266));
        _mouseScrollSpeedInput = CreateNumericInput(160, 260, 82, _settings.MouseScrollSpeed);
        page.Controls.Add(CreateLabel("키보드 스크롤", 276, 266));
        _keyboardScrollSpeedInput = CreateNumericInput(382, 260, 82, _settings.KeyboardScrollSpeed);

        var saveButton = CreateButton("설정 저장", 160, 314, 120, 34);
        saveButton.Click += (_, _) => SaveSettingsFromUi();

        page.Controls.Add(_replayRootText);
        page.Controls.Add(browseReplayButton);
        page.Controls.Add(_userMapRootText);
        page.Controls.Add(browseMapButton);
        page.Controls.Add(_ladderMapRootText);
        page.Controls.Add(browseLadderMapButton);
        page.Controls.Add(_hideAiNameCheck);
        page.Controls.Add(_gameSpeedCombo);
        page.Controls.Add(_mouseScrollSpeedInput);
        page.Controls.Add(_keyboardScrollSpeedInput);
        page.Controls.Add(saveButton);
        var note = CreateReadOnlyBlock(
            18,
            376,
            1040,
            158,
            "사람 StarCraft는 W-MODE 기반 테두리 없는 전체 창모드로 실행합니다.\r\nAI 클라이언트는 창모드, 음소거, 커서 클립 OFF로 별도 실행합니다.\r\n게임 속도는 BWAPI speed_override로 적용합니다. 기본값은 StarCraft 원래 속도를 사용합니다.\r\n마우스/키보드 스크롤 속도는 StarCraft 실행 중 임시 적용한 뒤 기존 설정으로 되돌립니다.\r\n사용자 맵과 Remastered 래더맵은 .scm/.scx 파일을 읽어 Sparring 런타임 maps\\Sparring 폴더로 복사합니다.");
        page.Controls.Add(note);
        _replayRootText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _userMapRootText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _ladderMapRootText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _gameSpeedCombo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _mouseScrollSpeedInput.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _keyboardScrollSpeedInput.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        browseReplayButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browseMapButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browseLadderMapButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        note.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        page.Resize += (_, _) => LayoutSettingsTab(page);
        LayoutSettingsTab(page);
        return page;
    }

    private void LayoutSettingsTab(TabPage page)
    {
        if (_replayRootText is null || _gameSpeedCombo is null || page.ClientSize.Width <= 0)
        {
            return;
        }

        var pad = 18;
        var labelWidth = 180;
        var gap = 12;
        var browseWidth = 90;
        var contentWidth = Math.Max(860, page.ClientSize.Width - (pad * 2));
        var inputX = pad + labelWidth + gap;
        var inputWidth = Math.Max(360, contentWidth - labelWidth - gap - browseWidth - gap);
        var browseX = inputX + inputWidth + gap;
        var browseButtons = FindButtons(page, "찾기")
            .OrderBy(button => button.Top)
            .ToArray();

        FindLabel(page, "리플레이 저장 루트")?.SetBounds(pad, 28, labelWidth, 24);
        _replayRootText.SetBounds(inputX, 24, inputWidth, 28);
        if (browseButtons.Length > 0)
        {
            browseButtons[0].SetBounds(browseX, 22, browseWidth, 32);
        }

        FindLabel(page, "사용자 맵 폴더")?.SetBounds(pad, 78, labelWidth, 24);
        _userMapRootText.SetBounds(inputX, 74, inputWidth, 28);
        if (browseButtons.Length > 1)
        {
            browseButtons[1].SetBounds(browseX, 72, browseWidth, 32);
        }

        FindLabel(page, "래더맵 폴더")?.SetBounds(pad, 128, labelWidth, 24);
        _ladderMapRootText.SetBounds(inputX, 124, inputWidth, 28);
        if (browseButtons.Length > 2)
        {
            browseButtons[2].SetBounds(browseX, 122, browseWidth, 32);
        }

        _hideAiNameCheck.SetBounds(inputX, 170, 220, 28);
        FindLabel(page, "게임 속도")?.SetBounds(pad, 218, labelWidth, 24);
        _gameSpeedCombo.SetBounds(inputX, 212, Math.Min(340, inputWidth), 30);

        FindLabel(page, "마우스 스크롤")?.SetBounds(pad, 266, labelWidth, 24);
        _mouseScrollSpeedInput.SetBounds(inputX, 260, 82, 28);
        FindLabel(page, "키보드 스크롤")?.SetBounds(inputX + 124, 266, 110, 24);
        _keyboardScrollSpeedInput.SetBounds(inputX + 232, 260, 82, 28);
        FindButton(page, "설정 저장")?.SetBounds(inputX, 314, 132, 34);

        foreach (var box in page.Controls.OfType<TextBox>().Where(box => box.Multiline && box.ReadOnly))
        {
            if (box.Text.StartsWith("사람 StarCraft", StringComparison.Ordinal))
            {
                box.SetBounds(pad, 376, contentWidth, 166);
            }
        }

        page.AutoScrollMinSize = new Size(contentWidth + (pad * 2), 590);
    }

    private TabPage BuildHotkeyTab()
    {
        var page = CreateTabPage("Hotkeys");
        page.AutoScroll = true;

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
        AddHotkeyFilterButtons(page, _hotkeyRaceFilter, _hotkeyRaceButtons, 342, 16, 80, 36);

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

        var importButton = CreateButton("기본값", 752, 18, 110, 32);
        importButton.Click += (_, _) => ImportHotkeys();
        var importBattleNetButton = CreateButton("배틀넷", 872, 18, 110, 32);
        importBattleNetButton.Click += (_, _) => ImportRemasteredHotkeysFromDetectedLocation();
        var importFolderButton = CreateButton("폴더", 992, 18, 112, 32);
        importFolderButton.Click += (_, _) => ImportRemasteredHotkeysFromFolder();
        var saveButton = CreateButton("CSV", 872, 96, 110, 32);
        saveButton.Click += (_, _) => SaveHotkeys(applyMpq: false);
        var applyButton = CreateButton("반영", 992, 96, 112, 32);
        applyButton.Click += (_, _) => SaveHotkeys(applyMpq: true);
        page.Controls.Add(importButton);
        page.Controls.Add(importBattleNetButton);
        page.Controls.Add(importFolderButton);
        page.Controls.Add(saveButton);
        page.Controls.Add(applyButton);
        importButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        importBattleNetButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        importFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        saveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        applyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _hotkeyCountLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 96),
            Size = new Size(680, 20),
            ForeColor = Color.FromArgb(128, 218, 93),
            Text = "명령 0개"
        };
        page.Controls.Add(_hotkeyCountLabel);

        page.Controls.Add(CreateLabel("카드", 282, 98));
        _hotkeyPageFilter = CreateCombo(-1000, -1000, 128);
        _hotkeyPageFilter.Items.AddRange([
            HotkeyCommandLayout.PageGeneral,
            HotkeyCommandLayout.PageBasicStructures,
            HotkeyCommandLayout.PageAdvancedStructures,
            HotkeyCommandLayout.PageAll
        ]);
        _hotkeyPageFilter.SelectedIndex = 0;
        _hotkeyPageFilter.SelectedIndexChanged += (_, _) =>
        {
            RefreshHotkeyFilterButtons();
            RefreshHotkeyTiles();
        };
        AddHotkeyFilterButtons(page, _hotkeyPageFilter, _hotkeyPageButtons, 328, 92, 100, 30);

        page.Controls.Add(CreateLabel("선택 항목", 18, 120));
        _hotkeyObjectList = CreateListBox(18, 142, 246, 428);
        _hotkeyObjectList.Font = new Font("Segoe UI", 10);
        _hotkeyObjectList.DrawMode = DrawMode.OwnerDrawFixed;
        _hotkeyObjectList.ItemHeight = 52;
        _hotkeyObjectList.DrawItem += DrawHotkeyObjectItem;
        _hotkeyObjectList.SelectedIndexChanged += (_, _) => RefreshHotkeyTiles();
        page.Controls.Add(_hotkeyObjectList);

        page.Controls.Add(CreateLabel("명령", 282, 120));
        _hotkeyCommandPanel = new TableLayoutPanel
        {
            Location = new Point(282, 142),
            Size = new Size(318, 318),
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            ColumnCount = 3,
            RowCount = 3,
            Padding = new Padding(5)
        };
        for (var index = 0; index < 3; index++)
        {
            _hotkeyCommandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            _hotkeyCommandPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
        }

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
        _hotkeyCommandTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _hotkeyCommandMeta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _hotkeyDefaultText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

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

        _hotkeyHelpText = CreateReadOnlyBlock(
            730,
            370,
            366,
            142,
            "선택한 항목은 3x3 게임 명령 카드로 표시합니다.\r\nSparring 기본값은 내장 CSV를 사람 런타임으로 복사합니다.\r\nBattle.net/폴더 지정은 Remastered STR_* 핫키를 현재 작업 CSV에 반영합니다.\r\nCSV 저장은 작업 파일만 갱신합니다.\r\n런타임 반영은 사람 런타임 patch_rt.mpq에만 적용합니다.");
        _hotkeyRuntimeHelpText = CreateReadOnlyBlock(
            18,
            580,
            1078,
            38,
            "봇/AI 런타임에는 사람 핫키를 반영하지 않습니다. 사람 런타임만 커스텀 핫키를 사용합니다.");
        page.Controls.Add(_hotkeyHelpText);
        page.Controls.Add(_hotkeyRuntimeHelpText);
        _hotkeyHelpText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _hotkeyRuntimeHelpText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        LoadHotkeys();
        page.Resize += (_, _) => LayoutHotkeyTab(page);
        LayoutHotkeyTab(page);
        return page;
    }

    private void LayoutHotkeyTab(TabPage page)
    {
        if (_hotkeyObjectList is null || _hotkeyCommandPanel is null || page.ClientSize.Width <= 0)
        {
            return;
        }

        var pad = 18;
        var gap = 16;
        var width = Math.Max(980, page.ClientSize.Width - pad);
        var height = Math.Max(640, page.ClientSize.Height);
        var leftWidth = width < 1160 ? 274 : 304;
        var commandX = pad + leftWidth + gap;
        var availableForDetails = width - commandX - pad;
        var compact = width < 1250;
        var commandSize = Math.Min(360, Math.Max(300, availableForDetails - 396));
        var detailsX = commandX + commandSize + gap;
        var detailsWidth = width - detailsX - pad;
        var detailsBelow = detailsWidth < 320;
        if (detailsBelow)
        {
            commandSize = Math.Min(330, Math.Max(286, availableForDetails));
            detailsX = commandX;
            detailsWidth = Math.Max(360, width - detailsX - pad);
        }

        const int hotkeyFilterLabelWidth = 88;
        const int hotkeyFilterControlOffset = hotkeyFilterLabelWidth + 4;
        FindLabel(page, "검색")?.SetBounds(pad, 26, hotkeyFilterLabelWidth, 24);
        _hotkeySearch.SetBounds(pad + hotkeyFilterControlOffset, 20, Math.Max(160, leftWidth - hotkeyFilterControlOffset - 8), 30);
        FindLabel(page, "종족")?.SetBounds(commandX, 24, hotkeyFilterLabelWidth, 24);
        LayoutButtonGroup(_hotkeyRaceButtons, commandX + hotkeyFilterControlOffset, 16, 80, 36);
        FindLabel(page, "분류")?.SetBounds(commandX, 66, hotkeyFilterLabelWidth, 24);
        var categoryBottom = compact
            ? LayoutButtonWrap(_hotkeyCategoryButtons, commandX + hotkeyFilterControlOffset, 58, width - pad, 82, 30, 6)
            : LayoutButtonGroup(_hotkeyCategoryButtons, commandX + hotkeyFilterControlOffset, 58, 82, 30);

        var cardY = compact ? categoryBottom + 8 : 92;
        FindLabel(page, "카드")?.SetBounds(commandX, cardY + 6, hotkeyFilterLabelWidth, 24);
        var pageBottom = compact
            ? LayoutButtonWrap(_hotkeyPageButtons, commandX + hotkeyFilterControlOffset, cardY, width - pad, 100, 30, 6)
            : LayoutButtonGroup(_hotkeyPageButtons, commandX + hotkeyFilterControlOffset, 92, 100, 30);

        var actionBottom = 128;
        if (compact)
        {
            actionBottom = LayoutButtonWrap(
                [
                    (FindButton(page, "기본값"), 110),
                    (FindButton(page, "배틀넷"), 110),
                    (FindButton(page, "폴더"), 110),
                    (FindButton(page, "CSV"), 110),
                    (FindButton(page, "반영"), 110)
                ],
                commandX,
                pageBottom + 8,
                width - pad,
                32,
                8);
        }
        else
        {
            FindButton(page, "기본값")?.SetBounds(width - 346, 18, 110, 32);
            FindButton(page, "배틀넷")?.SetBounds(width - 228, 18, 110, 32);
            FindButton(page, "폴더")?.SetBounds(width - 110, 18, 110, 32);
            FindButton(page, "CSV")?.SetBounds(width - 228, 96, 110, 32);
            FindButton(page, "반영")?.SetBounds(width - 110, 96, 110, 32);
        }

        _hotkeyCountLabel.SetBounds(pad, 98, leftWidth, 22);

        var contentTop = compact ? Math.Max(180, actionBottom + 36) : 154;
        FindLabel(page, "선택 항목")?.SetBounds(pad, contentTop - 26, leftWidth, 24);
        _hotkeyObjectList.SetBounds(pad, contentTop, leftWidth, Math.Max(260, height - contentTop - 118));
        FindLabel(page, "명령")?.SetBounds(commandX, contentTop - 26, commandSize, 24);
        _hotkeyCommandPanel.SetBounds(commandX, contentTop, commandSize, commandSize);

        var detailTop = detailsBelow
            ? _hotkeyCommandPanel.Bottom + gap
            : compact ? contentTop - 8 : 128;
        _hotkeyCommandTitle.SetBounds(detailsX, detailTop, detailsWidth, 60);
        _hotkeyCommandMeta.SetBounds(detailsX, _hotkeyCommandTitle.Bottom + 4, detailsWidth, 54);
        _hotkeyDefaultText.SetBounds(detailsX, _hotkeyCommandMeta.Bottom + 4, detailsWidth, 56);
        FindLabel(page, "현재 키")?.SetBounds(detailsX, _hotkeyDefaultText.Bottom + 20, 64, 24);
        _hotkeyKeyText.SetBounds(detailsX + 64, _hotkeyDefaultText.Bottom + 14, 84, 30);
        FindButton(page, "키 적용")?.SetBounds(detailsX + 162, _hotkeyDefaultText.Bottom + 10, 110, 36);

        _hotkeyHelpText.SetBounds(detailsX, _hotkeyDefaultText.Bottom + 72, detailsWidth, 142);

        var runtimeHelpTop = Math.Max(_hotkeyObjectList.Bottom + 36, _hotkeyCommandPanel.Bottom + 36);
        runtimeHelpTop = Math.Max(runtimeHelpTop, _hotkeyHelpText.Bottom + 36);
        _hotkeyRuntimeHelpText.SetBounds(pad, runtimeHelpTop, width - (pad * 2), 42);

        var scrollWidth = Math.Max(980, Math.Max(width, detailsX + detailsWidth + pad));
        var scrollHeight = Math.Max(680, page.Controls.Cast<Control>().Max(control => control.Bottom) + 20);
        page.AutoScrollMinSize = ShouldUseCompactScroll(page, 680, 430)
            ? new Size(scrollWidth, scrollHeight)
            : Size.Empty;
    }

    private bool ShouldUseCompactScroll(Control page, int visibleWidthThreshold, int visibleHeightThreshold)
    {
        var dpi = Math.Max(96, DeviceDpi);
        var visibleWidth = page.ClientSize.Width * 96 / dpi;
        var visibleHeight = page.ClientSize.Height * 96 / dpi;
        return visibleWidth < visibleWidthThreshold || visibleHeight < visibleHeightThreshold;
    }

    private static int LayoutButtonGroup(IReadOnlyList<Button> buttons, int x, int y, int buttonWidth, int buttonHeight)
    {
        for (var index = 0; index < buttons.Count; index++)
        {
            buttons[index].SetBounds(x + index * (buttonWidth + 6), y, buttonWidth, buttonHeight);
        }

        return y + buttonHeight;
    }

    private static int LayoutButtonWrap(
        IReadOnlyList<Button> buttons,
        int x,
        int y,
        int maxRight,
        int buttonWidth,
        int buttonHeight,
        int gap)
    {
        var currentX = x;
        var currentY = y;
        var bottom = y + buttonHeight;
        foreach (var button in buttons)
        {
            if (currentX > x && currentX + buttonWidth > maxRight)
            {
                currentX = x;
                currentY += buttonHeight + gap;
            }

            button.SetBounds(currentX, currentY, buttonWidth, buttonHeight);
            bottom = Math.Max(bottom, currentY + buttonHeight);
            currentX += buttonWidth + gap;
        }

        return bottom;
    }

    private static int LayoutButtonWrap(
        IReadOnlyList<(Button? Button, int Width)> buttons,
        int x,
        int y,
        int maxRight,
        int buttonHeight,
        int gap)
    {
        var currentX = x;
        var currentY = y;
        var bottom = y + buttonHeight;
        foreach (var (button, width) in buttons)
        {
            if (button is null)
            {
                continue;
            }

            if (currentX > x && currentX + width > maxRight)
            {
                currentX = x;
                currentY += buttonHeight + gap;
            }

            button.SetBounds(currentX, currentY, width, buttonHeight);
            bottom = Math.Max(bottom, currentY + buttonHeight);
            currentX += width + gap;
        }

        return bottom;
    }

    private TabPage BuildHistoryTab()
    {
        var page = CreateTabPage("전적");
        page.AutoScroll = true;
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
            GridColor = Color.FromArgb(45, 70, 45),
            DataSource = _historySource,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            BorderStyle = BorderStyle.FixedSingle,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            ColumnHeadersHeight = 30,
            RowTemplate = { Height = 30 },
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        StyleHistoryGrid(_historyGrid);
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.StartedLocalText), "시작", 130));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.ModeText), "모드", 70));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.OutcomeText), "결과", 64));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.BotName), "AI", 140));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.MapName), "맵", 180, DataGridViewAutoSizeColumnMode.Fill));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.MatchupText), "종족", 120));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.ActionsPerMinute), "APM", 64));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.ActionCount), "액션", 72));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.DurationText), "시간", 72));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.RatingText), "래더 점수", 116));
        _historyGrid.Columns.Add(CreateHistoryTextColumn(nameof(PracticeSessionRecord.ResultSourceText), "판정 근거", 132));

        page.Controls.Add(refreshButton);
        page.Controls.Add(_historyGrid);
        _historyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        page.Resize += (_, _) => LayoutHistoryTab(page);
        LayoutHistoryTab(page);
        RefreshHistory();
        return page;
    }

    private void LayoutHistoryTab(TabPage page)
    {
        if (_historyGrid is null || page.ClientSize.Width <= 0)
        {
            return;
        }

        var pad = 18;
        FindButton(page, "새로고침")?.SetBounds(pad, 18, 124, 34);
        _historyGrid.SetBounds(
            pad,
            64,
            Math.Max(760, page.ClientSize.Width - (pad * 2)),
            Math.Max(420, page.ClientSize.Height - 88));
        page.AutoScrollMinSize = new Size(820, 540);
    }

    private static DataGridViewTextBoxColumn CreateHistoryTextColumn(
        string propertyName,
        string headerText,
        int width,
        DataGridViewAutoSizeColumnMode autoSizeMode = DataGridViewAutoSizeColumnMode.None)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            Width = width,
            AutoSizeMode = autoSizeMode,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
    }

    private static void StyleHistoryGrid(DataGridView grid)
    {
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(16, 34, 18),
            ForeColor = Color.FromArgb(168, 255, 120),
            SelectionBackColor = Color.FromArgb(16, 34, 18),
            SelectionForeColor = Color.FromArgb(168, 255, 120),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(4, 0, 4, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(3, 6, 4),
            ForeColor = Color.FromArgb(214, 239, 206),
            SelectionBackColor = Color.FromArgb(36, 92, 42),
            SelectionForeColor = Color.White,
            Font = new Font("Segoe UI", 9F),
            Padding = new Padding(4, 0, 4, 0)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(8, 13, 9),
            ForeColor = Color.FromArgb(214, 239, 206),
            SelectionBackColor = Color.FromArgb(36, 92, 42),
            SelectionForeColor = Color.White,
            Font = new Font("Segoe UI", 9F),
            Padding = new Padding(4, 0, 4, 0)
        };
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
            var baseCatalog = PracticeAssetCatalogReader.Read(_paths);
            var userMaps = UserMapCatalogReader.ReadDirectory(_settings.UserMapRoot);
            var ladderMapRoot = string.IsNullOrWhiteSpace(_settings.LadderMapRoot)
                ? RemasteredLadderMapCatalogReader.DefaultDirectory()
                : _settings.LadderMapRoot;
            var ladderMaps = RemasteredLadderMapCatalogReader.ReadDirectory(ladderMapRoot, baseCatalog);
            _catalog = UserMapCatalogReader.Merge(
                UserMapCatalogReader.Merge(baseCatalog, ladderMaps),
                userMaps);
            _statusLabel.Text = $"Sparring 카탈로그 로드 완료: 봇 {_catalog.Bots.Count}개, 맵 {_catalog.Maps.Count}개 (래더맵 {ladderMaps.Count}개, 사용자 맵 {userMaps.Count}개)";
            RefreshRatingUi();
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
        var selectedSpeed = _gameSpeedCombo.SelectedItem as GameSpeedOption ?? GameSpeedOptions[0];
        _settings = _settings with
        {
            ReplayRoot = string.IsNullOrWhiteSpace(_replayRootText.Text)
                ? PracticeRuntimeOptions.Defaults().ReplayRoot
                : _replayRootText.Text.Trim(),
            UserMapRoot = _userMapRootText.Text.Trim(),
            LadderMapRoot = _ladderMapRootText.Text.Trim(),
            HideAiName = _hideAiNameCheck.Checked,
            UseBotNameAsAiCharacter = null,
            GameSpeedOverrideMs = selectedSpeed.SpeedOverrideMs,
            MouseScrollSpeed = (int)_mouseScrollSpeedInput.Value,
            KeyboardScrollSpeed = (int)_keyboardScrollSpeedInput.Value
        };
        _settingsStore.Save(_settings);
        LoadCatalog();
        MessageBox.Show(this, "설정을 저장했습니다.", "설정");
    }

    private void ApplyRatingFromUi()
    {
        if (!int.TryParse(_ladderRatingText.Text.Trim(), out var rating) || rating < 0)
        {
            MessageBox.Show(this, "래더 점수는 0 이상의 숫자로 입력해주세요.", "래더 점수", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _ratingStore.Save(rating);
        RefreshRatingUi();
        MessageBox.Show(this, "래더 점수를 저장했습니다.", "래더 점수");
    }

    private void ResetRatingFromUi()
    {
        _ratingStore.Reset();
        RefreshRatingUi();
        MessageBox.Show(this, "래더 점수를 1500으로 초기화했습니다.", "래더 점수");
    }

    private void RefreshRatingUi()
    {
        var rating = _ratingStore.Load();
        if (_ladderRatingLabel is not null)
        {
            _ladderRatingLabel.Text = $"현재 MMR: {rating.PlayerRating}";
        }

        if (_ladderRatingText is not null)
        {
            _ladderRatingText.Text = rating.PlayerRating.ToString();
        }
    }

    private PracticeRuntimeOptions CurrentRuntimeOptions()
    {
        return new PracticeRuntimeOptions(
            _settings.ReplayRoot,
            _settings.GameSpeedOverrideMs,
            _settings.MouseScrollSpeed,
            _settings.KeyboardScrollSpeed);
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
            : PracticeAssetPaths.DefaultHotkeyCsv(_paths);
        var messages = PracticeAssetPaths.Messages(_paths);
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
        var objects = FilterHotkeyObjectEntries()
            .GroupBy(entry => HotkeyCommandLayout.ObjectFor(entry))
            .OrderBy(group => group.Key.CategoryRank)
            .ThenBy(group => group.Key.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new HotkeyObjectItem(
                group.Key,
                group.Count(),
                group.OrderBy(entry => entry.StringId).First()))
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
        var entries = FilterHotkeyTileEntries(selectedObject)
            .OrderBy(entry => HotkeyCommandLayout.Slot(entry) < 0 ? 99 : HotkeyCommandLayout.Slot(entry))
            .ThenBy(entry => HotkeyCommandRank(entry))
            .ThenBy(entry => entry.StringId)
            .ToList();

        _hotkeyCommandPanel.SuspendLayout();
        _hotkeyCommandPanel.Controls.Clear();
        var slots = BuildHotkeyCommandSlots(entries);
        for (var index = 0; index < slots.Length; index++)
        {
            var control = slots[index] is { } entry
                ? CreateHotkeyTile(entry)
                : CreateHotkeyEmptySlot();
            _hotkeyCommandPanel.Controls.Add(control, index % 3, index / 3);
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

    private IEnumerable<HotkeyEntry> FilterHotkeyObjectEntries()
    {
        var query = _hotkeySearch?.Text.Trim() ?? string.Empty;
        var race = SelectedComboText(_hotkeyRaceFilter, "전체");
        var category = SelectedComboText(_hotkeyCategoryFilter, "전체");

        return _hotkeyEntries
            .Where(entry => HotkeyMatchesQuery(entry, query))
            .Where(entry =>
            {
                var info = HotkeyCommandLayout.ObjectFor(entry);
                return (race == "전체" || info.Race == race) &&
                       (category == "전체" || info.Category == category);
            });
    }

    private IEnumerable<HotkeyEntry> FilterHotkeyTileEntries(HotkeyCommandObjectInfo? selectedObject)
    {
        var query = _hotkeySearch?.Text.Trim() ?? string.Empty;
        var page = SelectedComboText(_hotkeyPageFilter, HotkeyCommandLayout.PageGeneral);
        var entries = _hotkeyEntries.Where(entry => HotkeyMatchesQuery(entry, query));

        if (selectedObject is not null)
        {
            entries = entries.Where(entry =>
            {
                var info = HotkeyCommandLayout.ObjectFor(entry);
                return info.Key == selectedObject.Key ||
                       selectedObject.IncludeCommonCommands && HotkeyCommandLayout.IsCommonCommand(entry);
            });
        }

        if (page != HotkeyCommandLayout.PageAll)
        {
            entries = entries.Where(entry => HotkeyCommandLayout.PageName(entry) == page);
        }

        return entries;
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
            var button = CreateButton(HotkeyFilterDisplayText(value), x + index * (buttonWidth + 6), y, buttonWidth, buttonHeight);
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

    private static string HotkeyFilterDisplayText(string value)
    {
        return value switch
        {
            "Terran" => "Terr",
            "Protoss" => "Prot",
            "Common" => "공통",
            "업그레이드" => "업글",
            HotkeyCommandLayout.PageBasicStructures => "기본",
            HotkeyCommandLayout.PageAdvancedStructures => "고급",
            _ => value
        };
    }

    private void RefreshHotkeyFilterButtons()
    {
        RefreshHotkeyFilterButtonGroup(_hotkeyRaceButtons, SelectedComboText(_hotkeyRaceFilter, "전체"));
        RefreshHotkeyFilterButtonGroup(_hotkeyCategoryButtons, SelectedComboText(_hotkeyCategoryFilter, "전체"));
        RefreshHotkeyFilterButtonGroup(_hotkeyPageButtons, SelectedComboText(_hotkeyPageFilter, HotkeyCommandLayout.PageGeneral));
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

    private static void SelectComboValue(ComboBox combo, string value)
    {
        for (var index = 0; index < combo.Items.Count; index++)
        {
            if (string.Equals(combo.Items[index]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = index;
                return;
            }
        }
    }

    private void SaveLastGameSelections()
    {
        if (_updatingSelections ||
            _modeCombo is null ||
            _enemyRaceFilter is null ||
            _sortCombo is null ||
            _playerRaceCombo is null ||
            _botList is null ||
            _mapList is null)
        {
            return;
        }

        _settings = _settings with
        {
            LastMode = SelectedComboText(_modeCombo, "스파링"),
            LastEnemyRace = SelectedComboText(_enemyRaceFilter, "모두"),
            LastSort = SelectedComboText(_sortCombo, "ELO 높은순"),
            LastPlayerRace = _playerRaceCombo.SelectedItem is StarCraftRace race ? race : StarCraftRace.Protoss,
            LastBotName = (_botList.SelectedItem as BotItem)?.Bot?.Name,
            LastMapName = (_mapList.SelectedItem as MapItem)?.Map?.Name
        };
        _settingsStore.Save(_settings);
    }

    private void SelectBotItem(List<BotItem> items, string? preferredName)
    {
        if (items.Count == 0)
        {
            return;
        }

        var selected = !string.IsNullOrWhiteSpace(preferredName)
            ? items.FirstOrDefault(item => string.Equals(item.Bot?.Name, preferredName, StringComparison.OrdinalIgnoreCase))
            : null;
        _botList.SelectedItem = selected ?? items.First();
    }

    private void SelectMapItem(List<MapItem> items, string? preferredName)
    {
        if (items.Count == 0)
        {
            return;
        }

        var selected = !string.IsNullOrWhiteSpace(preferredName)
            ? items.FirstOrDefault(item => string.Equals(item.Map?.Name, preferredName, StringComparison.OrdinalIgnoreCase))
            : null;
        _mapList.SelectedItem = selected ?? items.First();
    }

    private static HotkeyEntry?[] BuildHotkeyCommandSlots(IReadOnlyList<HotkeyEntry> entries)
    {
        var slots = new HotkeyEntry?[9];
        var overflow = new List<HotkeyEntry>();

        foreach (var entry in entries)
        {
            var slot = HotkeyCommandLayout.Slot(entry);
            if (slot >= 0 && slot < slots.Length && slots[slot] is null)
            {
                slots[slot] = entry;
            }
            else
            {
                overflow.Add(entry);
            }
        }

        foreach (var entry in overflow)
        {
            var emptyIndex = Array.FindIndex(slots, value => value is null);
            if (emptyIndex < 0)
            {
                break;
            }

            slots[emptyIndex] = entry;
        }

        return slots;
    }

    private Button CreateHotkeyTile(HotkeyEntry entry)
    {
        var key = string.IsNullOrWhiteSpace(entry.Hotkey) ? "-" : entry.Hotkey.ToUpperInvariant();
        var icon = LoadHotkeyIcon(entry);
        var tile = new Button
        {
            Tag = entry,
            Text = string.Empty,
            Dock = DockStyle.Fill,
            Margin = new Padding(3),
            BackColor = Color.FromArgb(4, 10, 5),
            ForeColor = Color.FromArgb(166, 255, 126),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F)
        };
        tile.FlatAppearance.BorderColor = Color.FromArgb(82, 150, 68);
        tile.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 44, 18);
        tile.Paint += (_, e) => PaintHotkeyTile(e.Graphics, tile.ClientRectangle, entry, key, icon);
        tile.Click += (_, _) => SelectHotkey(entry);
        _hotkeyToolTip.SetToolTip(tile, $"{entry.Description} ({key})");
        return tile;
    }

    private static Control CreateHotkeyEmptySlot()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3),
            BackColor = Color.FromArgb(2, 5, 3)
        };
        panel.Paint += (_, e) =>
        {
            using var border = new Pen(Color.FromArgb(24, 44, 24));
            var rect = panel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.DrawRectangle(border, rect);
        };
        return panel;
    }

    private static void PaintHotkeyTile(
        Graphics graphics,
        Rectangle bounds,
        HotkeyEntry entry,
        string key,
        Image? icon)
    {
        graphics.Clear(Color.FromArgb(4, 10, 5));
        var accent = HotkeyTileAccentColor(entry);
        var iconRect = new Rectangle(bounds.Left + 18, bounds.Top + 10, 44, 44);
        var keyRect = new Rectangle(bounds.Right - 34, bounds.Top + 8, 24, 22);
        var titleRect = new Rectangle(bounds.Left + 6, bounds.Bottom - 26, bounds.Width - 12, 20);

        if (icon is not null)
        {
            DrawHotkeyIcon(graphics, icon, iconRect);
        }
        else
        {
            using var fallbackBack = new SolidBrush(Color.FromArgb(12, 24, 12));
            using var fallbackBorder = new Pen(accent, 1.5F);
            graphics.FillRectangle(fallbackBack, iconRect);
            graphics.DrawRectangle(fallbackBorder, iconRect);
        }

        using var keyBack = new SolidBrush(Color.FromArgb(16, 27, 11));
        using var keyBorder = new Pen(accent, 1F);
        graphics.FillRectangle(keyBack, keyRect);
        graphics.DrawRectangle(keyBorder, keyRect);

        using var keyFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        TextRenderer.DrawText(
            graphics,
            key,
            keyFont,
            keyRect,
            Color.FromArgb(230, 255, 195),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        using var titleFont = new Font("Segoe UI", 7.5F, FontStyle.Regular);
        TextRenderer.DrawText(
            graphics,
            entry.Description,
            titleFont,
            titleRect,
            Color.FromArgb(207, 239, 198),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void DrawHotkeyObjectItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox listBox || e.Index >= listBox.Items.Count)
        {
            return;
        }

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        using var back = new SolidBrush(selected ? Color.FromArgb(16, 58, 22) : Color.Black);
        e.Graphics.FillRectangle(back, e.Bounds);

        if (listBox.Items[e.Index] is HotkeyObjectItem item)
        {
            var icon = LoadHotkeyIcon(item.PreviewEntry);
            var iconRect = new Rectangle(e.Bounds.Left + 7, e.Bounds.Top + 7, 38, 38);
            if (icon is not null)
            {
                DrawHotkeyIcon(e.Graphics, icon, iconRect);
            }
            else
            {
                using var border = new Pen(Color.FromArgb(70, 120, 60));
                e.Graphics.DrawRectangle(border, iconRect);
            }

            var titleRect = new Rectangle(e.Bounds.Left + 54, e.Bounds.Top + 6, e.Bounds.Width - 60, 22);
            var metaRect = new Rectangle(e.Bounds.Left + 54, e.Bounds.Top + 28, e.Bounds.Width - 60, 18);
            TextRenderer.DrawText(
                e.Graphics,
                item.Info.DisplayName,
                listBox.Font,
                titleRect,
                selected ? Color.White : Color.FromArgb(190, 255, 144),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            using var metaFont = new Font("Segoe UI", 8F);
            TextRenderer.DrawText(
                e.Graphics,
                $"{item.Info.Race} / {item.Info.Category} / {item.Count}",
                metaFont,
                metaRect,
                Color.FromArgb(128, 218, 93),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        e.DrawFocusRectangle();
    }

    private static void DrawHotkeyIcon(Graphics graphics, Image icon, Rectangle bounds)
    {
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        graphics.DrawImage(icon, bounds);
        using var border = new Pen(Color.FromArgb(82, 150, 68));
        graphics.DrawRectangle(border, bounds);
    }

    private Image? LoadHotkeyIcon(HotkeyEntry entry)
    {
        var iconRoot = PracticeAssetPaths.HotkeyIconRoot(_paths);
        foreach (var relativePath in HotkeyIconCandidates(entry))
        {
            if (_hotkeyIconCache.TryGetValue(relativePath, out var cached))
            {
                return cached;
            }

            var fullPath = Path.Combine(iconRoot, relativePath);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            try
            {
                using var stream = File.OpenRead(fullPath);
                using var source = Image.FromStream(stream);
                var image = new Bitmap(source);
                _hotkeyIconCache[relativePath] = image;
                return image;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static readonly IReadOnlyDictionary<string, string> HotkeyIconOverrides =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["general_cmd_move"] = Path.Combine("general", "move.bmp"),
            ["general_cmd_stop"] = Path.Combine("general", "stop.bmp"),
            ["general_cmd_attack"] = Path.Combine("general", "attack.bmp"),
            ["general_cmd_patrol"] = Path.Combine("general", "patrol.bmp"),
            ["general_cmd_holdpos"] = Path.Combine("general", "hold_position.bmp"),
            ["general_cmd_land"] = Path.Combine("terran", "land.jpg"),
            ["general_cmd_liftoff"] = Path.Combine("terran", "liftoff.jpg"),
            ["general_cmd_rally"] = Path.Combine("general", "set_rally_point.bmp"),
            ["general_cmd_gather"] = Path.Combine("general", "gather.bmp"),
            ["general_cmd_cargo"] = Path.Combine("general", "return_cargo.bmp"),
            ["general_cmd_load"] = Path.Combine("general", "load.bmp"),
            ["general_cmd_unload"] = Path.Combine("general", "unload.bmp"),

            ["terran_cmd_repair"] = Path.Combine("general", "repair.bmp"),
            ["terran_cmd_buildstruc"] = Path.Combine("terran", "cmd_scv_build.bmp"),
            ["terran_cmd_buildadvstruc"] = Path.Combine("terran", "cmd_scv_build_adv.bmp"),
            ["terran_cmd_armsilo"] = Path.Combine("terran", "cmd_armnukesilo.bmp"),
            ["terran_cmd_nuke"] = Path.Combine("terran", "cmd_nuclear_strike.bmp"),
            ["terran_cmd_heal"] = Path.Combine("terran", "heal.jpg"),
            ["terran_cmd_restoration"] = Path.Combine("terran", "restoration.jpg"),
            ["terran_cmd_optflare"] = Path.Combine("terran", "opticalflare.jpg"),
            ["terran_ab_usestim"] = Path.Combine("terran", "cmd_stim_pack.bmp"),
            ["terran_ab_lockdown"] = Path.Combine("terran", "cmd_lockdown.bmp"),
            ["terran_ab_spidermines"] = Path.Combine("terran", "spidermine.bmp"),
            ["terran_ab_scanner"] = Path.Combine("terran", "cmd_scanner_sweep.bmp"),
            ["terran_ab_siege"] = Path.Combine("terran", "siege_tank_siege_mode.bmp"),
            ["terran_ab_unsiege"] = Path.Combine("terran", "siege_tank_tank_mode.bmp"),
            ["terran_ab_defmatrix"] = Path.Combine("terran", "cmd_defensive_matrix.bmp"),
            ["terran_ab_emp"] = Path.Combine("terran", "cmd_emp.bmp"),
            ["terran_ab_irradiate"] = Path.Combine("terran", "cmd_irradiate.bmp"),
            ["terran_ab_yamato"] = Path.Combine("terran", "yamato_gun.bmp"),
            ["terran_ab_cloak"] = Path.Combine("terran", "cmd_cloak.bmp"),
            ["terran_ab_decloak"] = Path.Combine("terran", "cmd_decloak.bmp"),
            ["terran_train_vessel"] = Path.Combine("terran", "science_vessel.bmp"),
            ["terran_train_tank"] = Path.Combine("terran", "siege_tank_tank_mode.bmp"),

            ["protoss_cmd_inteceptor"] = Path.Combine("protoss", "interceptor.bmp"),
            ["protoss_cmd_scarab"] = Path.Combine("protoss", "make_scarab.bmp"),
            ["protoss_cmd_recharge"] = Path.Combine("protoss", "cmd_recharge_shields.bmp"),
            ["protoss_cmd_psistorm"] = Path.Combine("protoss", "psi_storm.bmp"),
            ["protoss_cmd_archonwarp"] = Path.Combine("protoss", "archon.bmp"),
            ["protoss_cmd_darkarchonmeld"] = Path.Combine("protoss", "darkarchon.jpg"),
            ["protoss_cmd_distweb"] = Path.Combine("protoss", "disruptionweb.jpg"),
            ["protoss_cmd_mindcontrol"] = Path.Combine("protoss", "mind_control.bmp"),
            ["protoss_cmd_feedback"] = Path.Combine("protoss", "feedback.jpg"),
            ["protoss_cmd_malestrom"] = Path.Combine("protoss", "maelstrom.jpg"),
            ["protoss_cmd_recall"] = Path.Combine("protoss", "recall.bmp"),
            ["protoss_cmd_stasis"] = Path.Combine("protoss", "stasis.bmp"),
            ["protoss_cmd_hallucination"] = Path.Combine("protoss", "hallucination.bmp"),
            ["protoss_build_shieldbattery"] = Path.Combine("protoss", "shiled_battery.bmp"),
            ["protos_train_carrier"] = Path.Combine("protoss", "carrier.bmp"),

            ["zerg_cmd_selectlarva"] = Path.Combine("zerg", "larva.bmp"),
            ["zerg_cmd_buildstruc"] = Path.Combine("zerg", "cmd_basicmut.jpg"),
            ["zerg_cmd_buildadvstruc"] = Path.Combine("zerg", "cmd_advmut.jpg"),
            ["zerg_cmd_unburrow"] = Path.Combine("zerg", "cmd_unburrow.bmp"),
            ["zerg_cmd_infest_cc"] = Path.Combine("zerg", "cmd_infest_command_center.bmp"),
            ["zerg_cmd_broodling"] = Path.Combine("zerg", "spawn_broodlings.bmp"),
            ["zerg_cmd_darkswarm"] = Path.Combine("zerg", "dark_swarm.bmp"),
            ["zerg_cmd_devourer"] = Path.Combine("zerg", "devourer.jpg"),
            ["zerg_cmd_lurker"] = Path.Combine("zerg", "lurker.jpg"),
            ["zerg_cmd_greaterspire"] = Path.Combine("zerg", "greater_spire.bmp"),
            ["zerg_cmd_sporecolony"] = Path.Combine("zerg", "spore_colony.bmp"),
            ["zerg_cmd_sunkencolony"] = Path.Combine("zerg", "sunken_colony.bmp"),
            ["zerg_cmd_placenydusexit"] = Path.Combine("zerg", "nydus_canal.bmp"),
            ["zerg_cmode_plague"] = Path.Combine("zerg", "plague.bmp"),
            ["zerg_buiild_queensnest"] = Path.Combine("zerg", "queens_nest.bmp")
        };

    private static IEnumerable<string> HotkeyIconCandidates(HotkeyEntry entry)
    {
        var commandId = entry.CommandId.ToLowerInvariant();
        if (HotkeyIconOverrides.TryGetValue(commandId, out var overridePath))
        {
            yield return overridePath;
        }

        var folder = HotkeyIconFolder(commandId);
        var stem = StripHotkeyIconCommandPrefix(commandId);
        foreach (var candidate in HotkeyIconFileCandidates(folder, stem))
        {
            yield return candidate;
        }

        var categorylessStem = StripHotkeyIconCategoryPrefix(stem);
        if (!string.Equals(stem, categorylessStem, StringComparison.Ordinal))
        {
            foreach (var candidate in HotkeyIconFileCandidates(folder, categorylessStem))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<string> HotkeyIconFileCandidates(string folder, string stem)
    {
        var normalized = NormalizeHotkeyIconStem(stem);
        yield return Path.Combine(folder, $"{stem}.bmp");
        yield return Path.Combine(folder, $"{stem}.jpg");
        if (!string.Equals(stem, normalized, StringComparison.Ordinal))
        {
            yield return Path.Combine(folder, $"{normalized}.bmp");
            yield return Path.Combine(folder, $"{normalized}.jpg");
        }
    }

    private static string HotkeyIconFolder(string commandId)
    {
        return commandId switch
        {
            var value when value.StartsWith("terran_", StringComparison.Ordinal) => "terran",
            var value when value.StartsWith("protoss_", StringComparison.Ordinal) => "protoss",
            var value when value.StartsWith("protos_", StringComparison.Ordinal) => "protoss",
            var value when value.StartsWith("zerg_", StringComparison.Ordinal) => "zerg",
            _ => "general"
        };
    }

    private static string StripHotkeyIconCommandPrefix(string commandId)
    {
        foreach (var prefix in new[] { "terran_", "protoss_", "protos_", "zerg_", "general_" })
        {
            if (commandId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return commandId[prefix.Length..];
            }
        }

        return commandId;
    }

    private static string StripHotkeyIconCategoryPrefix(string stem)
    {
        foreach (var prefix in new[] { "cmd_", "ab_", "cmode_", "train_", "build_", "res_", "upg_" })
        {
            if (stem.StartsWith(prefix, StringComparison.Ordinal))
            {
                return stem[prefix.Length..];
            }
        }

        return stem;
    }

    private static string NormalizeHotkeyIconStem(string stem)
    {
        return stem
            .Replace("psistorm", "psi_storm", StringComparison.Ordinal)
            .Replace("distweb", "disruptionweb", StringComparison.Ordinal)
            .Replace("malestrom", "maelstrom", StringComparison.Ordinal)
            .Replace("darkarchon", "darkarchon", StringComparison.Ordinal)
            .Replace("defmatrix", "defensive_matrix", StringComparison.Ordinal)
            .Replace("spidermines", "spidermine", StringComparison.Ordinal)
            .Replace("scannner", "scanner", StringComparison.Ordinal);
    }

    private static Color HotkeyTileAccentColor(HotkeyEntry entry)
    {
        return HotkeyRaceName(entry) switch
        {
            "Terran" => Color.FromArgb(120, 210, 255),
            "Zerg" => Color.FromArgb(198, 130, 255),
            "Protoss" => Color.FromArgb(255, 218, 104),
            _ => Color.FromArgb(154, 238, 112)
        };
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
            _hotkeyStore.ImportFromDefaultAssets(_paths, _paths.PlayerRuntimeRoot);
            LoadHotkeys();
            MessageBox.Show(this, "Sparring 기본 핫키 CSV를 작업 런타임으로 가져왔습니다.", "핫키");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "핫키 가져오기 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportRemasteredHotkeysFromDetectedLocation()
    {
        try
        {
            var source = RemasteredHotkeyImporter.FindDefaultCandidateFile();
            if (source is null)
            {
                MessageBox.Show(
                    this,
                    "Battle.net/Remastered 핫키 파일을 자동으로 찾지 못했습니다. '폴더 지정'으로 StarCraft 설치 또는 Documents\\StarCraft 폴더를 선택해 주세요.",
                    "핫키");
                return;
            }

            ImportRemasteredHotkeyFile(source);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Battle.net 핫키 가져오기 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportRemasteredHotkeysFromFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Battle.net/Remastered 핫키 파일이 있는 StarCraft 폴더를 선택하세요.",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var source = RemasteredHotkeyImporter.FindFirstCandidateFile([dialog.SelectedPath]);
            if (source is null)
            {
                MessageBox.Show(this, "선택한 폴더에서 STR_* 형식의 Remastered 핫키 파일을 찾지 못했습니다.", "핫키");
                return;
            }

            ImportRemasteredHotkeyFile(source);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "폴더 핫키 가져오기 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportRemasteredHotkeyFile(string source)
    {
        ApplySelectedHotkey(showStatus: false);
        var result = RemasteredHotkeyImporter.ApplyFromFile(source, _hotkeyEntries);
        RefreshHotkeyObjects();
        _statusLabel.Text = $"Remastered 핫키 가져오기: {result.UpdatedCount}개 반영 | {source}";
        MessageBox.Show(
            this,
            $"Remastered 핫키 {result.UpdatedCount}개를 현재 작업 CSV에 반영했습니다.\r\nCSV 저장 또는 런타임 반영을 눌러 적용하세요.\r\n\r\n원본: {source}",
            "핫키");
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
            var mapConstraint = !IsLadderMode && _mapList.SelectedItem is MapItem { Map: { } selectedMap }
                ? selectedMap
                : null;
            var bots = FilterBotsForCurrentControls(mapConstraint);
            bots = IsLadderMode
                ? bots.OrderByDescending(bot => bot.Elo ?? int.MinValue).ThenBy(bot => bot.Name)
                : (_sortCombo.SelectedItem?.ToString() ?? "ELO 높은순") switch
            {
                "ELO 낮은순" => bots.OrderBy(bot => bot.Elo ?? int.MaxValue).ThenBy(bot => bot.Name),
                "이름순" => bots.OrderBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase),
                _ => bots.OrderByDescending(bot => bot.Elo ?? int.MinValue).ThenBy(bot => bot.Name)
            };

            var items = bots.Select(bot => new BotItem(bot)).ToList();
            if (!IsLadderMode)
            {
                items.Insert(0, BotItem.Random);
            }

            var previousBotName = (_botList.SelectedItem as BotItem)?.Bot?.Name ?? _settings.LastBotName;
            _botList.DataSource = items;
            SelectBotItem(items, previousBotName);
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
            var previousMapId = (_mapList.SelectedItem as MapItem)?.Map?.Id;
            var previousMapName = (_mapList.SelectedItem as MapItem)?.Map?.Name ?? _settings.LastMapName;
            var maps = botItem.Bot is null
                ? _catalog.Maps
                    .Where(map => map.Enabled)
                    .Where(map => FilterBotsForCurrentControls(map).Any())
                    .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : PracticeCatalogCompatibility.MapsForBot(_catalog, botItem.Bot.Id);
            var items = maps
                .Select(map => new MapItem(map))
                .ToList();
            items.Insert(0, MapItem.Random);
            _mapList.DataSource = items;
            var restore = items.FirstOrDefault(item => item.Map?.Id == previousMapId) ??
                items.FirstOrDefault(item => string.Equals(item.Map?.Name, previousMapName, StringComparison.OrdinalIgnoreCase)) ??
                items.FirstOrDefault();
            if (restore is not null)
            {
                _mapList.SelectedItem = restore;
            }
        }
        finally
        {
            _updatingSelections = false;
        }

        UpdateDetails();
    }

    private IEnumerable<PracticeBot> FilterBotsForCurrentControls(PracticeMap? mapConstraint)
    {
        if (_catalog is null)
        {
            return [];
        }

        var query = _searchBox.Text.Trim();
        var raceFilter = SelectedEnemyRace();
        var bots = _catalog.Bots.Where(bot => bot.UsesBwapiIniAiModule);

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

        if (mapConstraint is not null)
        {
            bots = bots.Where(bot => PracticeCatalogCompatibility.IsCompatible(_catalog, bot.Id, mapConstraint.Id));
        }
        else
        {
            bots = bots.Where(bot => PracticeCatalogCompatibility.MapsForBot(_catalog, bot.Id).Count > 0);
        }

        return bots;
    }

    private void UpdateDetails()
    {
        if (_catalog is null)
        {
            UpdateMapPreview(null);
            return;
        }

        var mapItem = _mapList.SelectedItem as MapItem;
        UpdateMapPreview(mapItem?.Map);
        if (IsLadderMode)
        {
            UpdateLadderDetails(mapItem?.Map);
            return;
        }

        if (_botList.SelectedItem is not BotItem botItem)
        {
            return;
        }

        if (botItem.Bot is null)
        {
            var randomMap = mapItem?.Map;
            var candidates = FilterBotsForCurrentControls(randomMap).ToList();
            _difficultyLabel.Text = candidates.Count == 0
                ? "랜덤\r\n후보 없음"
                : $"랜덤\r\n후보 {candidates.Count}개";
            _detailsText.Text = string.Join(Environment.NewLine, new[]
            {
                "봇: Random",
                $"맵: {(randomMap is null ? "Random" : $"{randomMap.Name} ({randomMap.FileName})")}",
                $"호환 후보: {candidates.Count}개",
                $"상대 종족: {RaceFilterLabel(SelectedEnemyRace())}",
                "시작 시점에 호환 가능한 봇/맵 조합 중 하나를 무작위로 선택합니다."
            });
            return;
        }

        var bot = botItem.Bot;
        var map = mapItem?.Map;
        var compatibleMapCount = PracticeCatalogCompatibility.MapsForBot(_catalog, bot.Id).Count;
        var difficulty = LadderDifficultyEstimator.EstimateFromSchnailElo(bot.Elo);
        var profile = BotProfileSummarizer.Summarize(bot);
        _difficultyLabel.Text = difficulty is null
            ? "난이도\r\nSCR 환산 미확인"
            : $"난이도\r\n{difficulty.Label}";

        _detailsText.Text = string.Join(Environment.NewLine, new[]
        {
            $"AI: {bot.Name}",
            $"종족: {bot.Race}",
            $"난이도: {difficulty?.Label ?? $"ELO {bot.Elo?.ToString() ?? "미확인"}"}",
            $"성향: {profile.StyleLabel}",
            $"빌드: {profile.BuildLabel}",
            $"호환 맵: {compatibleMapCount}개",
            $"선택 맵: {(map is null ? "Random" : $"{map.Name} ({map.FileName})")}",
            "",
            "설명",
            profile.PlayerSummary
        });
    }

    private void UpdateMapPreview(PracticeMap? map)
    {
        if (_mapPreviewBox is null || _mapPreviewLabel is null)
        {
            return;
        }

        _mapPreviewBox.Image = null;
        _mapPreviewImage?.Dispose();
        _mapPreviewImage = null;

        if (map is null)
        {
            _mapPreviewLabel.Text = "맵 미리보기";
            return;
        }

        var previewPath = ResolveMapPreviewPath(map);
        if (previewPath is null)
        {
            _mapPreviewLabel.Text = $"{map.Name} / 미리보기 없음";
            return;
        }

        try
        {
            using var stream = File.OpenRead(previewPath);
            using var source = Image.FromStream(stream);
            _mapPreviewImage = new Bitmap(source);
            _mapPreviewBox.Image = _mapPreviewImage;
            _mapPreviewLabel.Text = map.Name;
        }
        catch
        {
            _mapPreviewLabel.Text = $"{map.Name} / 미리보기 로드 실패";
        }
    }

    private string? ResolveMapPreviewPath(PracticeMap map)
    {
        var direct = ResolveDirectMapPreviewPath(map);
        if (direct is not null)
        {
            return direct;
        }

        if (_catalog is null || map.CompatibilityMapIds is not { Count: > 0 })
        {
            return null;
        }

        foreach (var compatibilityMapId in map.CompatibilityMapIds)
        {
            var compatibleMap = _catalog.Maps.FirstOrDefault(candidate => candidate.Id == compatibilityMapId);
            if (compatibleMap is null || compatibleMap.Id == map.Id)
            {
                continue;
            }

            var compatiblePreview = ResolveDirectMapPreviewPath(compatibleMap);
            if (compatiblePreview is not null)
            {
                return compatiblePreview;
            }
        }

        return null;
    }

    private string? ResolveDirectMapPreviewPath(PracticeMap map)
    {
        foreach (var candidate in EnumerateMapPreviewPathCandidates(map))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private IEnumerable<string> EnumerateMapPreviewPathCandidates(PracticeMap map)
    {
        if (!string.IsNullOrWhiteSpace(map.ImagePath))
        {
            yield return Path.IsPathRooted(map.ImagePath)
                ? map.ImagePath
                : Path.Combine(_paths.AssetRoot, map.ImagePath);
        }

        if (!string.IsNullOrWhiteSpace(map.SourcePath))
        {
            yield return map.SourcePath + ".jpg";
        }

        if (!string.IsNullOrWhiteSpace(map.FileName))
        {
            yield return Path.Combine(_paths.AssetRoot, "maps", map.FileName + ".jpg");
        }
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
            maps.Insert(0, MapItem.Random);
            var previousMapName = (_mapList.SelectedItem as MapItem)?.Map?.Name ?? _settings.LastMapName;
            _mapList.DataSource = maps;
            SelectMapItem(maps, previousMapName);
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
        var playerRating = _ratingStore.Load().PlayerRating;
        if (map is null)
        {
            var compatibleMaps = _catalog.Maps
                .Where(candidate => candidate.Enabled)
                .Where(candidate => LadderBotSelector.CandidatesForMap(_catalog, candidate.Id, enemyRace).Count > 0)
                .OrderBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var weightedCandidates = LadderBotSelector.CandidatesForEnabledMaps(_catalog, enemyRace);
            var randomMapPreview = weightedCandidates
                .OrderBy(bot => Math.Abs((bot.Elo ?? EloRatingCalculator.DefaultRating) - playerRating))
                .ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .Select(bot =>
                {
                    var difficulty = LadderDifficultyEstimator.EstimateFromSchnailElo(bot.Elo);
                    return $"- {bot.Name} / {bot.Race} / {difficulty?.Label ?? $"ELO {bot.Elo?.ToString() ?? "?"}"}";
                });
            _difficultyLabel.Text = compatibleMaps.Count == 0
                ? "래더\r\n후보 없음"
                : $"래더\r\nMMR 후보 {weightedCandidates.Count}개";
            _detailsText.Text = string.Join(Environment.NewLine, new[]
            {
                "모드: 래더",
                $"내 종족: {(_playerRaceCombo.SelectedItem is StarCraftRace selectedRace ? selectedRace : StarCraftRace.Terran)}",
                $"상대 종족: {RaceFilterLabel(enemyRace)}",
                "선택 맵: Random",
                $"현재 MMR: {playerRating}",
                $"호환 맵 후보: {compatibleMaps.Count}개",
                $"래더 봇 후보: {weightedCandidates.Count}개",
                "매칭: 현재 MMR 근처 봇을 먼저 가중 선택한 뒤, 해당 봇의 호환 맵을 선택합니다.",
                "",
                "MMR 근접 후보 미리보기",
                string.Join(Environment.NewLine, randomMapPreview)
            });
            return;
        }

        IReadOnlyList<PracticeBot> candidates = map is null
            ? Array.Empty<PracticeBot>()
            : LadderBotSelector.CandidatesForMap(_catalog, map.Id, enemyRace);
        _difficultyLabel.Text = candidates.Count == 0
            ? "래더\r\n후보 없음"
            : $"래더\r\n후보 {candidates.Count}개";

        var difficultyRange = FormatDifficultyRange(candidates);
        var preview = candidates
            .OrderBy(bot => Math.Abs((bot.Elo ?? EloRatingCalculator.DefaultRating) - playerRating))
            .ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(bot =>
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
            $"현재 MMR: {playerRating}",
            $"래더 봇 후보: {candidates.Count}개",
            $"난이도 범위: {difficultyRange}",
            "매칭: 선택 맵 안에서 현재 MMR 근처 봇을 높은 확률로 선택합니다.",
            "승리하면 Elo 계산식 기준으로 점수가 오르고, 승리 보상은 최소 +1점입니다.",
            "패배하면 상대 MMR과 현재 MMR 차이에 따라 점수가 내려갑니다.",
            "",
            "MMR 근접 후보 미리보기",
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
            DisposePracticeOverlay();
            var existingStarCraftProcesses = StarCraftBorderlessWindow.CurrentStarCraftProcessIds();

            var report = await Task.Run(() => new PracticeSessionLauncher().Launch(
                plan,
                runtimeOptions,
                PracticeSessionLaunchOptions.Defaults()));
            var borderlessApplied = false;

            if (report.Ai.StarCraftProcessId is not null)
            {
                _aiMinimizeKeeper = new StarCraftWindowMinimizeOnceWhenReady(
                    report.Ai.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(18));
            }

            if (report.Player.StarCraftProcessId is not null)
            {
                borderlessApplied = cncDdrawHandlesPlayerDisplay ||
                    await Task.Run(() => StarCraftBorderlessWindow.ApplyToProcessWhenReady(
                        report.Player.StarCraftProcessId.Value,
                        screenBounds,
                        TimeSpan.FromSeconds(8)).Applied);
            }

            if (report.Player.StarCraftProcessId is not null && !cncDdrawHandlesPlayerDisplay)
            {
                _borderlessKeeper = new StarCraftBorderlessKeeper(
                    report.Player.StarCraftProcessId.Value,
                    screenBounds,
                    TimeSpan.FromSeconds(8));
            }

            if (report.Player.StarCraftProcessId is null)
            {
                throw new InvalidOperationException("StarCraft player process was not detected, so the in-game timer could not start.");
            }

            _statusLabel.Text = $"{CurrentModeLabel()} 게임 화면 진입 대기 중 | 타이머/APM은 HUD 감지 후 시작";
            await Task.Run(() => StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(
                report.Player.StarCraftProcessId.Value,
                TimeSpan.FromSeconds(5)));
            var inGameDetected = await StarCraftScreenDetector.WaitForInGameAsync(
                report.Player.StarCraftProcessId.Value,
                TimeSpan.FromSeconds(90));
            if (!inGameDetected)
            {
                throw new InvalidOperationException("StarCraft in-game HUD was not detected within 90 seconds, so the timer/APM overlay was not started.");
            }

            var sessionStartedAt = DateTime.UtcNow;
            var actionCounter = new ActionRateCounter();
            _inputActionHook = new GlobalInputActionHook(
                actionCounter,
                existingStarCraftProcesses,
                report.Player.StarCraftProcessId.Value,
                RequestPlayerGracefulExitFromShortcut);
            _practiceOverlay = new PracticeOverlayForm();
            var overlayBounds = StarCraftBorderlessWindow.TryGetProcessWindowBounds(
                report.Player.StarCraftProcessId.Value,
                out var playerWindowBounds)
                    ? playerWindowBounds
                    : screenBounds;
            _practiceOverlay.StartSession(overlayBounds, sessionStartedAt, actionCounter);
            var sessionMode = CurrentSessionMode();
            StartSessionHistory(plan, runtimeOptions, sessionStartedAt, actionCounter, sessionMode);
            PracticeRuntimeConfigurator.DisableAutoMenuAfterGameStart(plan);
            StartSessionLifecycleMonitor(plan, runtimeOptions, report, sessionStartedAt, actionCounter, sessionMode);

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
        if (_catalog is null)
        {
            throw new InvalidOperationException("카탈로그를 먼저 로드해주세요.");
        }

        var (bot, map) = IsLadderMode
            ? ResolveLadderSelection()
            : ResolveSparringSelection();
        var aiRoot = RuntimeProvisioner.EnsureAiRoot(_paths.PlayerRuntimeRoot);
        var paths = _paths with { AiRuntimeRoot = aiRoot };
        var race = _playerRaceCombo.SelectedItem is StarCraftRace selectedRace ? selectedRace : StarCraftRace.Terran;
        var selection = new PracticeSelection(
            bot.Id,
            map.Id,
            race,
            IsLadderMode ? "Sparring Ladder" : "Sparring Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false,
            HideAiName: _settings.EffectiveHideAiName);
        var plan = PracticeLaunchPlanBuilder.Build(_catalog, paths, selection);
        return RuntimeProvisioner.PrepareRuntimeAssets(plan);
    }

    private (PracticeBot Bot, PracticeMap Map) ResolveLadderSelection()
    {
        if (_catalog is null || _mapList.SelectedItem is not MapItem mapItem)
        {
            throw new InvalidOperationException("래더 모드에서는 맵을 먼저 선택해주세요.");
        }

        var enemyRace = SelectedEnemyRace();
        var playerRating = _ratingStore.Load().PlayerRating;
        if (mapItem.Map is { } selectedMap)
        {
            var selectedMapBot = LadderBotSelector.PickForRating(
                _catalog,
                selectedMap.Id,
                enemyRace,
                playerRating,
                _random);
            return (selectedMapBot, selectedMap);
        }

        var bot = LadderBotSelector.PickForRatingAcrossEnabledMaps(
            _catalog,
            enemyRace,
            playerRating,
            _random);
        var map = PickRandomLadderMapForBot(bot);
        return (bot, map);
    }

    private PracticeMap PickRandomLadderMapForBot(PracticeBot bot)
    {
        if (_catalog is null)
        {
            throw new InvalidOperationException("카탈로그를 먼저 로드해주세요.");
        }

        var maps = PracticeCatalogCompatibility.MapsForBot(_catalog, bot.Id)
            .ToList();
        if (maps.Count == 0)
        {
            throw new InvalidOperationException($"래더에 사용할 수 있는 '{bot.Name}' 호환 맵이 없습니다.");
        }

        return maps[_random.Next(maps.Count)];
    }

    private (PracticeBot Bot, PracticeMap Map) ResolveSparringSelection()
    {
        if (_catalog is null ||
            _botList.SelectedItem is not BotItem botItem ||
            _mapList.SelectedItem is not MapItem mapItem)
        {
            throw new InvalidOperationException("스파링 모드에서는 봇과 맵을 먼저 선택해주세요.");
        }

        if (botItem.Bot is { } selectedBot && mapItem.Map is { } selectedMap)
        {
            if (!PracticeCatalogCompatibility.IsCompatible(_catalog, selectedBot.Id, selectedMap.Id))
            {
                throw new InvalidOperationException($"'{selectedBot.Name}' 봇과 '{selectedMap.Name}' 맵은 호환되지 않습니다.");
            }

            return (selectedBot, selectedMap);
        }

        if (botItem.Bot is { } bot)
        {
            var maps = PracticeCatalogCompatibility.MapsForBot(_catalog, bot.Id);
            if (maps.Count == 0)
            {
                throw new InvalidOperationException($"'{bot.Name}' 봇과 호환되는 맵이 없습니다.");
            }

            return (bot, maps[_random.Next(maps.Count)]);
        }

        if (mapItem.Map is { } map)
        {
            var bots = FilterBotsForCurrentControls(map).ToList();
            if (bots.Count == 0)
            {
                throw new InvalidOperationException($"'{map.Name}' 맵과 호환되는 봇이 없습니다.");
            }

            return (bots[_random.Next(bots.Count)], map);
        }

        var pairs = FilterBotsForCurrentControls(null)
            .SelectMany(bot => PracticeCatalogCompatibility.MapsForBot(_catalog, bot.Id), (bot, map) => (bot, map))
            .ToList();
        if (pairs.Count == 0)
        {
            throw new InvalidOperationException("랜덤으로 선택할 수 있는 호환 봇/맵 조합이 없습니다.");
        }

        return pairs[_random.Next(pairs.Count)];
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
            _buildCombo is null ||
            _ladderRatingLabel is null ||
            _ladderRatingText is null ||
            _applyRatingButton is null ||
            _resetRatingButton is null)
        {
            return;
        }

        var ladder = IsLadderMode;
        _searchBox.Enabled = !ladder;
        _sortCombo.Enabled = !ladder;
        _botList.Enabled = !ladder;
        _botListLabel.Text = ladder ? "래더 후보" : "상대 선택";
        _launchButton.Text = ladder ? "래더 시작" : "스파링 시작";
        _ladderRatingLabel.Visible = ladder;
        _ladderRatingText.Visible = ladder;
        _applyRatingButton.Visible = ladder;
        _resetRatingButton.Visible = ladder;
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

    private PracticeSessionMode CurrentSessionMode()
    {
        return IsLadderMode ? PracticeSessionMode.Ladder : PracticeSessionMode.Sparring;
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
        ActionRateCounter actionCounter,
        PracticeSessionMode mode)
    {
        _sessionHistoryTimer?.Stop();
        _sessionHistoryTimer?.Dispose();

        _activeActionCounter = actionCounter;
        _activeSessionStartedAtUtc = startedAtUtc;
        var playerRating = mode == PracticeSessionMode.Ladder
            ? _ratingStore.Load().PlayerRating
            : (int?)null;
        var opponentRating = mode == PracticeSessionMode.Ladder
            ? plan.Bot.Elo ?? EloRatingCalculator.DefaultRating
            : (int?)null;
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
            DurationSeconds: 0,
            Mode: mode,
            Outcome: PracticeSessionOutcome.InProgress,
            PlayerRatingBefore: playerRating,
            OpponentRating: opponentRating,
            PlayerRatingAfter: playerRating,
            RatingDelta: null,
            ResultSource: null);

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

    private void StartSessionLifecycleMonitor(
        PracticeLaunchPlan plan,
        PracticeRuntimeOptions runtimeOptions,
        PracticeSessionLaunchReport report,
        DateTime startedAtUtc,
        ActionRateCounter actionCounter,
        PracticeSessionMode mode)
    {
        _sessionLifecycleTimer?.Stop();
        _sessionLifecycleTimer?.Dispose();
        _notInGameSamples = 0;
        _finalizingSession = false;
        _activeSession = new ActivePracticeSession(plan, runtimeOptions, report, startedAtUtc, actionCounter, mode);
        _sessionLifecycleTimer = new System.Windows.Forms.Timer
        {
            Interval = 1500
        };
        _sessionLifecycleTimer.Tick += OnSessionLifecycleTick;
        _sessionLifecycleTimer.Start();
    }

    private async void OnSessionLifecycleTick(object? sender, EventArgs e)
    {
        if (_finalizingSession || _activeSession is not { } session)
        {
            return;
        }

        var playerAlive = IsProcessRunning(session.LaunchReport.Player.StarCraftProcessId);
        var aiAlive = IsProcessRunning(session.LaunchReport.Ai.StarCraftProcessId);
        if (!playerAlive)
        {
            await FinalizeActiveSessionAsync("player-process-exited");
            return;
        }

        if (!aiAlive)
        {
            await FinalizeActiveSessionAsync("ai-process-exited");
            return;
        }

        var state = session.LaunchReport.Player.StarCraftProcessId is null
            ? StarCraftScreenState.Unknown
            : await Task.Run(() => StarCraftScreenDetector.Detect(session.LaunchReport.Player.StarCraftProcessId.Value));

        if (state == StarCraftScreenState.InGame)
        {
            _notInGameSamples = 0;
            return;
        }

        if (state is StarCraftScreenState.MenuLike or
            StarCraftScreenState.GameRoom or
            StarCraftScreenState.PreGameWait or
            StarCraftScreenState.BlockedDialog)
        {
            _notInGameSamples++;
        }

        if (_notInGameSamples >= 3)
        {
            await FinalizeActiveSessionAsync($"player-left-ingame:{state}");
        }
    }

    private async Task FinalizeActiveSessionAsync(string reason)
    {
        if (_finalizingSession || _activeSession is not { } session)
        {
            return;
        }

        _finalizingSession = true;
        _sessionLifecycleTimer?.Stop();
        var result = FindBotResult(session);
        var gameStateResult = FindPlayerGameStateResult(session);
        var outcome = PracticeSessionOutcomeResolver.Resolve(session.Mode, result, gameStateResult, reason);
        CompleteActiveSessionHistory(outcome, result?.SourcePath ?? gameStateResult?.SourcePath ?? reason);
        DisposePracticeOverlay();

        var stopped = await Task.Run(() =>
        {
            var cleaner = new LocalRuntimeProcessCleaner();
            var cleanup = PracticeSessionCleanupPolicy.ForGameFinalization(session.Plan, session.LaunchReport);
            if (cleanup.LeaveGameBeforeTerminate && cleanup.KnownStarCraftProcessId is { } aiProcessId)
            {
                StarCraftGameExitController.LeaveGameThenCloseProcess(
                    aiProcessId,
                    TimeSpan.FromSeconds(4),
                    TimeSpan.FromSeconds(3));
            }

            return cleaner.StopKnown(cleanup.KnownStarCraftProcessId) +
                   cleaner.Stop(cleanup.RuntimeRoot);
        });

        _statusLabel.Text =
            $"게임 종료 감지 | 결과 {PracticeSessionOutcomeText.ToDisplayText(outcome)} | " +
            $"로컬 StarCraft/AI 정리 {stopped}개 | 타이머/APM OFF";
    }

    private void RequestPlayerGracefulExitFromShortcut()
    {
        if (_playerExitShortcutRequested)
        {
            return;
        }

        _playerExitShortcutRequested = true;
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(new Action(() => _ = HandlePlayerGracefulExitShortcutAsync()));
        }
        catch
        {
            // The form may be closing while the low-level keyboard hook is unwinding.
        }
    }

    private async Task HandlePlayerGracefulExitShortcutAsync()
    {
        if (_finalizingSession || _activeSession is not { } session)
        {
            return;
        }

        _statusLabel.Text = "Alt+F4 감지 | 게임을 정상 나가기 순서로 종료합니다.";
        if (session.LaunchReport.Player.StarCraftProcessId is { } playerProcessId)
        {
            _sessionLifecycleTimer?.Stop();
            await Task.Run(() => StarCraftGameExitController.LeaveGameThenCloseProcess(
                playerProcessId,
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(3)));
        }

        await FinalizeActiveSessionAsync("player-left-ingame:AltF4");
    }

    private BotResultLogObservation? FindBotResult(ActivePracticeSession session)
    {
        var roots = new List<string>();
        var botExecutablePath = Path.Combine(session.Plan.Ai.RuntimeRoot, session.Plan.Ai.BotExecutable);
        var botDirectory = Path.GetDirectoryName(botExecutablePath);
        if (!string.IsNullOrWhiteSpace(botDirectory))
        {
            roots.Add(botDirectory);
        }

        roots.Add(Path.Combine(session.Plan.Ai.RuntimeRoot, "bwapi-data", "logs"));
        return BotResultLogReader.FindLatestPlayerOutcome(roots, session.StartedAtUtc);
    }

    private static TournamentGameStateObservation? FindPlayerGameStateResult(ActivePracticeSession session)
    {
        return TournamentGameStateReader.FindPlayerOutcome(
            session.Plan.Player.RuntimeRoot,
            session.StartedAtUtc,
            session.Plan.Player.CharacterName);
    }

    private void CompleteActiveSessionHistory(PracticeSessionOutcome outcome, string resultSource)
    {
        UpdateActiveSessionHistory();
        if (_activeSessionRecord is null)
        {
            return;
        }

        var record = _activeSessionRecord with
        {
            Outcome = outcome,
            ResultSource = resultSource
        };

        if (record.Mode == PracticeSessionMode.Ladder &&
            outcome is PracticeSessionOutcome.PlayerWin or PracticeSessionOutcome.PlayerLoss or PracticeSessionOutcome.Draw)
        {
            var before = record.PlayerRatingBefore ?? _ratingStore.Load().PlayerRating;
            var opponent = record.OpponentRating ?? EloRatingCalculator.DefaultRating;
            var change = EloRatingCalculator.Calculate(before, opponent, outcome);
            _ratingStore.Save(change.PlayerRatingAfter);
            record = record with
            {
                PlayerRatingBefore = change.PlayerRatingBefore,
                OpponentRating = change.OpponentRating,
                PlayerRatingAfter = change.PlayerRatingAfter,
                RatingDelta = change.Delta
            };
            RefreshRatingUi();
        }

        _activeSessionRecord = record;
        _historyStore.Upsert(record);
        RefreshHistory();
    }

    private static bool IsProcessRunning(int? processId)
    {
        if (processId is null)
        {
            return false;
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId.Value);
            try
            {
                return !process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        DisposePracticeOverlay();
        _hotkeyToolTip.Dispose();
        foreach (var image in _hotkeyIconCache.Values)
        {
            image.Dispose();
        }

        _hotkeyIconCache.Clear();
        base.OnFormClosed(e);
    }

    private void DisposePracticeOverlay()
    {
        UpdateActiveSessionHistory();
        _sessionHistoryTimer?.Stop();
        _sessionHistoryTimer?.Dispose();
        _sessionHistoryTimer = null;
        _sessionLifecycleTimer?.Stop();
        _sessionLifecycleTimer?.Dispose();
        _sessionLifecycleTimer = null;
        _activeSessionRecord = null;
        _activeActionCounter = null;
        _activeSession = null;
        _notInGameSamples = 0;
        _finalizingSession = false;
        _playerExitShortcutRequested = false;

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
            AutoSize = false,
            Size = new Size(120, 24),
            ForeColor = Color.FromArgb(128, 218, 93),
            Location = new Point(x, y)
        };
    }

    private static Label? FindLabel(Control root, string text)
    {
        return root.Controls
            .OfType<Label>()
            .FirstOrDefault(label => string.Equals(label.Text, text, StringComparison.Ordinal));
    }

    private static Button? FindButton(Control root, string text)
    {
        return root.Controls
            .OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Text, text, StringComparison.Ordinal));
    }

    private static IEnumerable<Button> FindButtons(Control root, string text)
    {
        return root.Controls
            .OfType<Button>()
            .Where(button => string.Equals(button.Text, text, StringComparison.Ordinal));
    }

    private static TextBox? FindRuntimeTextBox(Control root)
    {
        return root.Controls
            .OfType<TextBox>()
            .FirstOrDefault(textBox => textBox.Multiline && textBox.Text.StartsWith("사람:", StringComparison.Ordinal));
    }

    private static ComboBox CreateCombo(int x, int y, int width)
    {
        return new SparringComboBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 28)
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

    private static NumericUpDown CreateNumericInput(int x, int y, int width, int value)
    {
        return new NumericUpDown
        {
            Location = new Point(x, y),
            Size = new Size(width, 28),
            Minimum = 0,
            Maximum = 6,
            Value = Math.Clamp(value, 0, 6),
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

    private void SelectGameSpeedOption(int speedOverrideMs)
    {
        var option = GameSpeedOptions.FirstOrDefault(item => item.SpeedOverrideMs == speedOverrideMs) ?? GameSpeedOptions[0];
        _gameSpeedCombo.SelectedItem = option;
    }

    private sealed record GameSpeedOption(string Label, int SpeedOverrideMs)
    {
        public override string ToString() => Label;
    }

    private static readonly IReadOnlyList<GameSpeedOption> GameSpeedOptions =
    [
        new("기본값", -1),
        new("Fastest", 42),
        new("Faster", 48),
        new("Fast", 56),
        new("Normal", 67),
        new("Slow", 83),
        new("Slower", 111),
        new("Slowest", 167)
    ];

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

    private sealed record BotItem(PracticeBot? Bot)
    {
        public static BotItem Random { get; } = new((PracticeBot?)null);

        public override string ToString()
        {
            if (Bot is null)
            {
                return "Random";
            }

            var elo = Bot.Elo is null ? "ELO ?" : $"ELO {Bot.Elo}";
            return $"{Bot.Name} / {Bot.Race} / {elo}";
        }
    }

    private sealed record MapItem(PracticeMap? Map)
    {
        public static MapItem Random { get; } = new((PracticeMap?)null);

        public override string ToString() => Map?.Name ?? "Random";
    }

    private sealed record HotkeyObjectItem(HotkeyCommandObjectInfo Info, int Count, HotkeyEntry PreviewEntry)
    {
        public string Key => Info.Key;

        public override string ToString() => $"{Info.Category}  {Info.DisplayName}  ({Count})";
    }

    private sealed record ActivePracticeSession(
        PracticeLaunchPlan Plan,
        PracticeRuntimeOptions RuntimeOptions,
        PracticeSessionLaunchReport LaunchReport,
        DateTime StartedAtUtc,
        ActionRateCounter ActionCounter,
        PracticeSessionMode Mode);

}
