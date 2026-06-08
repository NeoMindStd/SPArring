namespace StarAI.PracticeClient.Core;

public sealed record PracticePaths(
    string RepositoryRoot,
    string TaskbarLauncherPath,
    string PlayerRuntimeRoot,
    string AiRuntimeRoot,
    string AssetRoot,
    string ReferenceSchnailRoot)
{
    public PracticePaths(
        string repositoryRoot,
        string taskbarLauncherPath,
        string playerRuntimeRoot,
        string aiRuntimeRoot,
        string referenceSchnailRoot)
        : this(
            repositoryRoot,
            taskbarLauncherPath,
            playerRuntimeRoot,
            aiRuntimeRoot,
            Path.Combine(repositoryRoot, "data"),
            referenceSchnailRoot)
    {
    }

    public static PracticePaths Defaults()
    {
        const string repositoryRoot = @"C:\starai\StarAI.PracticeClient";
        return new PracticePaths(
            RepositoryRoot: repositoryRoot,
            TaskbarLauncherPath: @"C:\starai\Start-StarAI-PracticeClient.cmd",
            PlayerRuntimeRoot: @"C:\starai\SC116AI",
            AiRuntimeRoot: @"C:\starai\SC116AI_ai",
            AssetRoot: Path.Combine(repositoryRoot, "data"),
            ReferenceSchnailRoot: @"C:\Program Files (x86)\SCHNAIL Client");
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

        if (IsSameOrUnder(paths.PlayerRuntimeRoot, paths.AssetRoot) ||
            IsSameOrUnder(paths.AiRuntimeRoot, paths.AssetRoot))
        {
            issues.Add(new PathPolicyIssue(
                "runtime.inside-assets",
                "Mutable StarCraft runtimes must not live inside the StarAI bundled asset folder."));
        }

        if (IsSameOrUnder(paths.PlayerRuntimeRoot, paths.ReferenceSchnailRoot) ||
            IsSameOrUnder(paths.AiRuntimeRoot, paths.ReferenceSchnailRoot))
        {
            issues.Add(new PathPolicyIssue(
                "runtime.inside-schnail",
                "Mutable StarCraft runtimes must not live inside reference SCHNAIL folders."));
        }

        return issues;
    }

    public static PathSafetyVerdict CheckMutableRuntimeTarget(PracticePaths paths, string targetPath)
    {
        if (IsSameOrUnder(targetPath, paths.AssetRoot))
        {
            return new PathSafetyVerdict(
                false,
                "target.protected-assets",
                "StarAI bundled asset files are read-only at runtime.");
        }

        if (IsSameOrUnder(targetPath, paths.ReferenceSchnailRoot))
        {
            return new PathSafetyVerdict(
                false,
                "target.protected-schnail",
                "Reference SCHNAIL files are read-only import sources.");
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
