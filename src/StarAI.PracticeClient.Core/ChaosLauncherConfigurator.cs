using Microsoft.Win32;
using System.Runtime.Versioning;

namespace StarAI.PracticeClient.Core;

public enum ChaosLaunchMode
{
    Human,
    Bot
}

public sealed class ChaosLauncherConfigurator
{
    private const string LauncherKey = @"Software\Chaoslauncher\Launcher";
    private const string EnabledKey = @"Software\Chaoslauncher\PluginsEnabled";
    private const string RunIncompatibleKey = @"Software\Chaoslauncher\PluginsRunIncompatible";
    private const string BwapiPlugin = "BWAPI 4.4.0 Injector [RELEASE]";
    private const string WModePlugin = "W-MODE 1.02";

    [SupportedOSPlatform("windows")]
    public void Apply(ChaosLaunchMode mode)
    {
        using var launcher = Registry.CurrentUser.CreateSubKey(LauncherKey, true);
        using var enabled = Registry.CurrentUser.CreateSubKey(EnabledKey, true);
        using var incompatible = Registry.CurrentUser.CreateSubKey(RunIncompatibleKey, true);

        launcher.SetValue("GameVersion", "Starcraft 1.16.1", RegistryValueKind.String);
        launcher.SetValue("AutoUpdate", 0, RegistryValueKind.DWord);
        launcher.SetValue("WarnNoAdmin", 0, RegistryValueKind.DWord);
        launcher.SetValue("MinimizeOnRun", 0, RegistryValueKind.DWord);
        launcher.SetValue("StartMinimized", 0, RegistryValueKind.DWord);

        enabled.SetValue(WModePlugin, 1, RegistryValueKind.DWord);
        enabled.SetValue(BwapiPlugin, mode == ChaosLaunchMode.Bot ? 1 : 0, RegistryValueKind.DWord);
        incompatible.SetValue(WModePlugin, 0, RegistryValueKind.DWord);
        incompatible.SetValue(BwapiPlugin, 0, RegistryValueKind.DWord);
    }
}
