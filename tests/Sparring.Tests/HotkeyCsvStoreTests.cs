using Sparring.Core;

namespace Sparring.Tests;

public sealed class HotkeyCsvStoreTests
{
    [Fact]
    public void ParseLineUsesCustomOverrideColumnWhenPresent()
    {
        var entry = HotkeyCsvStore.ParseLine(
            "599,p<1>Build <3>P<1>robe<0>,protoss_train_probe,e<1>Build Probe(<3>E<1>)<0>");

        Assert.NotNull(entry);
        Assert.Equal(599, entry.StringId);
        Assert.Equal("e", entry.Hotkey);
        Assert.Equal("protoss_train_probe", entry.CommandId);
    }

    [Fact]
    public void SaveWorkingCopyWritesFourColumnSchnailStyleCsv()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = new HotkeyCsvStore();
        var entry = new HotkeyEntry
        {
            StringId = 599,
            CommandId = "protoss_train_probe",
            Hotkey = "p",
            Description = "Build Probe",
            DefaultText = "e<1>Build Probe(<3>E<1>)<0>",
            CurrentText = "e<1>Build Probe(<3>E<1>)<0>"
        };

        var path = store.SaveWorkingCopy(root, [entry]);

        var saved = File.ReadAllText(path);
        Assert.Contains("599,e<1>Build Probe", saved);
        Assert.Contains("protoss_train_probe,p<1>Build Probe(<3>P<1>)<0>", saved);
    }

    [Fact]
    public void RemasteredImporterAppliesKnownKeyValueHotkeys()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-hotkeys", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "remastered_hotkeys.txt");
        File.WriteAllLines(path,
        [
            "STR_MAKE_T_SCV=x",
            "STR_PSISTORM=q",
            "STR_BLD_GATEWAY=w",
            "STR_UNKNOWN=z"
        ]);
        var entries = new List<HotkeyEntry>
        {
            Entry("terran_train_scv", "s"),
            Entry("protoss_cmd_psistorm", "t"),
            Entry("protoss_build_gateway", "g")
        };

        var result = RemasteredHotkeyImporter.ApplyFromFile(path, entries);

        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal("x", entries.Single(entry => entry.CommandId == "terran_train_scv").Hotkey);
        Assert.Equal("q", entries.Single(entry => entry.CommandId == "protoss_cmd_psistorm").Hotkey);
        Assert.Equal("w", entries.Single(entry => entry.CommandId == "protoss_build_gateway").Hotkey);
    }

    [Fact]
    public void RemasteredImporterFindsCandidateFileInDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-hotkeys", Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "StarCraft", "Hotkeys");
        Directory.CreateDirectory(nested);
        var path = Path.Combine(nested, "Grid.hotkeys");
        File.WriteAllText(path, "STR_MAKE_T_SCV=s");

        var found = RemasteredHotkeyImporter.FindFirstCandidateFile([root]);

        Assert.Equal(path, found);
    }

    [Fact]
    public void RemasteredImporterFindsAndAppliesCSettingsJson()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-hotkeys", Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "StarCraft");
        Directory.CreateDirectory(nested);
        var path = Path.Combine(nested, "CSettings.json");
        File.WriteAllText(path, """
            {
              "Hotkeys": "STR_MAKE_T_SCV=x\nSTR_PSISTORM=q\nSTR_BLD_GATEWAY=w\n"
            }
            """);
        var entries = new List<HotkeyEntry>
        {
            Entry("terran_train_scv", "s"),
            Entry("protoss_cmd_psistorm", "t"),
            Entry("protoss_build_gateway", "g")
        };

        var found = RemasteredHotkeyImporter.FindFirstCandidateFile([root]);
        var result = RemasteredHotkeyImporter.ApplyFromFile(path, entries);

        Assert.Equal(path, found);
        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal("x", entries.Single(entry => entry.CommandId == "terran_train_scv").Hotkey);
        Assert.Equal("q", entries.Single(entry => entry.CommandId == "protoss_cmd_psistorm").Hotkey);
        Assert.Equal("w", entries.Single(entry => entry.CommandId == "protoss_build_gateway").Hotkey);
    }

    [Fact]
    public void RemasteredImporterDefaultRootsIncludeRegistryInstallPath()
    {
        var registry = new FakeRegistryAccess();
        registry.WriteValue(
            RegistryHiveKind.LocalMachine,
            ChaosLauncherConfigurator.StarCraftInstallKey,
            "InstallPath",
            @"D:\Games\StarCraft",
            Microsoft.Win32.RegistryValueKind.String);
        registry.WriteValue(
            RegistryHiveKind.LocalMachine,
            ChaosLauncherConfigurator.StarCraftInstallKey,
            "Program",
            @"D:\Games\StarCraft\StarCraft.exe",
            Microsoft.Win32.RegistryValueKind.String);

        var roots = RemasteredHotkeyImporter.DefaultCandidateRoots(registry);

        Assert.Contains(@"D:\Games\StarCraft", roots);
    }

    [Fact]
    public void StarCraftControlSettingsImporterReadsScrollSpeeds()
    {
        var registry = new FakeRegistryAccess();
        registry.WriteValue(
            RegistryHiveKind.CurrentUser,
            ChaosLauncherConfigurator.StarCraftUserSettingsKey,
            "mscroll",
            5,
            Microsoft.Win32.RegistryValueKind.DWord);
        registry.WriteValue(
            RegistryHiveKind.CurrentUser,
            ChaosLauncherConfigurator.StarCraftUserSettingsKey,
            "kscroll",
            2,
            Microsoft.Win32.RegistryValueKind.DWord);
        registry.WriteValue(
            RegistryHiveKind.CurrentUser,
            ChaosLauncherConfigurator.StarCraftUserSettingsKey,
            "speed",
            6,
            Microsoft.Win32.RegistryValueKind.DWord);
        registry.WriteValue(
            RegistryHiveKind.CurrentUser,
            ChaosLauncherConfigurator.StarCraftUserSettingsKey,
            "MouseSensitivity",
            44,
            Microsoft.Win32.RegistryValueKind.DWord);

        var settings = new StarCraftControlSettingsImporter(registry).Read();

        Assert.Equal(42, settings.GameSpeedOverrideMs);
        Assert.Equal(44, settings.MouseSensitivity);
        Assert.Equal(5, settings.MouseScrollSpeed);
        Assert.Equal(2, settings.KeyboardScrollSpeed);
    }

    [Fact]
    public void StarCraftControlSettingsImporterReadsCSettingsJson()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-hotkeys", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "CSettings.json");
        File.WriteAllText(path, """
            {
              "speed": 5,
              "MouseSensitivity": 67,
              "m_mscroll": 4,
              "m_kscroll": 1
            }
            """);

        var settings = new StarCraftControlSettingsImporter(new FakeRegistryAccess()).Read(path);

        Assert.Equal(48, settings.GameSpeedOverrideMs);
        Assert.Equal(67, settings.MouseSensitivity);
        Assert.Equal(4, settings.MouseScrollSpeed);
        Assert.Equal(1, settings.KeyboardScrollSpeed);
    }

    private static HotkeyEntry Entry(string commandId, string hotkey)
    {
        return new HotkeyEntry
        {
            StringId = 1,
            CommandId = commandId,
            Hotkey = hotkey,
            Description = commandId,
            DefaultText = commandId,
            CurrentText = commandId
        };
    }
}
