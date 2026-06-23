using Sparring.Core;

namespace Sparring.Tests;

public sealed class ChaosLauncherLogTests
{
    [Fact]
    public void CountCompletedStartsCountsOnlyCompletedLaunchLines()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-chaos-log-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(
            ChaosLauncherLog.PathForRuntime(root),
            string.Join(Environment.NewLine, new[]
            {
                "Begin Startup",
                "Starting Starcraft completed",
                "ApplyPatch for W-MODE 1.02",
                "Starting Starcraft completed"
            }));

        Assert.Equal(2, ChaosLauncherLog.CountCompletedStarts(root));
    }
}
