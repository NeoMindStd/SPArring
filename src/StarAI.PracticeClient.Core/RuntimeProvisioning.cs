using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public static partial class RuntimeProvisioner
{
    public static PracticeLaunchPlan PrepareRuntimeAssets(PracticeLaunchPlan plan)
    {
        Directory.CreateDirectory(plan.Player.RuntimeRoot);
        Directory.CreateDirectory(plan.Ai.RuntimeRoot);

        var playerMap = ProvisionMap(plan.Map, plan.Player.RuntimeRoot);
        _ = ProvisionMap(plan.Map, plan.Ai.RuntimeRoot);
        var botExecutable = ProvisionBot(plan.Bot, plan.Ai.RuntimeRoot);
        var aiModule = plan.Bot.UsesBwapiIniAiModule ? botExecutable.RelativeExecutablePath : string.Empty;

        var player = plan.Player with { MapFileName = playerMap.RelativeMapPath };
        var ai = plan.Ai with
        {
            AiModule = aiModule,
            BotExecutable = botExecutable.RelativeExecutablePath
        };

        return plan with
        {
            Player = player,
            Ai = ai
        };
    }

    public static string EnsureAiRoot(string playerRoot)
    {
        var sourceRoot = Path.GetFullPath(playerRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var aiRoot = sourceRoot + "_ai";
        Directory.CreateDirectory(aiRoot);

        foreach (var sourcePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, sourcePath);
            if (ShouldSkipRuntimeCopy(relativePath))
            {
                continue;
            }

            CopyIfDifferent(sourcePath, Path.Combine(aiRoot, relativePath));
        }

        return aiRoot;
    }

    public static ProvisionedMap ProvisionMap(PracticeMap map, string runtimeRoot)
    {
        if (string.IsNullOrWhiteSpace(map.SourcePath) || !File.Exists(map.SourcePath))
        {
            throw new FileNotFoundException("Selected map source file was not found.", map.SourcePath);
        }

        var fileName = map.IsUserMap
            ? $"{SafePathSegment(Path.GetFileNameWithoutExtension(map.FileName))}_{map.Id.ToString("N")[..8]}{Path.GetExtension(map.FileName)}"
            : Path.GetFileName(map.FileName);
        var relativePath = Path.Combine("maps", "StarAI", fileName);
        var targetPath = Path.Combine(runtimeRoot, relativePath);
        CopyIfDifferent(map.SourcePath, targetPath);
        return new ProvisionedMap(relativePath, targetPath);
    }

    public static ProvisionedBot ProvisionBot(PracticeBot bot, string aiRuntimeRoot)
    {
        if (string.IsNullOrWhiteSpace(bot.SourceDirectory) || !Directory.Exists(bot.SourceDirectory))
        {
            throw new DirectoryNotFoundException($"Selected bot source directory was not found: {bot.SourceDirectory}");
        }

        var safeName = SafePathSegment(bot.Name);
        var relativeDirectory = Path.Combine("bwapi-data", "AI", "StarAI", "Bots", safeName);
        var targetDirectory = Path.Combine(aiRuntimeRoot, relativeDirectory);
        Directory.CreateDirectory(targetDirectory);

        foreach (var sourcePath in Directory.EnumerateFiles(bot.SourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(bot.SourceDirectory, sourcePath);
            CopyIfDifferent(sourcePath, Path.Combine(targetDirectory, relative));
        }

        var relativeExecutable = Path.Combine(relativeDirectory, bot.ExecutableName);
        var executablePath = Path.Combine(aiRuntimeRoot, relativeExecutable);
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("Bot executable was not copied into the AI runtime.", executablePath);
        }

        return new ProvisionedBot(relativeDirectory, relativeExecutable, executablePath);
    }

    private static bool ShouldSkipRuntimeCopy(string relativePath)
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
        File.Copy(sourcePath, targetPath, overwrite: true);
        File.SetLastWriteTimeUtc(targetPath, source.LastWriteTimeUtc);
    }

    private static string SafePathSegment(string value)
    {
        var sanitized = UnsafePathCharsRegex().Replace(value, "_").Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "Bot" : sanitized;
    }

    [GeneratedRegex(@"[\\/:*?""<>|]")]
    private static partial Regex UnsafePathCharsRegex();
}

public sealed record ProvisionedMap(string RelativeMapPath, string FullMapPath);

public sealed record ProvisionedBot(string RelativeDirectory, string RelativeExecutablePath, string FullExecutablePath);
