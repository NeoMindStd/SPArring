using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

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
        var root = Path.Combine(Path.GetTempPath(), "starai-tests", Guid.NewGuid().ToString("N"));
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
}
