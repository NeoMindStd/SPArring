using System.Security.Cryptography;

namespace StarAI.PracticeClient.Core;

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

public static class InstallationVerifier
{
    public static IReadOnlyList<InstallationManifestEntry> BuildManifest(string sourceRoot)
    {
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException(sourceRoot);
        }

        return Directory
            .EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Select(path => new InstallationManifestEntry(
                NormalizeRelativePath(Path.GetRelativePath(sourceRoot, path)),
                ComputeSha256(path)))
            .OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
}
