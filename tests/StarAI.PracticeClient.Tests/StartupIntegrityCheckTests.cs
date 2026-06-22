using StarAI.PracticeClient.App;
using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class StartupIntegrityCheckTests
{
    [Fact]
    public void RunRepairsMissingPayloadFileFromCache()
    {
        using var workspace = TempWorkspace.Create();
        var appRoot = workspace.CreateDirectory("app");
        WriteMinimalAppPayload(appRoot);
        var manifest = InstallationVerifier.BuildManifest(appRoot);
        InstallationVerifier.SaveManifest(Path.Combine(appRoot, InstallationVerifier.ManifestFileName), manifest);
        var cachePath = InstallationVerifier.RepairPayloadPath(appRoot);
        var tempPayloadZip = Path.Combine(workspace.CreateDirectory("cache-source"), "payload.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(appRoot, tempPayloadZip);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        File.Copy(tempPayloadZip, cachePath);
        File.Delete(Path.Combine(appRoot, "data", "bots", "bots.dat"));
        var paths = PracticePaths.ForApplicationRoot(appRoot) with
        {
            PlayerRuntimeRoot = workspace.CreateDirectory("player"),
            AiRuntimeRoot = workspace.CreateDirectory("ai")
        };

        var report = StartupIntegrityCheck.Run(paths, _ => 0);

        Assert.True(report.ShouldNotify);
        Assert.NotNull(report.AppRepairResult);
        Assert.Empty(report.AppIssuesAfterRepair);
        Assert.True(File.Exists(Path.Combine(appRoot, "data", "bots", "bots.dat")));
    }

    [Fact]
    public void RunLeavesUnresolvedIssueWhenRepairCacheIsMissing()
    {
        using var workspace = TempWorkspace.Create();
        var appRoot = workspace.CreateDirectory("app");
        WriteMinimalAppPayload(appRoot);
        var manifest = InstallationVerifier.BuildManifest(appRoot);
        InstallationVerifier.SaveManifest(Path.Combine(appRoot, InstallationVerifier.ManifestFileName), manifest);
        File.Delete(Path.Combine(appRoot, "data", "maps", "maps.dat"));
        var paths = PracticePaths.ForApplicationRoot(appRoot) with
        {
            PlayerRuntimeRoot = workspace.CreateDirectory("player"),
            AiRuntimeRoot = workspace.CreateDirectory("ai")
        };

        var report = StartupIntegrityCheck.Run(paths, _ => 0);

        Assert.True(report.ShouldNotify);
        Assert.NotNull(report.AppRepairResult);
        Assert.NotEmpty(report.AppIssuesAfterRepair);
        Assert.Contains(report.AppIssuesAfterRepair, issue => issue.RelativePath == Path.Combine("data", "maps", "maps.dat"));
    }

    [Fact]
    public void RunInvokesRuntimeRepairWhenRuntimeFilesAreMissing()
    {
        using var workspace = TempWorkspace.Create();
        var appRoot = workspace.CreateDirectory("app");
        var playerRoot = workspace.CreateDirectory("SC116AI");
        var aiRoot = workspace.CreateDirectory("SC116AI_ai");
        WriteMinimalAppPayload(appRoot);
        WriteMinimalRuntime(playerRoot);
        WriteMinimalRuntime(aiRoot);
        File.Delete(Path.Combine(aiRoot, "ddraw.dll"));
        InstallationVerifier.SaveState(
            Path.Combine(appRoot, InstallationVerifier.StateFileName),
            new InstallationState(null, InstallJava: false, DateTime.UtcNow));
        var invoked = false;
        var paths = PracticePaths.ForApplicationRoot(appRoot) with
        {
            PlayerRuntimeRoot = playerRoot,
            AiRuntimeRoot = aiRoot
        };

        var report = StartupIntegrityCheck.Run(paths, request =>
        {
            invoked = true;
            File.WriteAllText(Path.Combine(request.AiRuntimeRoot, "ddraw.dll"), "ddraw");
            return 0;
        });

        Assert.True(invoked);
        Assert.True(report.RuntimeRepairAttempted);
        Assert.Equal(0, report.RuntimeRepairExitCode);
        Assert.Empty(report.RuntimeMissingAfterRepair);
    }

    [Fact]
    public void FormatUserMessageIncludesDefenderGuidance()
    {
        var report = new StartupIntegrityCheckReport(
            ManifestFound: true,
            AppIssuesBeforeRepair: [],
            AppRepairResult: null,
            AppIssuesAfterRepair: [],
            RuntimeMissingBeforeRepair: ["SC116AI\\ddraw.dll"],
            RuntimeRepairAttempted: false,
            RuntimeRepairExitCode: null,
            RuntimeMissingAfterRepair: ["SC116AI\\ddraw.dll"],
            Error: null);

        var message = StartupIntegrityCheck.FormatUserMessage(report);

        Assert.Contains("Windows Defender", message);
        Assert.Contains("보호 기록", message);
        Assert.Contains("C:\\starai\\SC116AI", message);
    }

    private static void WriteMinimalAppPayload(string appRoot)
    {
        File.WriteAllText(Path.Combine(appRoot, "StarAI.PracticeClient.App.exe"), "app");
        File.WriteAllText(Path.Combine(appRoot, "VERSION"), "1.3.4");
        File.WriteAllText(Path.Combine(appRoot, "README.md"), "readme");
        Directory.CreateDirectory(Path.Combine(appRoot, "scripts"));
        File.WriteAllText(Path.Combine(appRoot, "scripts", "setup-runtime.ps1"), "script");
        Directory.CreateDirectory(Path.Combine(appRoot, "data", "bots"));
        Directory.CreateDirectory(Path.Combine(appRoot, "data", "maps"));
        File.WriteAllText(Path.Combine(appRoot, "data", "bots", "bots.dat"), "bots");
        File.WriteAllText(Path.Combine(appRoot, "data", "maps", "maps.dat"), "maps");
    }

    private static void WriteMinimalRuntime(string root)
    {
        File.WriteAllText(Path.Combine(root, "StarCraft.exe"), "starcraft");
        File.WriteAllText(Path.Combine(root, "stardat.mpq"), "stardat");
        File.WriteAllText(Path.Combine(root, "broodat.mpq"), "broodat");
        File.WriteAllText(Path.Combine(root, "patch_rt.mpq"), "patch");
        File.WriteAllText(Path.Combine(root, "Chaoslauncher - MultiInstance.exe"), "chaos");
        Directory.CreateDirectory(Path.Combine(root, "Plugins"));
        File.WriteAllText(Path.Combine(root, "Plugins", "BWAPI_PluginInjector.bwl"), "bwapi plugin");
        Directory.CreateDirectory(Path.Combine(root, "bwapi-data", "TM"));
        File.WriteAllText(Path.Combine(root, "bwapi-data", "BWAPI.dll"), "bwapi");
        File.WriteAllText(Path.Combine(root, "bwapi-data", "bwapi.ini"), "ini");
        File.WriteAllText(Path.Combine(root, "bwapi-data", "TM", "TournamentModule.dll"), "tm");
        File.WriteAllText(Path.Combine(root, "ddraw.dll"), "ddraw");
    }

    private sealed class TempWorkspace : IDisposable
    {
        private readonly string _root;

        private TempWorkspace(string root)
        {
            _root = root;
        }

        public static TempWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "starai-startup-integrity", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TempWorkspace(root);
        }

        public string CreateDirectory(string name)
        {
            var path = Path.Combine(_root, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
