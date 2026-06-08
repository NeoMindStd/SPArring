using StarAI.PracticeClient.Core;
using System.Text;

namespace StarAI.PracticeClient.App;

internal static class CompatibilityAuditCommand
{
    public static int Run()
    {
        var paths = PracticePaths.Defaults();
        var settings = PracticeClientSettingsStore.Default().Load();
        var catalog = LoadCatalog(paths, settings);
        var report = PracticeCompatibilityAuditor.Audit(
            catalog,
            Path.Combine(paths.AiRuntimeRoot, "Errors"));
        var outputRoot = Path.Combine(paths.RepositoryRoot, "artifacts", "compatibility-audit");
        Directory.CreateDirectory(outputRoot);

        WritePairs(Path.Combine(outputRoot, "compatible-pairs.csv"), report.Pairs.Where(pair => pair.IsCompatible));
        WritePairs(Path.Combine(outputRoot, "blocked-declared-pairs.csv"), report.Pairs.Where(pair => !pair.IsCompatible));
        WriteIssues(Path.Combine(outputRoot, "issues.csv"), report.Issues);
        WriteCrashes(Path.Combine(outputRoot, "runtime-crashes.csv"), report.RuntimeCrashes);

        Console.WriteLine(
            "compatibility-audit: " +
            $"bots={report.BotCount}, dllBots={report.DllBotCount}, maps={report.MapCount}, " +
            $"declaredDllPairs={report.DeclaredDllPairCount}, compatibleDllPairs={report.CompatibleDllPairCount}, " +
            $"blockedDeclaredDllPairs={report.BlockedDeclaredDllPairCount}, issues={report.Issues.Count}, " +
            $"runtimeCrashes={report.RuntimeCrashes.Count}, output={outputRoot}");

        foreach (var issue in report.Issues.Take(20))
        {
            Console.WriteLine(
                $"compatibility-audit issue: {issue.Kind}, bot={issue.BotName}, map={issue.MapName}, evidence={issue.EvidencePath}");
        }

        return report.Issues.Count == 0 ? 0 : 1;
    }

    private static PracticeCatalog LoadCatalog(PracticePaths paths, PracticeClientSettings settings)
    {
        var schnailCatalog = SchnailCatalogReader.Read(paths.SchnailRoot);
        var ladderMapRoot = string.IsNullOrWhiteSpace(settings.LadderMapRoot)
            ? RemasteredLadderMapCatalogReader.DefaultDirectory()
            : settings.LadderMapRoot;
        var ladderMaps = RemasteredLadderMapCatalogReader.ReadDirectory(ladderMapRoot, schnailCatalog);
        var userMaps = UserMapCatalogReader.ReadDirectory(settings.UserMapRoot);
        return UserMapCatalogReader.Merge(
            UserMapCatalogReader.Merge(schnailCatalog, ladderMaps),
            userMaps);
    }

    private static void WritePairs(string path, IEnumerable<PracticeCompatibilityAuditPair> pairs)
    {
        var builder = new StringBuilder();
        builder.AppendLine("botName,botRace,botExecutable,botElo,mapName,mapFileName,isCompatible");
        foreach (var pair in pairs.OrderBy(pair => pair.BotName).ThenBy(pair => pair.MapName))
        {
            builder.AppendCsvLine(
                pair.BotName,
                pair.BotRace.ToString(),
                pair.BotExecutable,
                pair.BotElo?.ToString() ?? string.Empty,
                pair.MapName,
                pair.MapFileName,
                pair.IsCompatible.ToString());
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
    }

    private static void WriteIssues(string path, IEnumerable<PracticeCompatibilityAuditIssue> issues)
    {
        var builder = new StringBuilder();
        builder.AppendLine("kind,botName,mapName,message,evidencePath");
        foreach (var issue in issues.OrderBy(issue => issue.Kind).ThenBy(issue => issue.BotName).ThenBy(issue => issue.MapName))
        {
            builder.AppendCsvLine(
                issue.Kind.ToString(),
                issue.BotName,
                issue.MapName ?? string.Empty,
                issue.Message,
                issue.EvidencePath ?? string.Empty);
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
    }

    private static void WriteCrashes(string path, IEnumerable<RuntimeCrashEvidence> crashes)
    {
        var builder = new StringBuilder();
        builder.AppendLine("sourcePath,moduleName,botDirectoryName,mapName,mapFileName");
        foreach (var crash in crashes.OrderBy(crash => crash.ModuleName).ThenBy(crash => crash.MapFileName))
        {
            builder.AppendCsvLine(
                crash.SourcePath,
                crash.ModuleName,
                crash.BotDirectoryName ?? string.Empty,
                crash.MapName ?? string.Empty,
                crash.MapFileName ?? string.Empty);
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
    }

    private static void AppendCsvLine(this StringBuilder builder, params string[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains('"') &&
            !value.Contains(',') &&
            !value.Contains('\r') &&
            !value.Contains('\n'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
