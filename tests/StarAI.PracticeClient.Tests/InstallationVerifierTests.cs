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
