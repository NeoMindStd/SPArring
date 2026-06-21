using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class JavaRuntimeResolverTests
{
    [Fact]
    public void CandidatePathsIncludeBundledRuntimeUnderApplicationRoot()
    {
        var paths = PracticePaths.ForApplicationRoot(@"D:\Games\StarAI Practice");

        var candidates = JavaRuntimeResolver.BuildCandidatePaths(paths);

        Assert.Contains(@"D:\Games\StarAI Practice\runtime\jdk\bin\java.exe", candidates);
    }
}
