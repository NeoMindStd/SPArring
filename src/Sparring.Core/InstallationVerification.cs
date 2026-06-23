using System.Security.Cryptography;
using System.IO.Compression;
using System.Text.Json;

namespace Sparring.Core;

public sealed record InstallationManifestEntry(string RelativePath, string Sha256);

public sealed record InstallationVerificationIssue(
    string RelativePath,
    InstallationVerificationIssueKind Kind,
    string Message);

public enum InstallationVerificationIssueKind
{
    Missing,
    HashMismatch
}

public sealed record InstallationRepairResult(
    int RequestedCount,
    int RestoredCount,
    IReadOnlyList<string> RestoredRelativePaths,
    IReadOnlyList<string> SkippedRelativePaths);

public sealed record InstallationState(
    string? StarCraftSourceRoot,
    bool InstallJava,
    DateTime InstalledAtUtc);

public static class InstallationVerifier
{
    public const string ManifestFileName = "install-manifest.json";
    public const string StateFileName = "install-state.json";
    public const string RepairCacheDirectoryName = "install-cache";
    public const string RepairPayloadFileName = "payload.zip";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static IReadOnlyList<InstallationManifestEntry> BuildManifest(string sourceRoot)
    {
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException(sourceRoot);
        }

        return Directory
            .EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => ShouldIncludeInManifest(sourceRoot, path))
            .Select(path => new InstallationManifestEntry(
                NormalizeRelativePath(Path.GetRelativePath(sourceRoot, path)),
                ComputeSha256(path)))
            .OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void SaveManifest(string manifestPath, IEnumerable<InstallationManifestEntry> manifest)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
    }

    public static IReadOnlyList<InstallationManifestEntry> LoadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return [];
        }

        var entries = JsonSerializer.Deserialize<List<InstallationManifestEntry>>(File.ReadAllText(manifestPath));
        return entries?
            .Select(entry => entry with { RelativePath = NormalizeRelativePath(entry.RelativePath) })
            .OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    public static void SaveState(string statePath, InstallationState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(statePath))!);
        File.WriteAllText(statePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    public static InstallationState? LoadState(string statePath)
    {
        if (!File.Exists(statePath))
        {
            return null;
        }

        return JsonSerializer.Deserialize<InstallationState>(File.ReadAllText(statePath));
    }

    public static IReadOnlyList<InstallationVerificationIssue> Verify(
        string targetRoot,
        IEnumerable<InstallationManifestEntry> manifest)
    {
        var issues = new List<InstallationVerificationIssue>();
        foreach (var entry in manifest)
        {
            var targetPath = Path.Combine(targetRoot, entry.RelativePath);
            if (!File.Exists(targetPath))
            {
                issues.Add(new InstallationVerificationIssue(
                    entry.RelativePath,
                    InstallationVerificationIssueKind.Missing,
                    "File is missing."));
                continue;
            }

            var actualHash = ComputeSha256(targetPath);
            if (!string.Equals(actualHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new InstallationVerificationIssue(
                    entry.RelativePath,
                    InstallationVerificationIssueKind.HashMismatch,
                    "File checksum did not match."));
            }
        }

        return issues;
    }

    public static InstallationRepairResult RepairFromPayloadZip(
        string targetRoot,
        string payloadZipPath,
        IEnumerable<InstallationVerificationIssue> issues,
        Func<string, bool>? canRepair = null)
    {
        var requested = issues.ToList();
        var restored = new List<string>();
        var skipped = new List<string>();
        if (requested.Count == 0)
        {
            return new InstallationRepairResult(0, 0, restored, skipped);
        }

        if (!File.Exists(payloadZipPath))
        {
            return new InstallationRepairResult(requested.Count, 0, restored, requested.Select(issue => issue.RelativePath).ToList());
        }

        canRepair ??= _ => true;
        var pending = requested
            .Where(issue =>
            {
                var allowed = canRepair(issue.RelativePath);
                if (!allowed)
                {
                    skipped.Add(issue.RelativePath);
                }

                return allowed;
            })
            .ToDictionary(
                issue => NormalizeArchivePath(issue.RelativePath),
                issue => issue.RelativePath,
                StringComparer.OrdinalIgnoreCase);

        using var archive = ZipFile.OpenRead(payloadZipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var archivePath = NormalizeArchivePath(entry.FullName);
            if (!pending.TryGetValue(archivePath, out var relativePath))
            {
                continue;
            }

            var targetPath = GetSafeTargetPath(targetRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            entry.ExtractToFile(targetPath, overwrite: true);
            restored.Add(relativePath);
            pending.Remove(archivePath);
        }

        skipped.AddRange(pending.Values);
        return new InstallationRepairResult(requested.Count, restored.Count, restored, skipped);
    }

    public static string RepairPayloadPath(string installRoot)
    {
        return Path.Combine(installRoot, RepairCacheDirectoryName, RepairPayloadFileName);
    }

    public static IReadOnlyList<string> RequiredRuntimeFiles(
        string installRoot,
        string playerRuntimeRoot,
        string aiRuntimeRoot,
        bool includeJava)
    {
        var requiredFiles = new List<string>
        {
            Path.Combine(installRoot, "Sparring.Client.exe"),
            Path.Combine(installRoot, "VERSION"),
            Path.Combine(installRoot, "README.md"),
            Path.Combine(installRoot, "scripts", "setup-runtime.ps1"),
            Path.Combine(installRoot, "data", "bots", "bots.dat"),
            Path.Combine(installRoot, "data", "maps", "maps.dat")
        };

        foreach (var root in new[] { playerRuntimeRoot, aiRuntimeRoot })
        {
            requiredFiles.AddRange(
            [
                Path.Combine(root, "StarCraft.exe"),
                Path.Combine(root, "stardat.mpq"),
                Path.Combine(root, "broodat.mpq"),
                Path.Combine(root, "patch_rt.mpq"),
                Path.Combine(root, "Chaoslauncher - MultiInstance.exe"),
                Path.Combine(root, "Plugins", "BWAPI_PluginInjector.bwl"),
                Path.Combine(root, "bwapi-data", "BWAPI.dll"),
                Path.Combine(root, "bwapi-data", "bwapi.ini"),
                Path.Combine(root, "bwapi-data", "TM", "TournamentModule.dll"),
                Path.Combine(root, "ddraw.dll")
            ]);
        }

        if (includeJava)
        {
            requiredFiles.Add(Path.Combine(installRoot, "runtime", "jdk", "bin", "java.exe"));
        }

        return requiredFiles;
    }

    public static IReadOnlyList<string> MissingRequiredRuntimeFiles(
        string installRoot,
        string playerRuntimeRoot,
        string aiRuntimeRoot,
        bool includeJava)
    {
        return RequiredRuntimeFiles(installRoot, playerRuntimeRoot, aiRuntimeRoot, includeJava)
            .Where(path => !File.Exists(path))
            .Select(path => Path.GetRelativePath(installRoot, path))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private static bool ShouldIncludeInManifest(string sourceRoot, string path)
    {
        var relativePath = NormalizeRelativePath(Path.GetRelativePath(sourceRoot, path));
        if (string.Equals(relativePath, ManifestFileName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(relativePath, StateFileName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !relativePath.StartsWith(
            RepairCacheDirectoryName + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeArchivePath(string path)
    {
        return path
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .TrimStart('/');
    }

    private static string GetSafeTargetPath(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"Refusing to repair rooted path: {relativePath}");
        }

        var rootFullPath = Path.GetFullPath(root);
        var targetPath = Path.GetFullPath(Path.Combine(rootFullPath, relativePath));
        var rootWithSeparator = rootFullPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootFullPath
            : rootFullPath + Path.DirectorySeparatorChar;
        if (!targetPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to repair path outside install root: {relativePath}");
        }

        return targetPath;
    }
}
