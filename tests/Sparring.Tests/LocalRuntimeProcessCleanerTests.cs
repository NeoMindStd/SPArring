using Sparring.Core;

namespace Sparring.Tests;

public sealed class LocalRuntimeProcessCleanerTests
{
    [Fact]
    public void IsLocalRuntimeProcessMatchesOnlySparringRuntimeProcesses()
    {
        var roots = new[] { @"C:\sparring\SC116AI", @"C:\sparring\SC116AI_ai" };

        Assert.True(LocalRuntimeProcessCleaner.IsLocalRuntimeProcess(
            "StarCraft",
            @"C:\sparring\SC116AI\StarCraft.exe",
            roots));
        Assert.True(LocalRuntimeProcessCleaner.IsLocalRuntimeProcess(
            "Chaoslauncher - MultiInstance",
            @"C:\sparring\SC116AI_ai\Chaoslauncher - MultiInstance.exe",
            roots));
        Assert.False(LocalRuntimeProcessCleaner.IsLocalRuntimeProcess(
            "StarCraft",
            @"C:\Program Files (x86)\StarCraft\StarCraft.exe",
            roots));
        Assert.False(LocalRuntimeProcessCleaner.IsLocalRuntimeProcess(
            "StarCraft",
            null,
            roots));
        Assert.False(LocalRuntimeProcessCleaner.IsLocalRuntimeProcess(
            "Battle.net",
            @"C:\sparring\SC116AI\Battle.net.exe",
            roots));
    }

    [Fact]
    public void IsKnownLaunchedRuntimeProcessMatchesOnlyCapturedTargetPids()
    {
        var knownProcessIds = new[] { 1234, 5678 };

        Assert.True(LocalRuntimeProcessCleaner.IsKnownLaunchedRuntimeProcess(
            "StarCraft",
            1234,
            knownProcessIds));
        Assert.True(LocalRuntimeProcessCleaner.IsKnownLaunchedRuntimeProcess(
            "Chaoslauncher - MultiInstance",
            5678,
            knownProcessIds));
        Assert.False(LocalRuntimeProcessCleaner.IsKnownLaunchedRuntimeProcess(
            "StarCraft",
            9999,
            knownProcessIds));
        Assert.False(LocalRuntimeProcessCleaner.IsKnownLaunchedRuntimeProcess(
            "Battle.net",
            1234,
            knownProcessIds));
    }
}
