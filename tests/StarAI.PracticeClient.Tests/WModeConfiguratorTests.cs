using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class WModeConfiguratorTests
{
    [Fact]
    public void ApplyDisablesClipCursorWhenUnchecked()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-wmode-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var path = WModeConfigurator.Apply(root, new WModeSettings(
            WindowedMode: true,
            ClipCursor: false,
            DoubleSize: true,
            MuteNotFocused: false));
        var ini = IniDocument.Parse(File.ReadAllText(path));

        Assert.Equal("1", ini.Get("W-MODE", "Windowed"));
        Assert.Equal("0", ini.Get("W-MODE", "ClipCursor"));
        Assert.Equal("1", ini.Get("W-MODE", "DblSizeMode"));
        Assert.Equal("1", ini.Get("W-MODE", "EnableWindowMove"));
        Assert.Equal("0", ini.Get("W-MODE", "WindowClientX"));
        Assert.Equal("0", ini.Get("W-MODE", "WindowClientY"));
        Assert.Equal("0", ini.Get("W-MODE", "WindowClientXDblSized"));
        Assert.Equal("0", ini.Get("W-MODE", "WindowClientYDblSized"));
    }
}
