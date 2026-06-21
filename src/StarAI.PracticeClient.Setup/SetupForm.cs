using StarAI.PracticeClient.Core;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace StarAI.PracticeClient.Setup;

internal sealed class SetupForm : Form
{
    private const string DefaultInstallRoot = @"C:\starai\StarAI.PracticeClient";
    private const string PlayerRuntimeRoot = @"C:\starai\SC116AI";
    private const string AiRuntimeRoot = @"C:\starai\SC116AI_ai";
    private const string TaskbarLauncherPath = @"C:\starai\Start-StarAI-PracticeClient.cmd";
    private const string StarCraftGuideUrl = "https://github.com/NeoMindStd/SPArring#starcraft-1161-%EC%A4%80%EB%B9%84";
    private const string VcRedist2008Url = "https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x86.exe";
    private const string VcRedist2010Url = "https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe";
    private const string VcRedist2013Url = "https://aka.ms/highdpimfc2013x86enu";
    private const string VcRedistCurrentUrl = "https://aka.ms/vs/17/release/vc_redist.x86.exe";
    private const string TemurinJdkUrl = "https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jdk/hotspot/normal/eclipse";

    private readonly TextBox _installRootBox = new() { Text = DefaultInstallRoot };
    private readonly TextBox _starCraftSourceBox = new();
    private readonly CheckBox _installVcRedistsBox = new() { Text = "VC++ x86 런타임 설치 (권장)", Checked = true, AutoSize = true };
    private readonly CheckBox _installJavaBox = new() { Text = "Java 런타임 준비 (핫키용)", Checked = true, AutoSize = true };
    private readonly CheckBox _desktopShortcutBox = new() { Text = "바탕화면 바로가기 만들기", Checked = true, AutoSize = true };
    private readonly CheckBox _launchAfterInstallBox = new() { Text = "설치 후 StarAI Practice Client 실행", Checked = true, AutoSize = true };
    private readonly Button _installButton = new() { Text = "설치" };
    private readonly Button _cancelButton = new() { Text = "닫기" };
    private readonly TextBox _logBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

    public SetupForm()
    {
        Text = "StarAI Practice Client 설치";
        MinimumSize = new Size(800, 660);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(14, 18, 14);
        ForeColor = Color.FromArgb(190, 255, 140);

        var title = new Label
        {
            Text = "StarAI Practice Client 설치",
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        var description = new Label
        {
            Text = "StarCraft 1.16.1 원본 폴더를 읽어 사람/AI 런타임을 분리 구성합니다. 원본 폴더는 수정하지 않습니다.",
            AutoSize = true,
            ForeColor = Color.FromArgb(160, 230, 120)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 3,
            RowCount = 10
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(title, 0, 0);
        layout.SetColumnSpan(title, 3);
        layout.Controls.Add(description, 0, 1);
        layout.SetColumnSpan(description, 3);

        var prerequisitePanel = CreatePrerequisitePanel();
        layout.Controls.Add(prerequisitePanel, 0, 3);
        layout.SetColumnSpan(prerequisitePanel, 3);

        AddPathRow(layout, 4, "설치 경로", _installRootBox, "찾기", BrowseInstallRoot);
        AddPathRow(layout, 5, "StarCraft 1.16.1", _starCraftSourceBox, "찾기", BrowseStarCraftRoot);

        var link = new LinkLabel
        {
            Text = "StarCraft 1.16.1 준비 안내 열기",
            AutoSize = true,
            LinkColor = Color.FromArgb(120, 220, 255),
            ActiveLinkColor = Color.White,
            VisitedLinkColor = Color.FromArgb(120, 220, 255)
        };
        link.LinkClicked += (_, _) => OpenUrl(StarCraftGuideUrl);
        layout.Controls.Add(new Label(), 0, 6);
        layout.Controls.Add(link, 1, 6);
        layout.SetColumnSpan(link, 2);

        _logBox.BackColor = Color.Black;
        _logBox.ForeColor = Color.FromArgb(170, 255, 120);
        _logBox.BorderStyle = BorderStyle.FixedSingle;
        layout.Controls.Add(_logBox, 0, 7);
        layout.SetColumnSpan(_logBox, 3);

        var optionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        optionPanel.Controls.Add(_desktopShortcutBox);
        optionPanel.Controls.Add(_launchAfterInstallBox);
        layout.Controls.Add(optionPanel, 0, 8);
        layout.SetColumnSpan(optionPanel, 3);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true };
        buttonPanel.Controls.Add(_installButton);
        buttonPanel.Controls.Add(_cancelButton);
        layout.Controls.Add(buttonPanel, 0, 9);
        layout.SetColumnSpan(buttonPanel, 3);

        Controls.Add(layout);

        _installButton.Click += async (_, _) => await InstallAsync();
        _cancelButton.Click += (_, _) => Close();
    }

    private Control CreatePrerequisitePanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 4, 0, 8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new Label
        {
            Text = "선택 구성요소",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold)
        };
        panel.Controls.Add(heading, 0, 0);
        panel.SetColumnSpan(heading, 2);

