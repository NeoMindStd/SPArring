namespace StarAI.PracticeClient.Core;

public sealed record PracticePaths(
    string RepositoryRoot,
    string TaskbarLauncherPath,
    string PlayerRuntimeRoot,
    string AiRuntimeRoot,
    string SchnailRoot)
{
    public static PracticePaths Defaults()
    {
        return new PracticePaths(
            RepositoryRoot: @"C:\starai\StarAI.PracticeClient",
            TaskbarLauncherPath: @"C:\starai\Start-StarAI-PracticeClient.cmd",
            PlayerRuntimeRoot: @"C:\starai\SC116AI",
            AiRuntimeRoot: @"C:\starai\SC116AI_ai",
            SchnailRoot: @"C:\Program Files (x86)\SCHNAIL Client");
    }
}

public sealed record PathPolicyIssue(string Code, string Message);

public sealed record PathSafetyVerdict(bool Allowed, string Code, string Message);

public static class RuntimeWritePolicy
{
    public static IReadOnlyList<PathPolicyIssue> ValidateLayout(PracticePaths paths)
    {
        var issues = new List<PathPolicyIssue>();

        if (PathEquals(paths.PlayerRuntimeRoot, paths.AiRuntimeRoot))
        {
            issues.Add(new PathPolicyIssue(
                "runtime.same-root",
                "Player and AI runtimes must be separate directories."));
        }

        if (IsSameOrUnder(paths.PlayerRuntimeRoot, paths.SchnailRoot) ||
            IsSameOrUnder(paths.AiRuntimeRoot, paths.SchnailRoot))
        {
            issues.Add(new PathPolicyIssue(
                "runtime.inside-schnail",
                "Mutable StarCraft runtimes must not live inside the SCHNAIL install folder."));
        }

        return issues;
    }

    public static PathSafetyVerdict CheckMutableRuntimeTarget(PracticePaths paths, string targetPath)
    {
        if (IsSameOrUnder(targetPath, paths.SchnailRoot))
        {
            return new PathSafetyVerdict(
                false,
                "target.protected-schnail",
                "SCHNAIL install files are read-only reference data.");
        }

        if (IsSameOrUnder(targetPath, paths.PlayerRuntimeRoot) ||
            IsSameOrUnder(targetPath, paths.AiRuntimeRoot))
        {
            return new PathSafetyVerdict(true, "target.runtime", "Target is inside a mutable StarCraft runtime.");
        }

        return new PathSafetyVerdict(
            false,
            "target.outside-runtime",
            "Runtime writes are limited to the player and AI runtime folders.");
    }

    public static bool IsSameOrUnder(string candidatePath, string rootPath)
    {
        var candidate = Normalize(candidatePath);
        var root = Normalize(rootPath);

        if (PathEquals(candidate, root))
        {
            return true;
        }

        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathEquals(string left, string right)
    {
        return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path)
    {
        return Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
}
