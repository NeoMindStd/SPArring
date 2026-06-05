using Microsoft.Win32;
using StarAI.PracticeClient.Core;

#pragma warning disable CA1416

namespace StarAI.PracticeClient.Tests;

public sealed class ChaosLauncherConfiguratorTests
{
    [Fact]
    public void BuildWritesEnablesWModeAndBwapiButKeepsApmAlertOffByDefault()
    {
        var configurator = new ChaosLauncherConfigurator(new FakeRegistryAccess());

        var writes = configurator.BuildWrites(new ChaosLauncherRequest(
            @"C:\starai\SC116AI",
            RunStarCraftOnStartup: true,
            EnableWMode: true,
            EnableBwapi: true,
            EnableApmAlert: false));

        Assert.Equal(1, DwordValue(writes, ChaosLauncherConfigurator.EnabledKey, ChaosLauncherConfigurator.WModePlugin));
        Assert.Equal(1, DwordValue(writes, ChaosLauncherConfigurator.EnabledKey, ChaosLauncherConfigurator.BwapiPlugin));
        Assert.Equal(0, DwordValue(writes, ChaosLauncherConfigurator.EnabledKey, ChaosLauncherConfigurator.BwapiDebugPlugin));
        Assert.Equal(0, DwordValue(writes, ChaosLauncherConfigurator.EnabledKey, ChaosLauncherConfigurator.ApmAlertPlugin));
        Assert.Equal(1, DwordValue(writes, ChaosLauncherConfigurator.LauncherKey, "RunScOnStartup"));
        Assert.Equal(@"C:\starai\SC116AI", StringValue(writes, ChaosLauncherConfigurator.StarCraftInstallKey, "InstallPath"));
        Assert.Equal(@"C:\starai\SC116AI\StarCraft.exe", StringValue(writes, ChaosLauncherConfigurator.StarCraftInstallKey, "Program"));
    }

    [Fact]
    public void ApplyWithRestorePointRestoresPreviousGlobalChaosLauncherState()
    {
        var registry = new FakeRegistryAccess();
        registry.WriteValue(RegistryHiveKind.CurrentUser, ChaosLauncherConfigurator.EnabledKey, ChaosLauncherConfigurator.BwapiPlugin, 0, RegistryValueKind.DWord);
        registry.WriteValue(RegistryHiveKind.LocalMachine, ChaosLauncherConfigurator.StarCraftInstallKey, "InstallPath", @"D:\Games\StarCraft", RegistryValueKind.String);

        var configurator = new ChaosLauncherConfigurator(registry);
        var restorePoint = configurator.ApplyWithRestorePoint(new ChaosLauncherRequest(
            @"C:\starai\SC116AI_ai",
            RunStarCraftOnStartup: true,
            EnableWMode: true,
            EnableBwapi: true,
            EnableApmAlert: false));

        Assert.Equal(1, registry.ReadValue(RegistryHiveKind.CurrentUser, ChaosLauncherConfigurator.EnabledKey, ChaosLauncherConfigurator.BwapiPlugin).Value);
        Assert.Equal(@"C:\starai\SC116AI_ai", registry.ReadValue(RegistryHiveKind.LocalMachine, ChaosLauncherConfigurator.StarCraftInstallKey, "InstallPath").Value);

        restorePoint.Restore();

        Assert.Equal(0, registry.ReadValue(RegistryHiveKind.CurrentUser, ChaosLauncherConfigurator.EnabledKey, ChaosLauncherConfigurator.BwapiPlugin).Value);
        Assert.Equal(@"D:\Games\StarCraft", registry.ReadValue(RegistryHiveKind.LocalMachine, ChaosLauncherConfigurator.StarCraftInstallKey, "InstallPath").Value);
        Assert.False(registry.ReadValue(RegistryHiveKind.CurrentUser, ChaosLauncherConfigurator.EnabledKey, ChaosLauncherConfigurator.ApmAlertPlugin).Exists);
    }

    private static int DwordValue(IReadOnlyList<RegistryValueWrite> writes, string key, string name)
    {
        return (int)writes.Single(write => write.SubKey == key && write.Name == name).Value;
    }

    private static string StringValue(IReadOnlyList<RegistryValueWrite> writes, string key, string name)
    {
        return (string)writes.Single(write => write.SubKey == key && write.Name == name).Value;
    }
}

internal sealed class FakeRegistryAccess : IRegistryAccess
{
    private readonly Dictionary<(RegistryHiveKind Hive, string SubKey, string Name), RegistryValueState> _values = [];

    public RegistryValueState ReadValue(RegistryHiveKind hive, string subKey, string name)
    {
        return _values.TryGetValue((hive, subKey, name), out var value)
            ? value
            : new RegistryValueState(false, null, RegistryValueKind.Unknown);
    }

    public void WriteValue(RegistryHiveKind hive, string subKey, string name, object value, RegistryValueKind kind)
    {
        _values[(hive, subKey, name)] = new RegistryValueState(true, value, kind);
    }

    public void DeleteValue(RegistryHiveKind hive, string subKey, string name)
    {
        _values.Remove((hive, subKey, name));
    }
}
