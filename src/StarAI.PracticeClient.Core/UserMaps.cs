using System.Security.Cryptography;
using System.Text;

namespace StarAI.PracticeClient.Core;

public static class UserMapCatalogReader
{
    private static readonly string[] SupportedExtensions = [".scm", ".scx"];

    public static IReadOnlyList<PracticeMap> ReadDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return [];
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        return Directory.EnumerateFiles(directory, "*.*", options)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(CreateMap)
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static PracticeCatalog Merge(PracticeCatalog catalog, IReadOnlyList<PracticeMap> userMaps)
    {
        if (userMaps.Count == 0)
        {
            return catalog;
        }

        var existingSources = catalog.Maps
            .Select(map => map.SourcePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mergedMaps = catalog.Maps
            .Concat(userMaps.Where(map => map.SourcePath is not null && !existingSources.Contains(Path.GetFullPath(map.SourcePath))))
            .ToList();

        return catalog with { Maps = mergedMaps };
    }

    private static PracticeMap CreateMap(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var fileName = Path.GetFileName(fullPath);
        var name = Path.GetFileNameWithoutExtension(fullPath);
        return new PracticeMap(
            Id: StableGuid($"user-map:{fullPath.ToLowerInvariant()}"),
            Name: name,
            FileName: fileName,
            ImagePath: null,
            Enabled: true,
            SourcePath: fullPath,
            IsUserMap: true);
    }

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
