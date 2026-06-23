using Sparring.Core;

namespace Sparring.Tests;

public sealed class JavaRuntimeResolverTests
{
    [Fact]
    public void CandidatePathsIncludeBundledRuntimeUnderApplicationRoot()
    {
        var paths = PracticePaths.ForApplicationRoot(@"D:\Games\Sparring Practice");

        var candidates = JavaRuntimeResolver.BuildCandidatePaths(paths);

        Assert.Contains(@"D:\Games\Sparring Practice\runtime\jdk\bin\java.exe", candidates);
    }
}
