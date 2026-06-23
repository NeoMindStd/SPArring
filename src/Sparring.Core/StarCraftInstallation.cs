namespace Sparring.Core;

public static class StarCraftInstallation
{
    public static readonly string[] RequiredRootFiles =
    [
        "StarCraft.exe",
        "stardat.mpq",
        "broodat.mpq",
        "patch_rt.mpq"
    ];

    public static bool IsValidRoot(string? root)
    {
        return MissingRequiredFiles(root).Count == 0;
    }

    public static IReadOnlyList<string> MissingRequiredFiles(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return RequiredRootFiles;
        }

        return RequiredRootFiles
            .Where(relative => !File.Exists(Path.Combine(root, relative)))
            .ToList();
    }
}
