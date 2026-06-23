using Sparring.Core;

namespace Sparring.Tests;

public sealed class ChaosPluginFileConfiguratorTests
{
    [Fact]
    public void ApplyMovesDisabledOptionalPluginsOutOfChaosLauncherLoadPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-plugin-file-tests", Guid.NewGuid().ToString("N"));
        var plugins = Path.Combine(root, "Plugins");
        Directory.CreateDirectory(plugins);
        File.WriteAllText(Path.Combine(plugins, "wmode.bwl"), "wmode");
        File.WriteAllText(Path.Combine(plugins, "APMAlert.bwl"), "apm");

        ChaosPluginFileConfigurator.Apply(root, enableWMode: false, enableApmAlert: false);

        Assert.False(File.Exists(Path.Combine(plugins, "wmode.bwl")));
        Assert.False(File.Exists(Path.Combine(plugins, "APMAlert.bwl")));
        Assert.True(File.Exists(Path.Combine(plugins, "wmode.bwl.sparring-disabled")));
        Assert.True(File.Exists(Path.Combine(plugins, "APMAlert.bwl.sparring-disabled")));
    }

    [Fact]
    public void ApplyRestoresOptionalPluginWhenEnabled()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-plugin-file-tests", Guid.NewGuid().ToString("N"));
        var plugins = Path.Combine(root, "Plugins");
        Directory.CreateDirectory(plugins);
        File.WriteAllText(Path.Combine(plugins, "wmode.bwl.sparring-disabled"), "wmode");
        File.WriteAllText(Path.Combine(plugins, "APMAlert.bwl.sparring-disabled"), "apm");

        ChaosPluginFileConfigurator.Apply(root, enableWMode: true, enableApmAlert: true);

        Assert.True(File.Exists(Path.Combine(plugins, "wmode.bwl")));
        Assert.True(File.Exists(Path.Combine(plugins, "APMAlert.bwl")));
        Assert.False(File.Exists(Path.Combine(plugins, "wmode.bwl.sparring-disabled")));
        Assert.False(File.Exists(Path.Combine(plugins, "APMAlert.bwl.sparring-disabled")));
    }
}
