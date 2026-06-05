using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class LocalRuntimeProcessCleanerTests
{
    [Fact]
    public void IsLocalRuntimeProcessMatchesOnlyStarAiRuntimeProcesses()
    {
        var roots = new[] { @"C:\starai\SC116AI", @"C:\starai\SC116AI_ai" };

        Assert.True(LocalRuntimeProcessCleaner.IsLocalRuntimeProcess(
            "StarCraft",
            @"C:\starai\SC116AI\StarCraft.exe",
            roots));
        Assert.True(LocalRuntimeProcessCleaner.IsLocalRuntimeProcess(
            "Chaoslauncher - MultiInstance",
            @"C:\starai\SC116AI_ai\Chaoslauncher - MultiInstance.exe",
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
            @"C:\starai\SC116AI\Battle.net.exe",
            roots));
    }
}
