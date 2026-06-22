using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class InstallationVerifierTests
{
    [Fact]
    public void VerifyPassesForUnchangedCopy()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.CreateDirectory("source");
        var target = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(source, "StarAI.PracticeClient.App.exe"), "app");
        Directory.CreateDirectory(Path.Combine(source, "data", "bots"));
        File.WriteAllText(Path.Combine(source, "data", "bots", "bots.dat"), "bots");
        CopyDirectory(source, target);

        var manifest = InstallationVerifier.BuildManifest(source);
        var issues = InstallationVerifier.Verify(target, manifest);

        Assert.Empty(issues);
    }

    [Fact]
    public void VerifyReportsMissingFile()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.CreateDirectory("source");
        var target = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(source, "VERSION"), "1.3.2");

        var manifest = InstallationVerifier.BuildManifest(source);
        var issues = InstallationVerifier.Verify(target, manifest);

        var issue = Assert.Single(issues);
        Assert.Equal("VERSION", issue.RelativePath);
        Assert.Equal(InstallationVerificationIssueKind.Missing, issue.Kind);
    }

    [Fact]
    public void VerifyReportsHashMismatch()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.CreateDirectory("source");
        var target = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(source, "README.md"), "original");
        CopyDirectory(source, target);
        File.WriteAllText(Path.Combine(target, "README.md"), "changed");

        var manifest = InstallationVerifier.BuildManifest(source);
        var issues = InstallationVerifier.Verify(target, manifest);

        var issue = Assert.Single(issues);
        Assert.Equal("README.md", issue.RelativePath);
        Assert.Equal(InstallationVerificationIssueKind.HashMismatch, issue.Kind);
    }

    [Fact]
    public void ManifestCanBeSavedAndLoaded()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.CreateDirectory("source");
        File.WriteAllText(Path.Combine(source, "VERSION"), "1.3.4");
        Directory.CreateDirectory(Path.Combine(source, "data", "maps"));
        File.WriteAllText(Path.Combine(source, "data", "maps", "maps.dat"), "maps");
        var manifestPath = Path.Combine(workspace.CreateDirectory("target"), InstallationVerifier.ManifestFileName);

        var manifest = InstallationVerifier.BuildManifest(source);
        InstallationVerifier.SaveManifest(manifestPath, manifest);
        var loaded = InstallationVerifier.LoadManifest(manifestPath);

        Assert.Equal(manifest, loaded);
    }

    [Fact]
    public void ManifestExcludesRepairMetadata()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.CreateDirectory("source");
        File.WriteAllText(Path.Combine(source, "VERSION"), "1.3.4");
        File.WriteAllText(Path.Combine(source, InstallationVerifier.ManifestFileName), "manifest");
        File.WriteAllText(Path.Combine(source, InstallationVerifier.StateFileName), "state");
        Directory.CreateDirectory(Path.Combine(source, InstallationVerifier.RepairCacheDirectoryName));
        File.WriteAllText(
            Path.Combine(source, InstallationVerifier.RepairCacheDirectoryName, InstallationVerifier.RepairPayloadFileName),
            "payload");

        var manifest = InstallationVerifier.BuildManifest(source);

        var entry = Assert.Single(manifest);
        Assert.Equal("VERSION", entry.RelativePath);
    }

    [Fact]
    public void RepairFromPayloadZipRestoresOnlyRequestedFiles()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.CreateDirectory("source");
        var target = workspace.CreateDirectory("target");
        Directory.CreateDirectory(Path.Combine(source, "data", "bots"));
        File.WriteAllText(Path.Combine(source, "VERSION"), "1.3.4");
        File.WriteAllText(Path.Combine(source, "data", "bots", "bots.dat"), "bots");
        CopyDirectory(source, target);
        File.Delete(Path.Combine(target, "data", "bots", "bots.dat"));
        File.WriteAllText(Path.Combine(target, "VERSION"), "changed");
        var payloadZip = Path.Combine(workspace.CreateDirectory("cache"), "payload.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(source, payloadZip);
        var manifest = InstallationVerifier.BuildManifest(source);
        var issues = InstallationVerifier.Verify(target, manifest);

        var result = InstallationVerifier.RepairFromPayloadZip(target, payloadZip, issues);
        var remaining = InstallationVerifier.Verify(target, manifest);

        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(2, result.RestoredCount);
        Assert.Empty(result.SkippedRelativePaths);
        Assert.Empty(remaining);
        Assert.Equal("1.3.4", File.ReadAllText(Path.Combine(target, "VERSION")));
        Assert.Equal("bots", File.ReadAllText(Path.Combine(target, "data", "bots", "bots.dat")));
    }

    [Fact]
    public void RepairFromPayloadZipSkipsRunningExecutableWhenRequested()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.CreateDirectory("source");
        var target = workspace.CreateDirectory("target");
        File.WriteAllText(Path.Combine(source, "StarAI.PracticeClient.App.exe"), "app");
        var payloadZip = Path.Combine(workspace.CreateDirectory("cache"), "payload.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(source, payloadZip);
        var issues = new[]
        {
            new InstallationVerificationIssue(
                "StarAI.PracticeClient.App.exe",
                InstallationVerificationIssueKind.Missing,
                "File is missing.")
        };

        var result = InstallationVerifier.RepairFromPayloadZip(
            target,
            payloadZip,
            issues,
            relativePath => !string.Equals(relativePath, "StarAI.PracticeClient.App.exe", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(1, result.RequestedCount);
        Assert.Equal(0, result.RestoredCount);
        Assert.Equal("StarAI.PracticeClient.App.exe", Assert.Single(result.SkippedRelativePaths));
    }

    [Fact]
    public void MissingRequiredRuntimeFilesReportsRelativePaths()
    {
        using var workspace = TempWorkspace.Create();
        var appRoot = workspace.CreateDirectory("app");
        var playerRoot = workspace.CreateDirectory("player");
        var aiRoot = workspace.CreateDirectory("ai");
        File.WriteAllText(Path.Combine(appRoot, "StarAI.PracticeClient.App.exe"), "app");
        File.WriteAllText(Path.Combine(appRoot, "VERSION"), "1.3.4");
        File.WriteAllText(Path.Combine(appRoot, "README.md"), "readme");
        Directory.CreateDirectory(Path.Combine(appRoot, "scripts"));
        File.WriteAllText(Path.Combine(appRoot, "scripts", "setup-runtime.ps1"), "script");
        Directory.CreateDirectory(Path.Combine(appRoot, "data", "bots"));
        Directory.CreateDirectory(Path.Combine(appRoot, "data", "maps"));
        File.WriteAllText(Path.Combine(appRoot, "data", "bots", "bots.dat"), "bots");
        File.WriteAllText(Path.Combine(appRoot, "data", "maps", "maps.dat"), "maps");

        var missing = InstallationVerifier.MissingRequiredRuntimeFiles(appRoot, playerRoot, aiRoot, includeJava: false);

        Assert.Contains(Path.GetRelativePath(appRoot, Path.Combine(playerRoot, "StarCraft.exe")), missing);
        Assert.Contains(Path.GetRelativePath(appRoot, Path.Combine(aiRoot, "ddraw.dll")), missing);
        Assert.DoesNotContain("runtime\\jdk\\bin\\java.exe", missing);
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
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

    private sealed class TempWorkspace : IDisposable
    {
        private readonly string _root;

        private TempWorkspace(string root)
        {
            _root = root;
        }

        public static TempWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "starai-install-verifier", Guid.NewGuid().ToString("N"));
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
