using Sparring.Client;

namespace Sparring.Tests;

public sealed class RuntimeErrorLogSnapshotTests
{
    [Fact]
    public void IgnoresHumanRuntimeMissingAiModuleLog()
    {
        var path = Path.Combine(Path.GetTempPath(), "SparringTests", Guid.NewGuid().ToString("N"), "bwapi-error.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            """
            [2026/06/30 - 15:49:01] Could not find ai under ai in "C:\sparring\SC116AI\bwapi-data\bwapi.ini".
            [2026/06/30 - 15:50:01] Could not find ai under ai in "C:\sparring\SC116AI\bwapi-data\bwapi.ini".
            """);

        Assert.True(SmokeChecks.RuntimeErrorLogSnapshot.IsIgnorableRuntimeErrorFile(path));
    }

    [Fact]
    public void DoesNotIgnoreMixedBwapiErrorLog()
    {
        var path = Path.Combine(Path.GetTempPath(), "SparringTests", Guid.NewGuid().ToString("N"), "bwapi-error.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            """
            [2026/06/30 - 15:49:01] Could not find ai under ai in "C:\sparring\SC116AI\bwapi-data\bwapi.ini".
            [2026/06/30 - 15:50:01] Unable to load module Example.dll.
            """);

        Assert.False(SmokeChecks.RuntimeErrorLogSnapshot.IsIgnorableRuntimeErrorFile(path));
    }
}
