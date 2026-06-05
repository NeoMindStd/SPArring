using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class CncDdrawConfiguratorTests
{
    [Fact]
    public void RenderIniUsesBorderlessFullscreenForPlayer()
    {
        var ini = IniDocument.Parse(CncDdrawConfigurator.RenderIni(CncDdrawMode.BorderlessFullscreen));

        Assert.Equal("true", ini.Get("ddraw", "fullscreen"));
        Assert.Equal("true", ini.Get("ddraw", "windowed"));
        Assert.Equal("true", ini.Get("ddraw", "maintas"));
        Assert.Equal("true", ini.Get("ddraw", "adjmouse"));
        Assert.Equal("false", ini.Get("ddraw", "border"));
        Assert.Equal("true", ini.Get("ddraw", "nonexclusive"));
        Assert.Equal("0", ini.Get("ddraw", "fixchilds"));
        Assert.Equal("true", ini.Get("ddraw", "no_compat_warning"));
    }

    [Fact]
    public void RenderIniUsesSmallWindowForAi()
    {
        var ini = IniDocument.Parse(CncDdrawConfigurator.RenderIni(CncDdrawMode.Windowed));

        Assert.Equal("false", ini.Get("ddraw", "fullscreen"));
        Assert.Equal("true", ini.Get("ddraw", "windowed"));
        Assert.Equal("640", ini.Get("ddraw", "width"));
        Assert.Equal("480", ini.Get("ddraw", "height"));
        Assert.Equal("false", ini.Get("ddraw", "adjmouse"));
        Assert.Equal("true", ini.Get("ddraw", "border"));
    }
}
