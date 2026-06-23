using Sparring.Core;

namespace Sparring.Tests;

public sealed class PracticePathsTests
{
    [Fact]
    public void ForApplicationRootUsesSelectedInstallFolderForBundledAssets()
    {
        var paths = PracticePaths.ForApplicationRoot(@"D:\Games\Sparring Practice");

        Assert.Equal(@"D:\Games\Sparring Practice", paths.RepositoryRoot);
        Assert.Equal(@"D:\Games\Sparring Practice\data", paths.AssetRoot);
        Assert.Equal(@"C:\sparring\SC116AI", paths.PlayerRuntimeRoot);
        Assert.Equal(@"C:\sparring\SC116AI_ai", paths.AiRuntimeRoot);
    }

    [Fact]
    public void ResolveApplicationRootAcceptsInstallManifestWhenBundledDataIsMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-root-manifest", Guid.NewGuid().ToString("N"));
        var previous = Environment.GetEnvironmentVariable("SPARRING_ROOT");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, InstallationVerifier.ManifestFileName), "[]");
            Environment.SetEnvironmentVariable("SPARRING_ROOT", root);

            var resolved = PracticePaths.ResolveApplicationRoot();

            Assert.Equal(Path.GetFullPath(root), resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SPARRING_ROOT", previous);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void StarCraftInstallationRequiresClassic116Files()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-starcraft-root", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "StarCraft.exe"), string.Empty);
            File.WriteAllText(Path.Combine(root, "stardat.mpq"), string.Empty);
            File.WriteAllText(Path.Combine(root, "broodat.mpq"), string.Empty);

            var missing = StarCraftInstallation.MissingRequiredFiles(root);

            Assert.False(StarCraftInstallation.IsValidRoot(root));
            Assert.Equal(["patch_rt.mpq"], missing);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
