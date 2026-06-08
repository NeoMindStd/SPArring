using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public static partial class RemasteredLadderMapCatalogReader
{
    private static readonly string[] SupportedExtensions = [".scm", ".scx"];

    public static string DefaultDirectory()
    {
        var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
        var candidates = new[]
        {
            string.IsNullOrWhiteSpace(oneDrive)
                ? null
                : Path.Combine(oneDrive, "Documents", "StarCraft", "Maps", "ladder"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StarCraft", "Maps", "ladder")
        };

        return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            ?? string.Empty;
    }

    public static IReadOnlyList<PracticeMap> ReadDirectory(string? directory, PracticeCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return [];
        }

        var knownMapsByName = catalog.Maps
            .GroupBy(map => NormalizeMapName(map.Name))
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(
                group => group.Key,
                group => group.Select(map => map.Id).ToHashSet(),
                StringComparer.OrdinalIgnoreCase);

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        return Directory.EnumerateFiles(directory, "*.*", options)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(path => CreateMap(path, knownMapsByName))
            .Where(map => map.CompatibilityMapIds is { Count: > 0 })
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static PracticeMap CreateMap(string path, IReadOnlyDictionary<string, HashSet<Guid>> knownMapsByName)
    {
        var fullPath = Path.GetFullPath(path);
        var fileName = Path.GetFileName(fullPath);
        var name = Path.GetFileNameWithoutExtension(fullPath)
            .Replace("_", " ", StringComparison.Ordinal)
            .Trim();
        var key = NormalizeMapName(name);
        var compatibilityIds = knownMapsByName.TryGetValue(key, out var ids)
            ? ids
            : [];

        return new PracticeMap(
            Id: StableGuid($"remastered-ladder-map:{fullPath.ToLowerInvariant()}"),
            Name: $"{name} [Remastered Ladder]",
            FileName: fileName,
            ImagePath: null,
            Enabled: true,
            SourcePath: fullPath,
            IsUserMap: false,
            CompatibilityMapIds: compatibilityIds);
    }

    private static string NormalizeMapName(string value)
    {
        var normalized = value.ToLowerInvariant()
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);
        normalized = LeadingPlayerCountRegex().Replace(normalized, string.Empty);
        normalized = MapVersionSuffixRegex().Replace(normalized, string.Empty);
        normalized = MapNonWordRegex().Replace(normalized, " ");
        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }

    [GeneratedRegex(@"^\s*\(?\d+\)?\s*")]
    private static partial Regex LeadingPlayerCountRegex();

    [GeneratedRegex(@"\s*\d+(\.\d+)*(\s*bw\s*1\s*16\s*1)?\s*$")]
    private static partial Regex MapVersionSuffixRegex();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex MapNonWordRegex();
}
