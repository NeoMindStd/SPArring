using Sparring.Core;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace Sparring.Setup;

internal sealed class SetupForm : Form
{
    private const string DefaultInstallRoot = @"C:\sparring";
    private const string PlayerRuntimeRoot = @"C:\sparring\SC116AI";
    private const string AiRuntimeRoot = @"C:\sparring\SC116AI_ai";
    private const string LegacyCmdLauncherPath = @"C:\sparring\Start-Sparring.cmd";
    private const string StarCraftGuideUrl = "https://github.com/NeoMindStd/SPArring#starcraft-1161-%EC%A4%80%EB%B9%84";
    private const string VcRedist2008Url = "https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x86.exe";
    private const string VcRedist2010Url = "https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe";
    private const string VcRedist2013Url = "https://aka.ms/highdpimfc2013x86enu";
    private const string VcRedistCurrentUrl = "https://aka.ms/vs/17/release/vc_redist.x86.exe";
    private const string TemurinJdkUrl = "https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jdk/hotspot/normal/eclipse";

    private readonly Panel _pagePanel = new() { Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 16), AutoScroll = true };
    private readonly Label _headerTitle = new();
    private readonly Label _headerDescription = new();
    private readonly TextBox _installRootBox = new() { Text = DefaultInstallRoot };
    private readonly TextBox _starCraftSourceBox = new();
    private readonly Label _componentDescription = new();
    private readonly ProgressBar _progressBar = new() { Minimum = 0, Maximum = 100 };
    private readonly Label _statusLabel = new();
    private readonly TextBox _logBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly Button _backButton = new() { Text = "< 뒤로" };
    private readonly Button _nextButton = new() { Text = "다음 >" };
    private readonly Button _cancelButton = new() { Text = "취소" };

    private TreeNode? _desktopShortcutNode;
    private TreeNode? _vcRedistsNode;
    private TreeNode? _javaNode;
    private TreeNode? _launchAfterInstallNode;
    private SetupPage _page = SetupPage.Paths;
    private bool _installing;
    private bool _completed;
    private bool _failed;

    public SetupForm(float fontSize = 9F)
    {
        Text = "Sparring Setup";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(840, 680);
        MinimumSize = new Size(780, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", fontSize);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BackColor = SystemColors.Control;

        ConfigureWizardButtons();

        Controls.Add(_pagePanel);
        Controls.Add(CreateHeader());
        Controls.Add(CreateFooter());

        _backButton.Click += (_, _) => MoveBack();
        _nextButton.Click += async (_, _) => await MoveNextAsync();
        _cancelButton.Click += (_, _) => Close();

        RenderPage();
    }

    private Control CreateHeader()
    {
        var headerHeight = Math.Max(84, Font.Height * 4 + 24);
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = headerHeight,
            BackColor = Color.White,
            Padding = new Padding(22, 12, 22, 8)
        };

        var icon = new PictureBox
        {
            Image = SystemIcons.Application.ToBitmap(),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Dock = DockStyle.Right,
            Width = 48
        };

        _headerTitle.AutoSize = false;
        _headerTitle.Dock = DockStyle.Top;
        _headerTitle.Height = Math.Max(26, Font.Height + 8);
        _headerTitle.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);

        _headerDescription.AutoSize = false;
        _headerDescription.Dock = DockStyle.Fill;
        _headerDescription.ForeColor = SystemColors.ControlText;

        header.Controls.Add(_headerDescription);
        header.Controls.Add(_headerTitle);
        header.Controls.Add(icon);
        return header;
    }

    private Control CreateFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = Math.Max(58, Font.Height + 44),
            BackColor = SystemColors.Control
        };

        var footerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        footerLayout.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SystemColors.ControlDark
        }, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 12, 18, 0),
            WrapContents = false
        };
        buttons.Controls.Add(_cancelButton);
        buttons.Controls.Add(_nextButton);
        buttons.Controls.Add(_backButton);
        footerLayout.Controls.Add(buttons, 0, 1);
        footer.Controls.Add(footerLayout);
        return footer;
    }

    private void ConfigureWizardButtons()
    {
        foreach (var button in new[] { _backButton, _nextButton, _cancelButton })
        {
            button.AutoSize = false;
            button.Width = Math.Max(112, TextRenderer.MeasureText(button.Text, Font).Width + 32);
            button.Height = Math.Max(34, Font.Height + 16);
            button.Margin = new Padding(4, 0, 4, 0);
        }
    }

    private void RenderPage()
    {
        _pagePanel.Controls.Clear();
        _backButton.Enabled = !_installing && _page == SetupPage.Components;
        _cancelButton.Enabled = !_installing && !_completed;

        if (_completed)
        {
            _nextButton.Text = "마침";
            _nextButton.Enabled = true;
        }
        else if (_failed)
        {
            _nextButton.Text = "닫기";
            _nextButton.Enabled = true;
        }
        else if (_page == SetupPage.Components)
        {
            _nextButton.Text = "설치";
            _nextButton.Enabled = !_installing;
        }
        else
        {
            _nextButton.Text = "다음 >";
            _nextButton.Enabled = !_installing;
        }

        switch (_page)
        {
            case SetupPage.Paths:
                _headerTitle.Text = "설치 위치 선택";
                _headerDescription.Text = "Sparring를 설치할 폴더와 StarCraft 1.16.1 원본 폴더를 선택합니다.";
                RenderPathPage();
                break;
            case SetupPage.Components:
                _headerTitle.Text = "구성 요소 선택";
                _headerDescription.Text = "설치할 선택 구성 요소를 고릅니다. 일반 사용자는 기본값을 권장합니다.";
                RenderComponentsPage();
                break;
            case SetupPage.Progress:
                _headerTitle.Text = _failed ? "설치 실패" : _completed ? "설치 완료" : "설치 중";
                _headerDescription.Text = _failed
                    ? "아래 로그와 안내를 확인한 뒤 다시 설치해 주세요."
                    : _completed
                    ? "설치 파일과 런타임 파일 검증을 마쳤습니다."
                    : "파일 복사, 선택 구성 요소 설치, 런타임 구성을 진행합니다.";
                RenderProgressPage();
                break;
        }
    }

    private void RenderPathPage()
    {
        var layout = CreateContentLayout();
        layout.Dock = DockStyle.Top;
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(CreatePathSection(), 0, 0);
        layout.Controls.Add(CreateStarCraftSection(), 0, 1);
        _pagePanel.Controls.Add(layout);
    }

    private Control CreatePathSection()
    {
        return CreatePathPickerGroup(
            "설치 폴더",
            "Sparring 설치 폴더:",
            _installRootBox,
            BrowseInstallRoot,
            @"기본값: C:\sparring");
    }

    private Control CreateStarCraftSection()
    {
        var link = new LinkLabel
        {
            Text = "StarCraft 1.16.1 준비 방법 보기",
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0)
        };
        link.LinkClicked += (_, _) => OpenUrl(StarCraftGuideUrl);
        return CreatePathPickerGroup(
            "StarCraft 1.16.1 원본",
            "원본 폴더:",
            _starCraftSourceBox,
            BrowseStarCraftRoot,
            link);
    }

    private Control CreatePathPickerGroup(
        string title,
        string labelText,
        TextBox textBox,
        Action browse,
        string helpText)
    {
        return CreatePathPickerGroup(
            title,
            labelText,
            textBox,
            browse,
            new Label
            {
                Text = helpText,
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 6, 0, 0)
            });
    }

    private Control CreatePathPickerGroup(
        string title,
        string labelText,
        TextBox textBox,
        Action browse,
        Control helpControl)
    {
        var group = CreateGroupBox(title);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Math.Max(142, Font.Height * 6)));
        row.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        PreparePathTextBox(textBox);
        row.Controls.Add(textBox, 0, 0);
        row.Controls.Add(CreateBrowseButton("찾아보기...", browse), 1, 0);
        layout.Controls.Add(row, 0, 1);

        helpControl.Margin = new Padding(0, 6, 0, 0);
        layout.Controls.Add(helpControl, 0, 2);
        group.Controls.Add(layout);
        return group;
    }

    private void RenderComponentsPage()
    {
        var layout = CreateContentLayout();
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var intro = new Label
        {
            Text = "설치할 구성 요소를 체크하거나 해제한 뒤 설치를 누르세요.",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.Controls.Add(intro, 0, 0);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var tree = CreateComponentsTree();
        body.Controls.Add(tree, 0, 0);
        body.SetRowSpan(tree, 3);

        var descriptionGroup = CreateGroupBox("설명");
        descriptionGroup.Dock = DockStyle.Fill;
        _componentDescription.Dock = DockStyle.Fill;
        _componentDescription.ForeColor = SystemColors.ControlText;
        _componentDescription.Padding = new Padding(8);
        descriptionGroup.Controls.Add(_componentDescription);
        body.Controls.Add(descriptionGroup, 1, 0);

        var destination = new Label
        {
            Text = "설치 위치: " + _installRootBox.Text.Trim(),
            AutoSize = true,
            Margin = new Padding(12, 8, 0, 0)
        };
        body.Controls.Add(destination, 1, 1);

        var runtime = new Label
        {
            Text = $"런타임: {PlayerRuntimeRoot}, {AiRuntimeRoot}",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(12, 4, 0, 0)
        };
        body.Controls.Add(runtime, 1, 2);

        layout.Controls.Add(body, 0, 1);
        _pagePanel.Controls.Add(layout);
        UpdateComponentDescription(tree.SelectedNode ?? _desktopShortcutNode);
    }

    private TreeView CreateComponentsTree()
    {
        var tree = new TreeView
        {
            Dock = DockStyle.Fill,
            CheckBoxes = true,
            HideSelection = false,
            BorderStyle = BorderStyle.Fixed3D,
            LabelEdit = false,
            ShowLines = true
        };

        _desktopShortcutNode = new TreeNode("바탕화면 바로가기") { Checked = _desktopShortcutNode?.Checked ?? true };
        _vcRedistsNode = new TreeNode("VC++ x86 런타임 설치") { Checked = _vcRedistsNode?.Checked ?? true };
        _javaNode = new TreeNode("OpenJDK 17 준비") { Checked = _javaNode?.Checked ?? true };
        _launchAfterInstallNode = new TreeNode("설치 후 런처 실행") { Checked = _launchAfterInstallNode?.Checked ?? true };

        var root = new TreeNode("Sparring") { Checked = true };
        root.Nodes.Add(_desktopShortcutNode);
        root.Nodes.Add(_vcRedistsNode);
        root.Nodes.Add(_javaNode);
        root.Nodes.Add(_launchAfterInstallNode);
        tree.Nodes.Add(root);
        root.ExpandAll();
        tree.SelectedNode = _desktopShortcutNode;
        tree.AfterSelect += (_, e) => UpdateComponentDescription(e.Node);
        tree.AfterCheck += (_, e) =>
        {
            if (e.Node == root && !root.Checked)
            {
                root.Checked = true;
            }
        };
        return tree;
    }

    private void UpdateComponentDescription(TreeNode? node)
    {
        _componentDescription.Text = node?.Text switch
        {
            "바탕화면 바로가기" => "바탕화면에 Sparring 바로가기를 만듭니다.",
            "VC++ x86 런타임 설치" => "일부 32비트 AI 봇 DLL/EXE 실행에 필요할 수 있는 Microsoft VC++ 런타임을 설치합니다.",
            "OpenJDK 17 준비" => "커스텀 단축키 MPQ 적용에 필요한 Java를 Sparring 설치 폴더 안에 준비합니다. 시스템 Java 설정은 바꾸지 않습니다.",
            "설치 후 런처 실행" => "설치가 끝나면 Sparring 런처를 바로 실행합니다.",
            _ => "Sparring 앱 파일과 내장 봇/맵 데이터를 설치합니다."
        };
    }

    private void RenderProgressPage()
    {
        var layout = CreateContentLayout();
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _statusLabel.AutoSize = false;
        _statusLabel.Height = 26;
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        _progressBar.Dock = DockStyle.Top;
        _progressBar.Height = 20;

        _logBox.Dock = DockStyle.Fill;
        _logBox.BackColor = SystemColors.Window;
        _logBox.ForeColor = SystemColors.WindowText;
        _logBox.BorderStyle = BorderStyle.Fixed3D;

        layout.Controls.Add(_statusLabel, 0, 0);
        layout.Controls.Add(_progressBar, 0, 1);
        layout.Controls.Add(_logBox, 0, 2);
        _pagePanel.Controls.Add(layout);
    }

    private static TableLayoutPanel CreateContentLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 0
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return layout;
    }

    private static GroupBox CreateGroupBox(string text)
    {
        return new GroupBox
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 18, 12, 12),
            Margin = new Padding(0, 0, 0, 14)
        };
    }

    private static void PreparePathTextBox(TextBox textBox)
    {
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        textBox.Margin = new Padding(0, 2, 12, 0);
        textBox.MinimumSize = new Size(120, textBox.Font.Height + 12);
    }

    private Button CreateBrowseButton(string text, Action browse)
    {
        var button = new Button
        {
            Text = text,
            Width = Math.Max(132, TextRenderer.MeasureText(text, Font).Width + 36),
            Height = Math.Max(32, Font.Height + 14),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        button.Click += (_, _) => browse();
        return button;
    }

    private void MoveBack()
    {
        if (_installing)
        {
            return;
        }

        if (_page == SetupPage.Components)
        {
            _page = SetupPage.Paths;
            RenderPage();
        }
    }

    private async Task MoveNextAsync()
    {
        if (_completed)
        {
            Close();
            return;
        }

        if (_failed)
        {
            Close();
            return;
        }

        if (_page == SetupPage.Paths)
        {
            if (!ValidatePathPage())
            {
                return;
            }

            _page = SetupPage.Components;
            RenderPage();
            return;
        }

        if (_page == SetupPage.Components)
        {
            _page = SetupPage.Progress;
            RenderPage();
            await InstallAsync();
        }
    }

    private bool ValidatePathPage()
    {
        if (string.IsNullOrWhiteSpace(_installRootBox.Text))
        {
            MessageBox.Show(this, "설치 폴더를 입력해 주세요.", "설치 폴더", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_starCraftSourceBox.Text))
        {
            MessageBox.Show(this, "StarCraft 1.16.1 원본 폴더를 선택해 주세요.", "StarCraft 원본", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var starCraftSource = Path.GetFullPath(_starCraftSourceBox.Text.Trim());
        var missing = StarCraftInstallation.MissingRequiredFiles(starCraftSource);
        if (missing.Count > 0)
        {
            MessageBox.Show(
                this,
                "StarCraft 1.16.1 원본 폴더가 올바르지 않습니다.\r\n\r\n" +
                "필수 파일: StarCraft.exe, stardat.mpq, broodat.mpq, patch_rt.mpq\r\n" +
                "누락 파일: " + string.Join(", ", missing),
                "StarCraft 원본 확인",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private void BrowseInstallRoot()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Sparring를 설치할 폴더를 선택하세요.",
            SelectedPath = _installRootBox.Text
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _installRootBox.Text = dialog.SelectedPath;
        }
    }

    private void BrowseStarCraftRoot()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "StarCraft 1.16.1 원본 폴더를 선택하세요.",
            SelectedPath = _starCraftSourceBox.Text
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _starCraftSourceBox.Text = dialog.SelectedPath;
        }
    }

    private async Task InstallAsync()
    {
        var installRoot = Path.GetFullPath(_installRootBox.Text.Trim());
        var starCraftSource = Path.GetFullPath(_starCraftSourceBox.Text.Trim());
        _installing = true;
        _completed = false;
        _failed = false;
        _logBox.Clear();
        RenderPage();

        PayloadExtraction? payload = null;
        IReadOnlyList<InstallationManifestEntry>? payloadManifest = null;
        try
        {
            SetProgress(2, "설치 파일을 준비합니다.");
            payload = ExtractEmbeddedPayload();
            payloadManifest = await Task.Run(() => InstallationVerifier.BuildManifest(payload.Root));

            await CopyDirectoryWithProgressAsync(payload.Root, installRoot, 8, 30, "앱 파일과 내장 데이터를 복사합니다.");

            SetProgress(31, "복사된 설치 파일을 검증합니다.");
            await VerifyPayloadCopyAsync(installRoot, payloadManifest);

            SetProgress(34, "설치 복구 데이터를 저장합니다.");
            await Task.Run(() => WriteRepairMetadata(
                installRoot,
                payload.Root,
                payloadManifest,
                starCraftSource,
                InstallJava));

            SetProgress(42, "선택 구성 요소를 설치합니다.");
            await InstallSelectedPrerequisitesAsync(installRoot, 42, 62);

            SetProgress(62, "바로가기를 구성합니다.");
            RemoveLegacyCmdLaunchers(installRoot);
            if (InstallDesktopShortcut)
            {
                CreateDesktopShortcut(installRoot);
            }

            CreateStartMenuShortcut(installRoot);

            SetProgress(72, "StarCraft/BWAPI 런타임을 구성합니다.");
            await RunRuntimeSetupAsync(installRoot, starCraftSource);

            SetProgress(90, "설치 결과를 최종 검증합니다.");
            await VerifyPayloadCopyAsync(installRoot, payloadManifest);
            VerifyRequiredInstalledFiles(installRoot);

            SetProgress(100, "설치가 완료되었습니다.");
            _completed = true;

            if (LaunchAfterInstall)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(installRoot, "Sparring.Client.exe"),
                    WorkingDirectory = installRoot,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            _failed = true;
            SetProgress(Math.Max(_progressBar.Value, 1), "설치에 실패했습니다.");
            Log(ex.ToString());
            MessageBox.Show(this, ex.Message, "설치 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (payload is { DeleteAfterInstall: true })
            {
                try
                {
                    Directory.Delete(payload.Root, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup only.
                }
            }

            _installing = false;
            RenderPage();
        }
    }

    private bool InstallDesktopShortcut => _desktopShortcutNode?.Checked ?? true;

    private bool InstallVcRedists => _vcRedistsNode?.Checked ?? true;

    private bool InstallJava => _javaNode?.Checked ?? true;

    private bool LaunchAfterInstall => _launchAfterInstallNode?.Checked ?? true;

    private static void WriteRepairMetadata(
        string installRoot,
        string payloadRoot,
        IReadOnlyList<InstallationManifestEntry> payloadManifest,
        string starCraftSource,
        bool installJava)
    {
        InstallationVerifier.SaveManifest(
            Path.Combine(installRoot, InstallationVerifier.ManifestFileName),
            payloadManifest);

        InstallationVerifier.SaveState(
            Path.Combine(installRoot, InstallationVerifier.StateFileName),
            new InstallationState(
                StarCraftSourceRoot: starCraftSource,
                InstallJava: installJava,
                InstalledAtUtc: DateTime.UtcNow));

        var cachePath = InstallationVerifier.RepairPayloadPath(installRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        if (File.Exists(cachePath))
        {
            File.Delete(cachePath);
        }

        ZipFile.CreateFromDirectory(payloadRoot, cachePath, CompressionLevel.Optimal, includeBaseDirectory: false);
    }

    private async Task VerifyPayloadCopyAsync(
        string installRoot,
        IReadOnlyList<InstallationManifestEntry> payloadManifest)
    {
        var issues = await Task.Run(() => InstallationVerifier.Verify(installRoot, payloadManifest));
        ThrowIfVerificationIssues("설치 파일", issues);
    }

    private void VerifyRequiredInstalledFiles(string installRoot)
    {
        var missing = InstallationVerifier.MissingRequiredRuntimeFiles(
            installRoot,
            PlayerRuntimeRoot,
            AiRuntimeRoot,
            InstallJava);
        if (missing.Count == 0)
        {
            Log("필수 파일 검증 완료.");
            return;
        }

        throw new InvalidOperationException(
            "설치 후 필수 파일 일부가 보이지 않습니다.\r\n\r\n" +
            FormatVerificationAdvice(missing));
    }

    private static void ThrowIfVerificationIssues(
        string targetName,
        IReadOnlyList<InstallationVerificationIssue> issues)
    {
        if (issues.Count == 0)
        {
            return;
        }

        var details = issues
            .Take(8)
            .Select(issue => $"{issue.Kind}: {issue.RelativePath}")
            .ToList();

        throw new InvalidOperationException(
            $"{targetName} 검증에 실패했습니다.\r\n\r\n" +
            FormatVerificationAdvice(details));
    }

    private static string FormatVerificationAdvice(IReadOnlyList<string> details)
    {
        var suffix = details.Count > 8 ? "\r\n..." : string.Empty;
        return
            "Windows Defender 또는 백신 프로그램이 파일을 격리/삭제했을 수 있습니다. " +
            "Windows 보안의 보호 기록에서 차단 항목을 확인하고, 신뢰한 릴리즈 파일이라면 복원 또는 허용 처리한 뒤 다시 설치해 주세요.\r\n\r\n" +
            "확인 대상:\r\n" + string.Join("\r\n", details.Select(item => "- " + item)) + suffix;
    }

    private static PayloadExtraction ExtractEmbeddedPayload()
    {
        var fallbackPayload = Path.Combine(AppContext.BaseDirectory, "payload");
        if (Directory.Exists(fallbackPayload))
        {
            return new PayloadExtraction(fallbackPayload, DeleteAfterInstall: false);
        }

        var externalPayloadZip = Path.Combine(AppContext.BaseDirectory, "payload.zip");
        if (File.Exists(externalPayloadZip))
        {
            return ExtractPayloadZip(File.OpenRead(externalPayloadZip));
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            throw new FileNotFoundException("Installer payload was not embedded.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException("Installer payload resource could not be opened.");
        return ExtractPayloadZip(stream);
    }

    private static PayloadExtraction ExtractPayloadZip(Stream stream)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "SparringSetup", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(tempRoot);
        return new PayloadExtraction(tempRoot, DeleteAfterInstall: true);
    }

    private Task CopyDirectoryWithProgressAsync(string sourceDirectory, string targetDirectory, int start, int end, string message)
    {
        return Task.Run(() => CopyDirectory(sourceDirectory, targetDirectory, (copiedBytes, totalBytes) =>
        {
            if (totalBytes <= 0)
            {
                SetProgress(end, message, writeLog: false);
                return;
            }

            var ratio = Math.Clamp((double)copiedBytes / totalBytes, 0, 1);
            var value = start + (int)Math.Round((end - start) * ratio);
            SetProgress(value, message, writeLog: false);
        }));
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory, Action<long, long>? progress = null)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, directory)));
        }

        var files = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .ToList();
        var totalBytes = files.Sum(file => file.Length);
        long copiedBytes = 0;
        progress?.Invoke(copiedBytes, totalBytes);

        foreach (var sourceFile in files)
        {
            var sourcePath = sourceFile.FullName;
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            var targetPath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            CopyFileWithProgress(sourcePath, targetPath, sourceFile.Length, bytes =>
            {
                copiedBytes += bytes;
                progress?.Invoke(copiedBytes, totalBytes);
            });
        }
    }

    private static void CopyFileWithProgress(string sourcePath, string targetPath, long sourceLength, Action<long> progress)
    {
        const int BufferSize = 1024 * 1024;
        using var source = File.OpenRead(sourcePath);
        using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize);
        var buffer = new byte[BufferSize];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            target.Write(buffer, 0, read);
            progress(read);
        }

        if (sourceLength == 0)
        {
            progress(0);
        }
    }

    private static void RemoveLegacyCmdLaunchers(string installRoot)
    {
        foreach (var path in new[] { LegacyCmdLauncherPath, Path.Combine(installRoot, "Start-Sparring.cmd") })
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Legacy cleanup is best-effort; shortcuts now target the EXE directly.
            }
        }
    }

    private static void CreateDesktopShortcut(string installRoot)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        CreateShortcut(Path.Combine(desktop, "Sparring.lnk"), installRoot);
    }

    private static void CreateStartMenuShortcut(string installRoot)
    {
        var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var folder = Path.Combine(programs, "Sparring");
        Directory.CreateDirectory(folder);
        CreateShortcut(Path.Combine(folder, "Sparring.lnk"), installRoot);
    }

    private static void CreateShortcut(string shortcutPath, string installRoot)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows shortcut service is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = Path.Combine(installRoot, "Sparring.Client.exe");
        shortcut.WorkingDirectory = installRoot;
        shortcut.Description = "Sparring";
        shortcut.Save();
    }

    private async Task InstallSelectedPrerequisitesAsync(string installRoot, int start, int end)
    {
        if (!InstallVcRedists && !InstallJava)
        {
            Log("선택 구성 요소 설치를 건너뜁니다.");
            SetProgress(end, "선택 구성 요소 설치를 건너뜁니다.");
            return;
        }

        var current = start;
        if (InstallVcRedists)
        {
            var vcEnd = InstallJava ? start + ((end - start) / 2) : end;
            await InstallVcRedistsAsync(current, vcEnd);
            current = vcEnd;
        }

        if (InstallJava)
        {
            await InstallJavaRuntimeAsync(installRoot, current, end);
        }
    }

    private async Task InstallVcRedistsAsync(int start, int end)
    {
        Log("VC++ x86 런타임을 확인/설치합니다.");
        var packages = new[]
        {
            new RedistPackage("VC++ 2008 SP1 x86", VcRedist2008Url, "vc2008sp1_x86.exe", "/q /norestart"),
            new RedistPackage("VC++ 2010 SP1 x86", VcRedist2010Url, "vc2010sp1_x86.exe", "/q /norestart"),
            new RedistPackage("VC++ 2013 x86", VcRedist2013Url, "vc2013_x86.exe", "/install /quiet /norestart"),
            new RedistPackage("VC++ 2015-2022 x86", VcRedistCurrentUrl, "vc2015-2022_x86.exe", "/install /quiet /norestart")
        };

        for (var index = 0; index < packages.Length; index++)
        {
            var package = packages[index];
            var segmentStart = start + (int)Math.Round((end - start) * (index / (double)packages.Length));
            var segmentEnd = start + (int)Math.Round((end - start) * ((index + 1) / (double)packages.Length));
            var downloadEnd = segmentStart + Math.Max(1, (segmentEnd - segmentStart) / 2);
            SetProgress(segmentStart, $"{package.Name} 설치 파일을 준비합니다.");
            var installerPath = await DownloadDependencyAsync(
                package.Url,
                Path.Combine("vcredist", package.FileName),
                segmentStart,
                downloadEnd,
                $"{package.Name} 설치 파일을 다운로드합니다.");
            SetProgress(downloadEnd, $"{package.Name} 설치를 실행합니다.");
            await RunExternalProcessAsync(
                installerPath,
                package.Arguments,
                Path.GetDirectoryName(installerPath)!,
                [0, 1638, 3010]);
            SetProgress(segmentEnd, $"{package.Name} 설치가 완료되었습니다.");
        }
    }

    private async Task InstallJavaRuntimeAsync(string installRoot, int start, int end)
    {
        var targetRoot = Path.Combine(installRoot, "runtime", "jdk");
        var javaExe = Path.Combine(targetRoot, "bin", "java.exe");
        if (File.Exists(javaExe))
        {
            Log($"Java 런타임이 이미 준비되어 있습니다: {javaExe}");
            SetProgress(end, "Java 런타임이 이미 준비되어 있습니다.");
            return;
        }

        SetProgress(start, "OpenJDK 17 런타임을 준비합니다.");
        var downloadEnd = start + (int)Math.Round((end - start) * 0.45);
        var extractEnd = start + (int)Math.Round((end - start) * 0.65);
        var archivePath = await DownloadDependencyAsync(
            TemurinJdkUrl,
            Path.Combine("java", "temurin-jdk17-windows-x64.zip"),
            start,
            downloadEnd,
            "OpenJDK 17 런타임을 다운로드합니다.");
        var tempRoot = Path.Combine(Path.GetTempPath(), "SparringSetup", "jdk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            SetProgress(downloadEnd, "OpenJDK 17 런타임 압축을 풉니다.");
            await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, tempRoot));
            SetProgress(extractEnd, "OpenJDK 17 런타임 파일을 복사합니다.");
            var extractedJava = Directory
                .EnumerateFiles(tempRoot, "java.exe", SearchOption.AllDirectories)
                .FirstOrDefault(path => path.EndsWith(Path.Combine("bin", "java.exe"), StringComparison.OrdinalIgnoreCase))
                ?? throw new FileNotFoundException("Downloaded OpenJDK archive did not contain bin\\java.exe.", archivePath);

            var jdkRoot = Directory.GetParent(Path.GetDirectoryName(extractedJava)!)!.FullName;
            AssertSafeChildPath(targetRoot, installRoot);
            if (Directory.Exists(targetRoot))
            {
                Directory.Delete(targetRoot, recursive: true);
            }

            await CopyDirectoryWithProgressAsync(jdkRoot, targetRoot, extractEnd, end, "OpenJDK 17 런타임 파일을 복사합니다.");
            Log($"Java 런타임 준비 완료: {javaExe}");
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }

    private async Task<string> DownloadDependencyAsync(
        string url,
        string relativePath,
        int start,
        int end,
        string message)
    {
        var cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sparring",
            "deps",
            "installer");
        var destination = Path.Combine(cacheRoot, relativePath);
        if (File.Exists(destination))
        {
            Log($"캐시 사용: {destination}");
            SetProgress(end, message + " (캐시 사용)");
            return destination;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        Log($"다운로드: {url}");
        SetProgress(start, message);
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength;
        long downloadedBytes = 0;
        await using var source = await response.Content.ReadAsStreamAsync();
        await using var target = File.Create(destination);
        var buffer = new byte[1024 * 1024];
        int read;
        var lastProgress = start;
        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read));
            downloadedBytes += read;
            var progress = totalBytes is > 0
                ? start + (int)Math.Round((end - start) * Math.Clamp(downloadedBytes / (double)totalBytes.Value, 0, 1))
                : Math.Min(end - 1, start + (int)(downloadedBytes / (8 * 1024 * 1024)));
            if (progress != lastProgress)
            {
                lastProgress = progress;
                SetProgress(progress, message, writeLog: false);
            }
        }

        SetProgress(end, message);
        return destination;
    }

    private async Task RunExternalProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyCollection<int> allowedExitCodes)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.Start();
        var outputTask = PumpOutputAsync(process.StandardOutput);
        var errorTask = PumpOutputAsync(process.StandardError);
        await process.WaitForExitAsync();
        await Task.WhenAll(outputTask, errorTask);
        if (!allowedExitCodes.Contains(process.ExitCode))
        {
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} failed with exit code {process.ExitCode}.");
        }
    }

    private static void AssertSafeChildPath(string childPath, string rootPath)
    {
        var child = Path.GetFullPath(childPath);
        var root = Path.GetFullPath(rootPath);
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!child.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to modify path outside install root: {child}");
        }
    }

    private async Task RunRuntimeSetupAsync(string installRoot, string starCraftSource)
    {
        var scriptPath = Path.Combine(installRoot, "scripts", "setup-runtime.ps1");
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Runtime setup script was not installed.", scriptPath);
        }

        var arguments = string.Join(
            " ",
            "-NoProfile",
            "-ExecutionPolicy Bypass",
            "-File", Quote(scriptPath),
            "-AppRoot", Quote(installRoot),
            "-PlayerRuntimeRoot", Quote(PlayerRuntimeRoot),
            "-AiRuntimeRoot", Quote(AiRuntimeRoot),
            "-StarCraftSourceRoot", Quote(starCraftSource),
            "-NonInteractive");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = arguments,
                WorkingDirectory = installRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.Start();
        var outputTask = PumpOutputAsync(process.StandardOutput);
        var errorTask = PumpOutputAsync(process.StandardError);
        await process.WaitForExitAsync();
        await Task.WhenAll(outputTask, errorTask);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Runtime setup failed with exit code {process.ExitCode}.");
        }
    }

    private async Task PumpOutputAsync(StreamReader reader)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            Log(line);
        }
    }

    private void SetProgress(int value, string message, bool writeLog = true)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<int, string, bool>(SetProgress), value, message, writeLog);
            return;
        }

        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = Math.Clamp(value, _progressBar.Minimum, _progressBar.Maximum);
        _statusLabel.Text = message;
        if (writeLog)
        {
            Log(message);
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(Log), message);
            return;
        }

        if (_logBox.IsDisposed)
        {
            return;
        }

        _logBox.AppendText(message + Environment.NewLine);
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private enum SetupPage
    {
        Paths,
        Components,
        Progress
    }

    private sealed record PayloadExtraction(string Root, bool DeleteAfterInstall);

    private sealed record RedistPackage(string Name, string Url, string FileName, string Arguments);
}
