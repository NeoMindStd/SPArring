using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public class HotkeyImporterTests
{
    [Fact]
    public void ImportBestAvailable_CopiesSchnailHotkeyCsvAndExportsRemasteredHotkeys()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-tests", Guid.NewGuid().ToString("N"));
        var schnail = Path.Combine(root, "schnail");
        var sc = Path.Combine(root, "sc");
        Directory.CreateDirectory(Path.Combine(schnail, "starcraft_bundled"));
        Directory.CreateDirectory(Path.Combine(schnail, "res"));
        Directory.CreateDirectory(sc);

        var targetPatch = Path.Combine(sc, "patch_rt.mpq");
        File.WriteAllText(targetPatch, "original");
        File.WriteAllText(Path.Combine(schnail, "starcraft_bundled", "patch_rt.mpq"), "patched");
        File.WriteAllText(Path.Combine(schnail, "res", "sc_hotkeys.csv"), "599,e,protoss_train_probe");

        var settings = Path.Combine(root, "CSettings.json");
        File.WriteAllText(settings, """
            {
              "Hotkeys": "STR_MAKE_P_PROBE=e\nSTR_BLD_PYLON=e\n"
            }
            """);

        var result = HotkeyImporter.ImportBestAvailable(sc, schnail, settings);

        Assert.True(result.AppliedPatch);
        Assert.Equal("patched", File.ReadAllText(targetPatch));
        Assert.Equal("original", File.ReadAllText(targetPatch + ".starai-hotkey-original"));
        Assert.True(File.Exists(Path.Combine(sc, "bwapi-data", "read", "sc_hotkeys.csv")));
        Assert.Equal("599,e,protoss_train_probe", File.ReadAllText(Path.Combine(sc, "bwapi-data", "read", "sc_hotkeys.csv")));
        Assert.Contains("STR_BLD_PYLON=e", File.ReadAllText(Path.Combine(sc, "bwapi-data", "read", "remastered_hotkeys.txt")));
    }
}