        panel.Controls.Add(_installVcRedistsBox, 0, 1);
        panel.Controls.Add(CreateInfoLabel("미설치 시 일부 32비트 DLL/EXE 봇이 조용히 로드 실패할 수 있습니다. Microsoft 공식 VC++ x86 런타임을 설치합니다."), 1, 1);

        panel.Controls.Add(_installJavaBox, 0, 2);
        panel.Controls.Add(CreateInfoLabel("미설치 시 커스텀 단축키 MPQ 반영을 할 수 없습니다. 앱 폴더 안에 OpenJDK를 준비하며 시스템 Java는 바꾸지 않습니다. .NET 런타임은 설치 파일에 포함되어 별도 설치가 필요 없습니다."), 1, 2);

        return panel;
    }

    private Label CreateInfoLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            ForeColor = Color.FromArgb(160, 230, 120)
        };
    }

    private void AddPathRow(
        TableLayoutPanel layout,
        int row,
        string label,
        TextBox textBox,
        string buttonText,
        Action browse)
    {
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        textBox.BackColor = Color.Black;
        textBox.ForeColor = Color.FromArgb(180, 255, 140);
        textBox.BorderStyle = BorderStyle.FixedSingle;

        var browseButton = new Button { Text = buttonText, Dock = DockStyle.Fill };
        browseButton.Click += (_, _) => browse();

        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(textBox, 1, row);
        layout.Controls.Add(browseButton, 2, row);
    }

    private void BrowseInstallRoot()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "StarAI Practice Client를 설치할 폴더를 선택하세요.",
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
        var missing = StarCraftInstallation.MissingRequiredFiles(starCraftSource);
        if (missing.Count > 0)
        {
            MessageBox.Show(
                this,
                "StarCraft 1.16.1 폴더가 올바르지 않습니다.\r\n누락 파일: " + string.Join(", ", missing),
                "StarCraft 폴더 확인",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _installButton.Enabled = false;
        _cancelButton.Enabled = false;
        _logBox.Clear();

        PayloadExtraction? payload = null;
        try
        {
            Log("설치 파일을 준비합니다.");
            payload = ExtractEmbeddedPayload();

            Log($"앱 파일 복사: {installRoot}");
            CopyDirectory(payload.Root, installRoot);

            await InstallSelectedPrerequisitesAsync(installRoot);

            CreateLaunchers(installRoot);
            if (_desktopShortcutBox.Checked)
            {
                CreateDesktopShortcut(installRoot);
            }
            CreateStartMenuShortcut(installRoot);

            Log("StarCraft/BWAPI 런타임을 구성합니다.");
            await RunRuntimeSetupAsync(installRoot, starCraftSource);

            Log("설치가 완료되었습니다.");
            if (_launchAfterInstallBox.Checked)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(installRoot, "StarAI.PracticeClient.App.exe"),
                    WorkingDirectory = installRoot,
                    UseShellExecute = true
                });
            }

            MessageBox.Show(this, "설치가 완료되었습니다.", "StarAI Practice Client", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
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

            _installButton.Enabled = true;
            _cancelButton.Enabled = true;
        }
    }

    private static PayloadExtraction ExtractEmbeddedPayload()
    {
        var fallbackPayload = Path.Combine(AppContext.BaseDirectory, "payload");
        if (Directory.Exists(fallbackPayload))
        {
            return new PayloadExtraction(fallbackPayload, DeleteAfterInstall: false);
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            throw new FileNotFoundException("Installer payload was not embedded.");
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "StarAIPracticeClientSetup", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException("Installer payload resource could not be opened.");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(tempRoot);
        return new PayloadExtraction(tempRoot, DeleteAfterInstall: true);
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, directory)));
        }

        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            var targetPath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(sourcePath, targetPath, overwrite: true);
        }
    }

    private static void CreateLaunchers(string installRoot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TaskbarLauncherPath)!);
        var launcherText = "@echo off\r\n" +
                           $"start \"StarAI Practice Client\" \"{Path.Combine(installRoot, "StarAI.PracticeClient.App.exe")}\"\r\n";
        File.WriteAllText(TaskbarLauncherPath, launcherText, Encoding.Default);
        File.WriteAllText(Path.Combine(installRoot, "Start-StarAI-PracticeClient.cmd"), launcherText, Encoding.Default);
    }

    private static void CreateDesktopShortcut(string installRoot)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        CreateShortcut(Path.Combine(desktop, "StarAI Practice Client.lnk"), installRoot);
    }

    private static void CreateStartMenuShortcut(string installRoot)
    {
        var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var folder = Path.Combine(programs, "StarAI Practice Client");
        Directory.CreateDirectory(folder);
        CreateShortcut(Path.Combine(folder, "StarAI Practice Client.lnk"), installRoot);
    }

    private static void CreateShortcut(string shortcutPath, string installRoot)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows shortcut service is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = Path.Combine(installRoot, "StarAI.PracticeClient.App.exe");
        shortcut.WorkingDirectory = installRoot;
        shortcut.Description = "StarAI Practice Client";
        shortcut.Save();
    }

    private async Task InstallSelectedPrerequisitesAsync(string installRoot)
    {
        if (!_installVcRedistsBox.Checked && !_installJavaBox.Checked)
        {
            Log("선택 구성요소 설치를 건너뜁니다.");
            return;
        }

        if (_installVcRedistsBox.Checked)
        {
            await InstallVcRedistsAsync();
        }

        if (_installJavaBox.Checked)
        {
            await InstallJavaRuntimeAsync(installRoot);
        }
    }

    private async Task InstallVcRedistsAsync()
    {
        Log("VC++ x86 런타임을 확인/설치합니다.");
        var packages = new[]
        {
            new RedistPackage("VC++ 2008 SP1 x86", VcRedist2008Url, "vc2008sp1_x86.exe", "/q /norestart"),
            new RedistPackage("VC++ 2010 SP1 x86", VcRedist2010Url, "vc2010sp1_x86.exe", "/q /norestart"),
            new RedistPackage("VC++ 2013 x86", VcRedist2013Url, "vc2013_x86.exe", "/install /quiet /norestart"),
            new RedistPackage("VC++ 2015-2022 x86", VcRedistCurrentUrl, "vc2015-2022_x86.exe", "/install /quiet /norestart")
        };

        foreach (var package in packages)
        {
            var installerPath = await DownloadDependencyAsync(package.Url, Path.Combine("vcredist", package.FileName));
            Log($"{package.Name} 설치를 실행합니다.");
            await RunExternalProcessAsync(
                installerPath,
                package.Arguments,
                Path.GetDirectoryName(installerPath)!,
                [0, 1638, 3010]);
        }
    }

    private async Task InstallJavaRuntimeAsync(string installRoot)
    {
        var targetRoot = Path.Combine(installRoot, "runtime", "jdk");
        var javaExe = Path.Combine(targetRoot, "bin", "java.exe");
        if (File.Exists(javaExe))
        {
            Log($"Java 런타임이 이미 준비되어 있습니다: {javaExe}");
            return;
        }

        Log("OpenJDK 17 런타임을 앱 폴더에 준비합니다.");
        var archivePath = await DownloadDependencyAsync(TemurinJdkUrl, Path.Combine("java", "temurin-jdk17-windows-x64.zip"));
        var tempRoot = Path.Combine(Path.GetTempPath(), "StarAIPracticeClientSetup", "jdk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            ZipFile.ExtractToDirectory(archivePath, tempRoot);
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

            CopyDirectory(jdkRoot, targetRoot);
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

    private async Task<string> DownloadDependencyAsync(string url, string relativePath)
    {
        var cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StarAI.PracticeClient",
            "deps",
            "installer");
        var destination = Path.Combine(cacheRoot, relativePath);
        if (File.Exists(destination))
        {
            Log($"캐시 사용: {destination}");
            return destination;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        Log($"다운로드: {url}");
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync();
        await using var target = File.Create(destination);
        await source.CopyToAsync(target);
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

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(Log), message);
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

    private sealed record PayloadExtraction(string Root, bool DeleteAfterInstall);

    private sealed record RedistPackage(string Name, string Url, string FileName, string Arguments);
}
