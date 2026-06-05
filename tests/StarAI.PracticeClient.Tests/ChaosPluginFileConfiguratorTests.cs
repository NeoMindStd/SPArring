using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class ChaosPluginFileConfiguratorTests
{
    [Fact]
    public void ApplyMovesDisabledOptionalPluginsOutOfChaosLauncherLoadPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-plugin-file-tests", Guid.NewGuid().ToString("N"));
        var plugins = Path.Combine(root, "Plugins");
        Directory.CreateDirectory(plugins);
        File.WriteAllText(Path.Combine(plugins, "wmode.bwl"), "wmode");
        File.WriteAllText(Path.Combine(plugins, "APMAlert.bwl"), "apm");

        ChaosPluginFileConfigurator.Apply(root, enableWMode: false, enableApmAlert: false);

        Assert.False(File.Exists(Path.Combine(plugins, "wmode.bwl")));
        Assert.False(File.Exists(Path.Combine(plugins, "APMAlert.bwl")));
        Assert.True(File.Exists(Path.Combine(plugins, "wmode.bwl.starai-disabled")));
        Assert.True(File.Exists(Path.Combine(plugins, "APMAlert.bwl.starai-disabled")));
    }

    [Fact]
    public void ApplyRestoresOptionalPluginWhenEnabled()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-plugin-file-tests", Guid.NewGuid().ToString("N"));
        var plugins = Path.Combine(root, "Plugins");
        Directory.CreateDirectory(plugins);
        File.WriteAllText(Path.Combine(plugins, "wmode.bwl.starai-disabled"), "wmode");
        File.WriteAllText(Path.Combine(plugins, "APMAlert.bwl.starai-disabled"), "apm");

        ChaosPluginFileConfigurator.Apply(root, enableWMode: true, enableApmAlert: true);

        Assert.True(File.Exists(Path.Combine(plugins, "wmode.bwl")));
        Assert.True(File.Exists(Path.Combine(plugins, "APMAlert.bwl")));
        Assert.False(File.Exists(Path.Combine(plugins, "wmode.bwl.starai-disabled")));
        Assert.False(File.Exists(Path.Combine(plugins, "APMAlert.bwl.starai-disabled")));
    }
}
