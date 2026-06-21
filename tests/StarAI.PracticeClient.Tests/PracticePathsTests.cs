using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class PracticePathsTests
{
    [Fact]
    public void ForApplicationRootUsesSelectedInstallFolderForBundledAssets()
    {
        var paths = PracticePaths.ForApplicationRoot(@"D:\Games\StarAI Practice");

        Assert.Equal(@"D:\Games\StarAI Practice", paths.RepositoryRoot);
        Assert.Equal(@"D:\Games\StarAI Practice\data", paths.AssetRoot);
        Assert.Equal(@"C:\starai\SC116AI", paths.PlayerRuntimeRoot);
        Assert.Equal(@"C:\starai\SC116AI_ai", paths.AiRuntimeRoot);
    }

    [Fact]
    public void StarCraftInstallationRequiresClassic116Files()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-starcraft-root", Guid.NewGuid().ToString("N"));
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
