using Sparring.Core;

namespace Sparring.Tests;

public sealed class RuntimeWritePolicyTests
{
    [Fact]
    public void ValidateLayoutRejectsSharedPlayerAndAiRuntime()
    {
        var paths = SafePaths() with { AiRuntimeRoot = @"C:\sparring\SC116AI" };

        var issues = RuntimeWritePolicy.ValidateLayout(paths);

        Assert.Contains(issues, issue => issue.Code == "runtime.same-root");
    }

    [Fact]
    public void CheckMutableRuntimeTargetAllowsPlayerAndAiRuntimeFiles()
    {
        var paths = SafePaths();

        var playerVerdict = RuntimeWritePolicy.CheckMutableRuntimeTarget(
            paths,
            @"C:\sparring\SC116AI\bwapi-data\bwapi.ini");
        var aiVerdict = RuntimeWritePolicy.CheckMutableRuntimeTarget(
            paths,
            @"C:\sparring\SC116AI_ai\bwapi-data\bwapi.ini");

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
            @"C:\sparring\Sparring\data\bots\bots.dat");

        Assert.False(verdict.Allowed);
        Assert.Equal("target.protected-assets", verdict.Code);
    }

    [Fact]
    public void IsSameOrUnderUsesPathBoundaries()
    {
        Assert.False(RuntimeWritePolicy.IsSameOrUnder(
            @"C:\sparring\SC116AI_backup\bwapi.ini",
            @"C:\sparring\SC116AI"));
    }

    private static PracticePaths SafePaths()
    {
        return new PracticePaths(
            @"C:\sparring\Sparring",
            @"C:\sparring\SC116AI",
            @"C:\sparring\SC116AI_ai",
            @"C:\Program Files (x86)\SCHNAIL Client");
    }
}
