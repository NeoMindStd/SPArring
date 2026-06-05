using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class HotkeyStatTextPatcherTests
{
    [Fact]
    public void PatchUsesOneBasedStringId()
    {
        var statText = string.Join('\n', ["line1", "old probe", "line3"]);
        var entries = new[]
        {
            new HotkeyEntry
            {
                StringId = 2,
                CommandId = "protoss_train_probe",
                Hotkey = "p",
                Description = "Build Probe",
                DefaultText = "old probe",
                CurrentText = "old probe"
            }
        };

        var patched = HotkeyStatTextPatcher.Patch(statText, entries);

        Assert.Contains("line1", patched);
        Assert.Contains("p<1>Build Probe(<3>P<1>)<0>", patched);
        Assert.Contains("line3", patched);
    }
}
