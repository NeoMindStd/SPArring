using Sparring.Client;
using Sparring.Core;

namespace Sparring.Tests;

public sealed class SmokeStartCatalogTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void ShouldInitializeWinFormsOnlyForRuntimeStart(bool dryRun, bool prepareOnly, bool expected)
    {
        Assert.Equal(expected, SmokeChecks.ShouldInitializeWinFormsForStart(dryRun, prepareOnly));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShouldRunRuntimeCleanupOnlyAfterRuntimeLaunchStarts(bool runtimeLaunchStarted)
    {
        Assert.Equal(runtimeLaunchStarted, SmokeChecks.ShouldRunRuntimeCleanupForStart(runtimeLaunchStarted));
    }

    [Fact]
    public void LoadSmokeStartCatalogCanSkipConfiguredMapDirectories()
    {
        using var workspace = new TempWorkspace();
        var userMapRoot = Path.Combine(workspace.Root, "user-maps");
        var ladderMapRoot = Path.Combine(workspace.Root, "ladder-maps");
        Directory.CreateDirectory(userMapRoot);
        Directory.CreateDirectory(ladderMapRoot);
        File.WriteAllText(Path.Combine(userMapRoot, "Injected Custom.scx"), "map");
        File.WriteAllText(Path.Combine(ladderMapRoot, "(4)Fighting Spirit 1.4.scx"), "map");

        var settings = new SparringSettings(
            ReplayRoot: Path.Combine(workspace.Root, "replays"),
            UserMapRoot: userMapRoot,
            LadderMapRoot: ladderMapRoot);
        var paths = PracticePaths.ForApplicationRoot(FindRepositoryRoot());

        var configuredCatalog = SmokeChecks.LoadSmokeStartCatalog(paths, settings);
        Assert.Contains(configuredCatalog.Maps, map => map.Name == "Injected Custom" && map.IsUserMap);
        Assert.Contains(configuredCatalog.Maps, map => map.Name.Contains("[Remastered Ladder]", StringComparison.Ordinal));

        var bundledCatalog = SmokeChecks.LoadSmokeStartCatalog(paths, settings, includeConfiguredMaps: false);
        Assert.DoesNotContain(bundledCatalog.Maps, map => map.Name == "Injected Custom");
        Assert.DoesNotContain(bundledCatalog.Maps, map => map.Name.Contains("[Remastered Ladder]", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sparring.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "data")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Sparring repository root.");
    }

    private sealed class TempWorkspace : IDisposable
    {
        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "Sparring.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
                // Test cleanup is best-effort.
            }
        }
    }
}
