using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class HotkeyPatchApplierSmokeTests
{
    [Fact]
    public void SaveAndApplyPatchesRuntimeMpqCopyWithSfmpqWriter()
    {
        var defaults = PracticePaths.Defaults();
        var sourcePatch = Path.Combine(defaults.PlayerRuntimeRoot, "patch_rt.mpq");
        var compiler = Path.Combine(defaults.SchnailRoot, "res", "hotkey_data", "sctblcmp.exe");
        if (!File.Exists(sourcePatch) || !File.Exists(compiler))
        {
            return;
        }

        var root = Path.Combine(Path.GetTempPath(), "starai-hotkey-smoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.Copy(sourcePatch, Path.Combine(root, "patch_rt.mpq"));
        var paths = defaults with
        {
            PlayerRuntimeRoot = root,
            AiRuntimeRoot = root + "_ai"
        };
        Directory.CreateDirectory(paths.AiRuntimeRoot);
        var entries = new[]
        {
            new HotkeyEntry
            {
                StringId = 599,
                CommandId = "protoss_train_probe",
                Hotkey = "p",
                Description = "Build Probe",
                DefaultText = "e<1>Build Probe(<3>E<1>)<0>",
                CurrentText = "e<1>Build Probe(<3>E<1>)<0>"
            }
        };

        var result = new HotkeyPatchApplier().SaveAndApply(paths, root, entries, applyMpq: true);

        Assert.True(result.AppliedMpq);
        Assert.True(File.Exists(result.PatchedTblPath));
        Assert.Contains("반영", result.Message);
        Assert.NotEqual(
            File.ReadAllBytes(sourcePatch),
            File.ReadAllBytes(Path.Combine(root, "patch_rt.mpq")));
    }
}
