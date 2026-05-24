using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public class HotkeyCsvStoreTests
{
    [Fact]
    public void LoadAndSaveWorkingCopy_ParsesSchnailStyleCsv()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var csv = Path.Combine(root, "sc_hotkeys.csv");
        var messages = Path.Combine(root, "messages_en.properties");
        File.WriteAllText(csv, "599,e<1>Build Probe(<3>E<1>)<0>,protoss_train_probe");
        File.WriteAllText(messages, "hotkey_desc_protoss_train_probe=Build Probe");

        var store = new HotkeyCsvStore();
        var entries = store.Load(csv, messages);

        Assert.Single(entries);
        Assert.Equal("e", entries[0].Hotkey);
        Assert.Equal("Build Probe", entries[0].Description);

        foreach (var entry in entries)
        {
            entry.Hotkey = "p";
        }
        store.SaveWorkingCopy(root, entries);

        var saved = File.ReadAllText(Path.Combine(root, "bwapi-data", "read", "sc_hotkeys.csv"));
        Assert.Contains("p<1>Build Probe", saved);
    }

    [Fact]
    public void Load_UsesSchnailCustomOverrideColumnWhenPresent()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var csv = Path.Combine(root, "sc_hotkeys.csv");
        File.WriteAllText(csv, "599,p<1>Build <3>P<1>robe<0>,protoss_train_probe,e<1>Build Probe(<3>E<1>)<0>");

        var entries = new HotkeyCsvStore().Load(csv, messagesPath: null);

        Assert.Single(entries);
        Assert.Equal("e", entries[0].Hotkey);
    }
}
