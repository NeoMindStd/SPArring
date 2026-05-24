namespace StarAI.PracticeClient.Core;

public static class StarCraftRuntimeRoot
{
    public static string GetAiRoot(string playerRoot)
    {
        var normalized = Path.GetFullPath(playerRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return normalized + "_ai";
    }

    public static string EnsureAiRoot(string playerRoot)
    {
        var sourceRoot = Path.GetFullPath(playerRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var aiRoot = GetAiRoot(sourceRoot);
        Directory.CreateDirectory(aiRoot);

        foreach (var sourcePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, sourcePath);
            if (ShouldSkip(relativePath))
            {
                continue;
            }

            var targetPath = Path.Combine(aiRoot, relativePath);
            CopyIfDifferent(sourcePath, targetPath);
        }

        return aiRoot;
    }

    private static bool ShouldSkip(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        return normalized.EndsWith(".rep", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/write/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/logs/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/errors/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("errors/", StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyIfDifferent(string sourcePath, string targetPath)
    {
        var source = new FileInfo(sourcePath);
        var target = new FileInfo(targetPath);
        if (target.Exists &&
            target.Length == source.Length &&
            target.LastWriteTimeUtc >= source.LastWriteTimeUtc)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        try
        {
            File.Copy(sourcePath, targetPath, overwrite: true);
            File.SetLastWriteTimeUtc(targetPath, source.LastWriteTimeUtc);
        }
        catch (IOException) when (target.Exists)
        {
            // StarCraft keeps MPQ files open while a client is running. If the AI
            // runtime already has a copy, keep using it instead of blocking launch.
        }
        catch (UnauthorizedAccessException) when (target.Exists)
        {
            // Same fallback as above for file locks reported as access errors.
        }
    }
}
