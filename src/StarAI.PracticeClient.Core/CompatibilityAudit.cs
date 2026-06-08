using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public sealed record PracticeCompatibilityAuditReport(
    int BotCount,
    int DllBotCount,
    int MapCount,
    int DeclaredDllPairCount,
    int CompatibleDllPairCount,
    int BlockedDeclaredDllPairCount,
    IReadOnlyList<PracticeCompatibilityAuditPair> Pairs,
    IReadOnlyList<PracticeCompatibilityAuditIssue> Issues,
    IReadOnlyList<RuntimeCrashEvidence> RuntimeCrashes);

public sealed record PracticeCompatibilityAuditPair(
    string BotName,
    StarCraftRace BotRace,
    string BotExecutable,
    int? BotElo,
    string MapName,
    string MapFileName,
    bool IsCompatible);

public enum PracticeCompatibilityAuditIssueKind
{
    MissingBotSourceDirectory,
    MissingBotExecutable,
    MissingMapSource,
    RuntimeCrashEvidence
}

public sealed record PracticeCompatibilityAuditIssue(
    PracticeCompatibilityAuditIssueKind Kind,
    string BotName,
    string? MapName,
    string Message,
    string? EvidencePath);

public sealed record RuntimeCrashEvidence(
    string SourcePath,
    string ModuleName,
    string? BotDirectoryName,
    string? MapName,
    string? MapFileName);

public static class PracticeCompatibilityAuditor
{
    public static PracticeCompatibilityAuditReport Audit(PracticeCatalog catalog, string? errorDirectory)
    {
        var dllBots = catalog.Bots
            .Where(bot => bot.UsesBwapiIniAiModule)
            .OrderBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var maps = catalog.Maps
            .Where(map => map.Enabled)
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pairs = dllBots
            .SelectMany(bot => maps, (bot, map) => (bot, map))
            .Where(pair => IsDeclaredPair(pair.bot, pair.map))
            .Select(pair => new PracticeCompatibilityAuditPair(
                pair.bot.Name,
                pair.bot.Race,
                pair.bot.ExecutableName,
                pair.bot.Elo,
                pair.map.Name,
                pair.map.FileName,
                PracticeCatalogCompatibility.IsCompatible(catalog, pair.bot.Id, pair.map.Id)))
            .ToList();

        var issues = new List<PracticeCompatibilityAuditIssue>();
        AddBotFileIssues(dllBots, issues);
        AddMapFileIssues(maps, issues);

        var crashes = ReadRuntimeCrashes(errorDirectory);
        AddRuntimeCrashIssues(catalog, dllBots, maps, crashes, issues);

        return new PracticeCompatibilityAuditReport(
            BotCount: catalog.Bots.Count,
            DllBotCount: dllBots.Count,
            MapCount: maps.Count,
            DeclaredDllPairCount: pairs.Count,
            CompatibleDllPairCount: pairs.Count(pair => pair.IsCompatible),
            BlockedDeclaredDllPairCount: pairs.Count(pair => !pair.IsCompatible),
            Pairs: pairs,
            Issues: issues,
            RuntimeCrashes: crashes);
    }

    public static IReadOnlyList<RuntimeCrashEvidence> ReadRuntimeCrashes(string? errorDirectory)
    {
        if (string.IsNullOrWhiteSpace(errorDirectory) || !Directory.Exists(errorDirectory))
        {
            return [];
        }

        var crashes = new List<RuntimeCrashEvidence>();
        foreach (var path in Directory.EnumerateFiles(errorDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".ERR", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            crashes.AddRange(ParseRuntimeCrashEvidence(File.ReadAllText(path), path));
        }

        return crashes;
    }

    public static IReadOnlyList<RuntimeCrashEvidence> ParseRuntimeCrashEvidence(string text, string sourcePath)
    {
        var crashes = new List<RuntimeCrashEvidence>();
        string? mapName = null;
        string? mapFileName = null;
        var expectMapFile = false;

        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = CleanLine(rawLine);
            if (line.StartsWith("TIME:", StringComparison.OrdinalIgnoreCase))
            {
                mapName = null;
                mapFileName = null;
                expectMapFile = false;
                continue;
            }

            if (line.StartsWith("MAP:", StringComparison.OrdinalIgnoreCase))
            {
                mapName = line["MAP:".Length..].Trim();
                expectMapFile = true;
                continue;
            }

            if (expectMapFile && LooksLikeMapFile(line))
            {
                mapFileName = Path.GetFileName(line.Trim());
                expectMapFile = false;
                continue;
            }

            var fault = Regex.Match(line, @"FAULT:\s+0x[0-9A-Fa-f]+\s+(?<module>[^\s]+)");
            if (fault.Success)
            {
                var moduleValue = fault.Groups["module"].Value;
                crashes.Add(new RuntimeCrashEvidence(
                    sourcePath,
                    Path.GetFileName(moduleValue),
                    ExtractBotDirectoryName(moduleValue),
                    string.IsNullOrWhiteSpace(mapName) ? null : mapName,
                    string.IsNullOrWhiteSpace(mapFileName) ? null : mapFileName));
                continue;
            }

            var faultAddress = Regex.Match(line, @"Fault address:\s+.*?(?<path>[A-Z]:\\.*?\.dll)", RegexOptions.IgnoreCase);
            if (faultAddress.Success)
            {
                var modulePath = faultAddress.Groups["path"].Value;
                crashes.Add(new RuntimeCrashEvidence(
                    sourcePath,
                    Path.GetFileName(modulePath),
                    ExtractBotDirectoryName(modulePath),
                    string.IsNullOrWhiteSpace(mapName) ? null : mapName,
                    string.IsNullOrWhiteSpace(mapFileName) ? null : mapFileName));
            }
        }

        return crashes;
    }

