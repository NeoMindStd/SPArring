using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class RuntimeWritePolicyTests
{
    [Fact]
    public void ValidateLayoutRejectsSharedPlayerAndAiRuntime()
    {
        var paths = SafePaths() with { AiRuntimeRoot = @"C:\starai\SC116AI" };

        var issues = RuntimeWritePolicy.ValidateLayout(paths);

        Assert.Contains(issues, issue => issue.Code == "runtime.same-root");
    }

    [Fact]
    public void CheckMutableRuntimeTargetAllowsPlayerAndAiRuntimeFiles()
    {
        var paths = SafePaths();

        var playerVerdict = RuntimeWritePolicy.CheckMutableRuntimeTarget(
            paths,
            @"C:\starai\SC116AI\bwapi-data\bwapi.ini");
        var aiVerdict = RuntimeWritePolicy.CheckMutableRuntimeTarget(
            paths,
            @"C:\starai\SC116AI_ai\bwapi-data\bwapi.ini");

        Assert.True(playerVerdict.Allowed);
        Assert.True(aiVerdict.Allowed);
    }

    [Fact]
    public void CheckMutableRuntimeTargetRejectsSchnailInstallFiles()
    {
        var verdict = RuntimeWritePolicy.CheckMutableRuntimeTarget(
            SafePaths(),
            @"C:\Program Files (x86)\SCHNAIL Client\bots\bots.dat");

        Assert.False(verdict.Allowed);
        Assert.Equal("target.protected-schnail", verdict.Code);
    }

    [Fact]
    public void CheckMutableRuntimeTargetRejectsBundledAssetFiles()
    {
        var verdict = RuntimeWritePolicy.CheckMutableRuntimeTarget(
            SafePaths(),
            @"C:\starai\StarAI.PracticeClient\data\bots\bots.dat");

        Assert.False(verdict.Allowed);
        Assert.Equal("target.protected-assets", verdict.Code);
    }

    [Fact]
    public void IsSameOrUnderUsesPathBoundaries()
    {
        Assert.False(RuntimeWritePolicy.IsSameOrUnder(
            @"C:\starai\SC116AI_backup\bwapi.ini",
            @"C:\starai\SC116AI"));
    }

    private static PracticePaths SafePaths()
    {
        return new PracticePaths(
            @"C:\starai\StarAI.PracticeClient",
            @"C:\starai\Start-StarAI-PracticeClient.cmd",
            @"C:\starai\SC116AI",
            @"C:\starai\SC116AI_ai",
            @"C:\Program Files (x86)\SCHNAIL Client");
    }
}
