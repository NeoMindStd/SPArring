using Sparring.Client;

namespace Sparring.Tests;

public sealed class SmokeAiActivityLogSnapshotTests
{
    [Fact]
    public void AppendedProductionLineIsMeaningfulActivity()
    {
        var writeDirectory = CreateTempWriteDirectory();
        var logPath = Path.Combine(writeDirectory, "game_log_SparringHuman.txt");
        File.WriteAllText(
            logPath,
            """
            1782943422 0(0:00): Microwave (v2.39) vs SparringHuman on (4)Jade.scx
            1782943423 0(0:00): Strategy: 3HatchLingBust  games: 0  winrate: 0

            """);
        var baseline = SmokeAiActivityLogSnapshot.Capture(writeDirectory);

        File.AppendAllText(
            logPath,
            """
            1782943423 0(0:00): Started morping Zerg_Drone
            1782943508 1266(0:52): Started morping Zerg_Overlord

            """);

        var summary = SmokeAiActivityLogSnapshot.Capture(writeDirectory).FindActivitySince(baseline);

        Assert.True(summary.HasMeaningfulActivity);
        Assert.Equal(2, summary.MeaningfulLineCount);
        Assert.Contains("game_log_SparringHuman.txt", summary.FormatFiles());
    }

    [Fact]
    public void ExistingProductionLineBeforeSnapshotIsIgnored()
    {
        var writeDirectory = CreateTempWriteDirectory();
        File.WriteAllText(
            Path.Combine(writeDirectory, "game_log_SparringHuman.txt"),
            """
            1782943423 0(0:00): Started morping Zerg_Drone

            """);
        var baseline = SmokeAiActivityLogSnapshot.Capture(writeDirectory);

        var summary = SmokeAiActivityLogSnapshot.Capture(writeDirectory).FindActivitySince(baseline);

        Assert.False(summary.HasMeaningfulActivity);
        Assert.Equal(0, summary.MeaningfulLineCount);
    }

    [Fact]
    public void AppendedStrategyOnlyLineIsNotMeaningfulActivity()
    {
        var writeDirectory = CreateTempWriteDirectory();
        var logPath = Path.Combine(writeDirectory, "game_log_SparringHuman.txt");
        File.WriteAllText(logPath, "");
        var baseline = SmokeAiActivityLogSnapshot.Capture(writeDirectory);

        File.AppendAllText(
            logPath,
            """
            1782943422 0(0:00): Initial Expected Plan: Unknown
            1782943423 0(0:00): Strategy: 3HatchLingBust  games: 0  winrate: 0

            """);

        var summary = SmokeAiActivityLogSnapshot.Capture(writeDirectory).FindActivitySince(baseline);

        Assert.False(summary.HasMeaningfulActivity);
        Assert.True(summary.NewLineCount >= 2);
    }

    private static string CreateTempWriteDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Sparring.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