    private static void AddBotFileIssues(
        IReadOnlyList<PracticeBot> bots,
        List<PracticeCompatibilityAuditIssue> issues)
    {
        foreach (var bot in bots)
        {
            if (string.IsNullOrWhiteSpace(bot.SourceDirectory) || !Directory.Exists(bot.SourceDirectory))
            {
                issues.Add(new PracticeCompatibilityAuditIssue(
                    PracticeCompatibilityAuditIssueKind.MissingBotSourceDirectory,
                    bot.Name,
                    null,
                    $"Bot source directory is missing: {bot.SourceDirectory}",
                    bot.SourceDirectory));
                continue;
            }

            var executablePath = Path.Combine(bot.SourceDirectory, bot.ExecutableName);
            if (!File.Exists(executablePath))
            {
                issues.Add(new PracticeCompatibilityAuditIssue(
                    PracticeCompatibilityAuditIssueKind.MissingBotExecutable,
                    bot.Name,
                    null,
                    $"Bot executable is missing: {executablePath}",
                    executablePath));
            }
        }
    }

    private static void AddMapFileIssues(
        IReadOnlyList<PracticeMap> maps,
        List<PracticeCompatibilityAuditIssue> issues)
    {
        foreach (var map in maps)
        {
            if (string.IsNullOrWhiteSpace(map.SourcePath) || !File.Exists(map.SourcePath))
            {
                issues.Add(new PracticeCompatibilityAuditIssue(
                    PracticeCompatibilityAuditIssueKind.MissingMapSource,
                    string.Empty,
                    map.Name,
                    $"Map source file is missing: {map.SourcePath}",
                    map.SourcePath));
            }
        }
    }

    private static void AddRuntimeCrashIssues(
        PracticeCatalog catalog,
        IReadOnlyList<PracticeBot> bots,
        IReadOnlyList<PracticeMap> maps,
        IReadOnlyList<RuntimeCrashEvidence> crashes,
        List<PracticeCompatibilityAuditIssue> issues)
    {
        foreach (var crash in crashes)
        {
            var matchingBots = MatchingBotsForCrash(bots, crash);
            if (matchingBots.Count == 0 ||
                (crash.BotDirectoryName is null && matchingBots.Count > 1))
            {
                continue;
            }

            var matchingMaps = maps.Where(map => MatchesCrashMap(crash, map));

            foreach (var bot in matchingBots)
            {
                foreach (var map in matchingMaps)
                {
                    if (!IsDeclaredPair(bot, map) ||
                        !PracticeCatalogCompatibility.IsCompatible(catalog, bot.Id, map.Id))
                    {
                        continue;
                    }

                    issues.Add(new PracticeCompatibilityAuditIssue(
                        PracticeCompatibilityAuditIssueKind.RuntimeCrashEvidence,
                        bot.Name,
                        map.Name,
                        $"Current compatible pair has runtime crash evidence: {bot.Name} + {map.Name}",
                        crash.SourcePath));
                }
            }
        }
    }

    private static IReadOnlyList<PracticeBot> MatchingBotsForCrash(
        IReadOnlyList<PracticeBot> bots,
        RuntimeCrashEvidence crash)
    {
        if (!string.IsNullOrWhiteSpace(crash.BotDirectoryName))
        {
            return bots
                .Where(bot => string.Equals(bot.Name, crash.BotDirectoryName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return bots
            .Where(bot => string.Equals(
                Path.GetFileName(bot.ExecutableName),
                crash.ModuleName,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static bool IsDeclaredPair(PracticeBot bot, PracticeMap map)
    {
        return map.Enabled &&
            (map.IsUserMap || map.EffectiveCompatibilityMapIds.Any(bot.SupportsMap));
    }

    private static bool MatchesCrashMap(RuntimeCrashEvidence crash, PracticeMap map)
    {
        if (!string.IsNullOrWhiteSpace(crash.MapFileName) &&
            string.Equals(
                NormalizeMapKey(Path.GetFileNameWithoutExtension(crash.MapFileName)),
                NormalizeMapKey(Path.GetFileNameWithoutExtension(map.FileName)),
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(crash.MapName) &&
            string.Equals(
                NormalizeMapKey(crash.MapName),
                NormalizeMapKey(map.Name),
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeMapFile(string line)
    {
        var trimmed = line.Trim();
        return trimmed.EndsWith(".scm", StringComparison.OrdinalIgnoreCase) ||
               trimmed.EndsWith(".scx", StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanLine(string value)
    {
        return new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
    }

    private static string NormalizeMapKey(string value)
    {
        var normalized = value.ToLowerInvariant()
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, @"^\s*\(?\d+\)?\s*", string.Empty);
        normalized = Regex.Replace(normalized, @"\s*\d+(\.\d+)*(\s*bw\s*1\s*16\s*1)?\s*$", string.Empty);
        normalized = Regex.Replace(normalized, @"[^a-z0-9]+", " ");
        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? ExtractBotDirectoryName(string modulePath)
    {
        var normalized = modulePath.Replace('/', '\\');
        var marker = "\\Bots\\";
        var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var start = markerIndex + marker.Length;
        var end = normalized.IndexOf('\\', start);
        return end > start
            ? normalized[start..end]
            : null;
    }
}
