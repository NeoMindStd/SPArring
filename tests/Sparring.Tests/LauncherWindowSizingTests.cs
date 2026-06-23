using Sparring.Client;

namespace Sparring.Tests;

public sealed class LauncherWindowSizingTests
{
    [Theory]
    [InlineData(1440, 960)]
    [InlineData(1366, 768)]
    public void InitialSizeUsesMostOfSmallAndScaledScreens(int width, int height)
    {
        var size = LauncherWindowSizing.InitialSize(new Rectangle(0, 0, width, height));

        Assert.InRange(size.Width, width - 96, width);
        Assert.InRange(size.Height, height - 96, height);
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    [InlineData(3840, 2160)]
    public void InitialSizeDoesNotBecomeHugeOnLargeScreens(int width, int height)
    {
        var size = LauncherWindowSizing.InitialSize(new Rectangle(0, 0, width, height));

        Assert.InRange(size.Width, 1180, 1480);
        Assert.InRange(size.Height, 820, 980);
    }
}
