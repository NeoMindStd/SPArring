using System.Text.Json;

namespace StarAI.PracticeClient.Core;

public static partial class SchnailCatalogReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static PracticeCatalog Read(string schnailRoot)
    {
        return ReadFiles(
            Path.Combine(schnailRoot, "bots", "bots.dat"),
            Path.Combine(schnailRoot, "maps", "maps.dat"),
            schnailRoot);
    }

    public static PracticeCatalog ReadFiles(string botsDatPath, string mapsDatPath, string? schnailRoot = null)
    {
        var bots = ReadBots(File.ReadAllText(botsDatPath), schnailRoot);
        var maps = ReadMaps(File.ReadAllText(mapsDatPath), schnailRoot);

        return new PracticeCatalog(bots, maps);
    }

    public static IReadOnlyList<PracticeBot> ReadBots(string json, string? schnailRoot = null)
    {
        var records = JsonSerializer.Deserialize<List<SchnailBotRecord>>(json, JsonOptions) ?? [];
        var botsRoot = string.IsNullOrWhiteSpace(schnailRoot) ? null : Path.Combine(schnailRoot, "bots");

        return records
            .Where(record => record.Enabled && record.EnabledByUser)
            .Select(record => ToPracticeBot(record, botsRoot))
            .Where(bot => bot.Id != Guid.Empty && !string.IsNullOrWhiteSpace(bot.Name))
            .OrderBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<PracticeMap> ReadMaps(string json, string? schnailRoot = null)
    {
        var records = JsonSerializer.Deserialize<List<SchnailMapRecord>>(json, JsonOptions) ?? [];
        var mapsRoot = string.IsNullOrWhiteSpace(schnailRoot) ? null : Path.Combine(schnailRoot, "maps");
        var mapFiles = EnumerateMapFiles(mapsRoot);

        return records
            .Where(record => record.Enabled)
            .Select(record => ToPracticeMap(record, mapsRoot, mapFiles))
            .Where(map => map.Id != Guid.Empty && !string.IsNullOrWhiteSpace(map.FileName))
            .Where(map => string.IsNullOrWhiteSpace(mapsRoot) || File.Exists(map.SourcePath))
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static PracticeBot ToPracticeBot(SchnailBotRecord record, string? botsRoot)
    {
        return new PracticeBot(
            Id: ParseGuid(record.Guid),
            Name: record.Name ?? string.Empty,
            Race: ParseRace(record.Race),
            ExecutableName: record.BotExecutable ?? string.Empty,
            ExecutableKind: ParseExecutableKind(record.BotType),
            BwapiVersion: record.BwapiVersion ?? string.Empty,
            Elo: ParseElo(record.Elo),
            PracticeOnly: record.PracticeOnly,
            SupportedMapIds: ParseGuidSet(record.MapGuids),
            Description: record.Description,
            Author: record.Author,
            SourceDirectory: FindBotDirectory(botsRoot, record.Name, record.BotExecutable));
    }

    private static PracticeMap ToPracticeMap(
        SchnailMapRecord record,
        string? mapsRoot,
        IReadOnlyList<string> mapFiles)
    {
        var sourcePath = ResolveMapSourcePath(mapsRoot, mapFiles, record.FileName, record.Name);
        return new PracticeMap(
            Id: ParseGuid(record.Guid),
            Name: record.Name ?? record.FileName ?? string.Empty,
            FileName: record.FileName ?? string.Empty,
            ImagePath: record.ImagePath,
            Enabled: record.Enabled,
            SourcePath: sourcePath);
    }

    private static IReadOnlyList<string> EnumerateMapFiles(string? mapsRoot)
    {
        if (string.IsNullOrWhiteSpace(mapsRoot) || !Directory.Exists(mapsRoot))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFiles(mapsRoot, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => string.Equals(Path.GetExtension(path), ".scm", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(Path.GetExtension(path), ".scx", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static string? ResolveMapSourcePath(
        string? mapsRoot,
        IReadOnlyList<string> mapFiles,
        string? fileName,
        string? mapName)
    {
        if (string.IsNullOrWhiteSpace(mapsRoot) || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var exact = Path.Combine(mapsRoot, fileName);
        if (File.Exists(exact))
        {
            return exact;
        }

        var expectedFileKey = NormalizeMapName(Path.GetFileNameWithoutExtension(fileName));
        var expectedNameKey = NormalizeMapName(mapName ?? string.Empty);
        return mapFiles
            .Select(path => new
            {
                Path = path,
                Key = NormalizeMapName(Path.GetFileNameWithoutExtension(path))
            })
            .Where(candidate =>
                (!string.IsNullOrWhiteSpace(expectedFileKey) && candidate.Key.StartsWith(expectedFileKey, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(expectedNameKey) && candidate.Key.StartsWith(expectedNameKey, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(candidate => candidate.Key.Length)
            .Select(candidate => candidate.Path)
            .FirstOrDefault();
    }

    private static string NormalizeMapName(string value)
    {
        var normalized = value.ToLowerInvariant()
            .Replace('_', ' ')
            .Replace('-', ' ');
        normalized = LeadingPlayerCountRegex().Replace(normalized, string.Empty);
        normalized = MapVersionSuffixRegex().Replace(normalized, string.Empty);
        normalized = MapNonWordRegex().Replace(normalized, " ");
        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? FindBotDirectory(string? botsRoot, string? botName, string? executableName)
    {
        if (string.IsNullOrWhiteSpace(botsRoot))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(botName))
        {
            var nameMatch = Path.Combine(botsRoot, botName);
            if (Directory.Exists(nameMatch))
            {
                return nameMatch;
            }
        }

        if (string.IsNullOrWhiteSpace(executableName) || !Directory.Exists(botsRoot))
        {
            return null;
        }

        try
        {
            return Directory.EnumerateDirectories(botsRoot)
                .FirstOrDefault(directory => File.Exists(Path.Combine(directory, executableName)));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static StarCraftRace ParseRace(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "terran" => StarCraftRace.Terran,
            "protoss" => StarCraftRace.Protoss,
            "zerg" => StarCraftRace.Zerg,
            "random" => StarCraftRace.Random,
            _ => StarCraftRace.Unknown
        };
    }

    private static BotExecutableKind ParseExecutableKind(string? value)
    {
        return value?.Trim().ToUpperInvariant() switch
        {
            "DLL" => BotExecutableKind.Dll,
            "CLIENT_EXE" => BotExecutableKind.ClientExe,
            "CLIENT_JAR" => BotExecutableKind.ClientJar,
            _ => BotExecutableKind.Unknown
        };
    }

    private static int? ParseElo(string? value)
    {
        return int.TryParse(value, out var elo) ? elo : null;
    }

    private static Guid ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private static IReadOnlySet<Guid> ParseGuidSet(IReadOnlyList<string>? values)
    {
        return (values ?? [])
            .Select(ParseGuid)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
    }

    private sealed record SchnailBotRecord
    {
        public string? Name { get; init; }
        public string? Guid { get; init; }
        public bool Enabled { get; init; }
        public string? Race { get; init; }
        public string? Description { get; init; }
        public string? Author { get; init; }
        public string? BotExecutable { get; init; }
        public string? BotType { get; init; }
        public string? BwapiVersion { get; init; }
        public bool EnabledByUser { get; init; }
        public bool PracticeOnly { get; init; }
        public string? Elo { get; init; }
        public List<string>? MapGuids { get; init; }
    }

    private sealed record SchnailMapRecord
    {
        public string? Name { get; init; }
        public string? FileName { get; init; }
        public string? ImagePath { get; init; }
        public string? Guid { get; init; }
        public bool Enabled { get; init; }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^\s*\(?\d+\)?\s*")]
    private static partial System.Text.RegularExpressions.Regex LeadingPlayerCountRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"\s+\d+(\.\d+)*(\s*bw\s*1\s*16\s*1)?\s*$")]
    private static partial System.Text.RegularExpressions.Regex MapVersionSuffixRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"[^a-z0-9]+")]
    private static partial System.Text.RegularExpressions.Regex MapNonWordRegex();
}
