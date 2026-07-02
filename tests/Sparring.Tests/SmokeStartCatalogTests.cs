using Sparring.Client;
using Sparring.Core;
using System.Drawing;

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
    public void WaitForPracticeOverlayRetriesTransientMisses()
    {
        var attempts = 0;
        var pumped = 0;

        var visible = SmokeChecks.WaitForPracticeOverlay(
            () => ++attempts >= 3,
            _ => pumped++,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(1));

        Assert.True(visible);
        Assert.Equal(3, attempts);
        Assert.True(pumped >= 2);
    }

    [Fact]
    public void ContainsPracticeOverlayDetectsTopLeftOverlayOnBrightTerrain()
    {
        using var bitmap = new Bitmap(1280, 960);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.FromArgb(166, 92, 38));
        FillRectangle(bitmap, 0, 18, 150, 48, Color.FromArgb(4, 8, 4));
        FillRectangle(bitmap, 20, 28, 95, 18, Color.FromArgb(166, 255, 126));

        Assert.True(SmokeChecks.ContainsPracticeOverlay(bitmap));
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

    [Fact]
    public void DryRunRejectsKnownBadPairByDefault()
    {
        var exitCode = SmokeChecks.RunStart([
            "--bundled-catalog-only",
            "--dry-run",
            "--mode",
            "Sparring",
            "--bot",
            "Locutus",
            "--map",
            "(4)Fighting Spirit"
        ]);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void DryRunCanSelectKnownBadPairWhenExplicitlyAllowedForVerification()
    {
        var exitCode = SmokeChecks.RunStart([
            "--bundled-catalog-only",
            "--dry-run",
            "--allow-incompatible",
            "--mode",
            "Sparring",
            "--bot",
            "Locutus",
            "--map",
            "(4)Fighting Spirit"
        ]);

        Assert.Equal(0, exitCode);
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

    private static void FillRectangle(Bitmap bitmap, int x, int y, int width, int height, Color color)
    {
        using var graphics = Graphics.FromImage(bitmap);
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, x, y, width, height);
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
