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
    private const string StarCraftInstallKey = @"SOFTWARE\WOW6432Node\Blizzard Entertainment\StarCraft";
    private const string BwapiPlugin = "BWAPI 4.4.0 Injector [RELEASE]";
    private const string WModePlugin = "W-MODE 1.02";

    [SupportedOSPlatform("windows")]
    public void Apply(
        ChaosLaunchMode mode,
        string starCraftRoot,
        bool runStarCraftOnStartup = false,
        bool enableWMode = true)
    {
        using var launcher = Registry.CurrentUser.CreateSubKey(LauncherKey, true);
        using var enabled = Registry.CurrentUser.CreateSubKey(EnabledKey, true);
        using var incompatible = Registry.CurrentUser.CreateSubKey(RunIncompatibleKey, true);

        SetStarCraftInstallPath(starCraftRoot);

        launcher.SetValue("GameVersion", "Starcraft 1.16.1", RegistryValueKind.String);
        launcher.SetValue("AutoUpdate", 0, RegistryValueKind.DWord);
        launcher.SetValue("WarnNoAdmin", 0, RegistryValueKind.DWord);
        launcher.SetValue("MinimizeOnRun", 0, RegistryValueKind.DWord);
        launcher.SetValue("StartMinimized", 0, RegistryValueKind.DWord);
        launcher.SetValue("RunScOnStartup", runStarCraftOnStartup ? 1 : 0, RegistryValueKind.DWord);

        enabled.SetValue(WModePlugin, enableWMode ? 1 : 0, RegistryValueKind.DWord);
        enabled.SetValue(BwapiPlugin, mode == ChaosLaunchMode.Bot ? 1 : 0, RegistryValueKind.DWord);
        incompatible.SetValue(WModePlugin, 0, RegistryValueKind.DWord);
        incompatible.SetValue(BwapiPlugin, 0, RegistryValueKind.DWord);
    }

    [SupportedOSPlatform("windows")]
    public void SetRunStarCraftOnStartup(string starCraftRoot, bool enabled)
    {
        using var launcher = Registry.CurrentUser.CreateSubKey(LauncherKey, true);

        SetStarCraftInstallPath(starCraftRoot);
        launcher.SetValue("GameVersion", "Starcraft 1.16.1", RegistryValueKind.String);
        launcher.SetValue("WarnNoAdmin", 0, RegistryValueKind.DWord);
        launcher.SetValue("RunScOnStartup", enabled ? 1 : 0, RegistryValueKind.DWord);
    }

    [SupportedOSPlatform("windows")]
    private static void SetStarCraftInstallPath(string starCraftRoot)
    {
        var root = Path.GetFullPath(starCraftRoot);
        var starCraftExe = Path.Combine(root, "StarCraft.exe");

        using var install = Registry.LocalMachine.CreateSubKey(StarCraftInstallKey, true);
        install.SetValue("InstallPath", root, RegistryValueKind.String);
        install.SetValue("Program", starCraftExe, RegistryValueKind.String);
    }
}
