using System.Diagnostics;

namespace Sparring.Core;

public static class JavaRuntimeResolver
{
    public const string BundledJavaRelativePath = @"runtime\jdk\bin\java.exe";

    public static IReadOnlyList<string> BuildCandidatePaths(PracticePaths paths)
    {
        var candidates = new List<string>();
        AddIfPresent(candidates, Environment.GetEnvironmentVariable("SPARRING_JAVA_HOME"));
        AddIfPresent(candidates, Environment.GetEnvironmentVariable("JAVA_HOME"));
        candidates.Add(Path.Combine(paths.RepositoryRoot, BundledJavaRelativePath));
        candidates.Add("java.exe");
        candidates.Add(@"C:\Java\jdk-25.0.1\bin\java.exe");
        return candidates;
    }

    public static string ResolveJavaExe(PracticePaths paths)
    {
        foreach (var candidate in BuildCandidatePaths(paths))
        {
            if (CanRunJava(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Java 11+ runtime was not found. Install the Sparring Java runtime option or install Java manually to apply custom hotkeys.");
    }

    private static void AddIfPresent(List<string> candidates, string? javaHome)
    {
        if (string.IsNullOrWhiteSpace(javaHome))
        {
            return;
        }

        candidates.Add(Path.Combine(javaHome, "bin", "java.exe"));
    }

    private static bool CanRunJava(string candidate)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = candidate,
                Arguments = "-version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            process?.WaitForExit(3000);
            return process is { ExitCode: 0 };
        }
        catch
        {
            return false;
        }
    }
}
