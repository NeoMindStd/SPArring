namespace StarAI.PracticeClient.Core;

public static class PracticeAssetCatalogReader
{
    public static PracticeCatalog Read(PracticePaths paths)
    {
        return Read(paths.AssetRoot);
    }

    public static PracticeCatalog Read(string assetRoot)
    {
        var botsDat = Path.Combine(assetRoot, "bots", "bots.dat");
        var mapsDat = Path.Combine(assetRoot, "maps", "maps.dat");
        if (!File.Exists(botsDat))
        {
            throw new FileNotFoundException(
                "StarAI bundled bot catalog was not found. Run scripts\\import-schnail-assets.ps1 during release preparation.",
                botsDat);
        }

        if (!File.Exists(mapsDat))
        {
            throw new FileNotFoundException(
                "StarAI bundled map catalog was not found. Run scripts\\import-schnail-assets.ps1 during release preparation.",
                mapsDat);
        }

        return SchnailCatalogReader.ReadFiles(botsDat, mapsDat, assetRoot);
    }
}

public static class PracticeAssetPaths
{
    public static string DefaultHotkeyCsv(PracticePaths paths)
    {
        return Path.Combine(paths.AssetRoot, "res", "sc_hotkeys.csv");
    }

    public static string Messages(PracticePaths paths)
    {
        return Path.Combine(paths.AssetRoot, "res", "messages_kr.properties");
    }

    public static string HotkeyIconRoot(PracticePaths paths)
    {
        return Path.Combine(paths.AssetRoot, "res", "hotkey_icons");
    }

    public static string StatText(PracticePaths paths)
    {
        return Path.Combine(paths.AssetRoot, "res", "hotkey_data", "stat_txt.txt");
    }

    public static string TblCompiler(PracticePaths paths)
    {
        return Path.Combine(paths.AssetRoot, "res", "hotkey_data", "sctblcmp.exe");
    }

    public static string MpqWriterClasspath(PracticePaths paths)
    {
        return Path.Combine(paths.AssetRoot, "tools", "mpq", "schnail-client.exe");
    }
}
