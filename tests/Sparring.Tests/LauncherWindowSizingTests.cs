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

    [Fact]
    public void MinimumSizeAllowsSmallNotebookWindows()
    {
        Assert.Equal(new Size(760, 520), LauncherWindowSizing.MinimumSize);
    }

    [Fact]
    public void InitialSizeDoesNotThrowWhenWorkingAreaIsSmallerThanMinimum()
    {
        var size = LauncherWindowSizing.InitialSize(new Rectangle(0, 0, 640, 480));

        Assert.Equal(new Size(640, 480), size);
    }

    [Theory]
    [InlineData(1440, 192, 720)]
    [InlineData(1440, 144, 960)]
    [InlineData(980, 96, 980)]
    public void LayoutScaleComparesVisibleWidthAtBaseDpi(int logicalPixels, int deviceDpi, int expectedPixels)
    {
        Assert.Equal(expectedPixels, LauncherLayoutScale.ToBaseDpi(logicalPixels, deviceDpi));
    }

    [Theory]
    [InlineData(320, 192, 640)]
    [InlineData(460, 144, 690)]
    [InlineData(500, 96, 500)]
    public void LayoutScaleExpandsReadableSizesForDeviceDpi(int basePixels, int deviceDpi, int expectedPixels)
    {
        Assert.Equal(expectedPixels, LauncherLayoutScale.FromBaseDpi(basePixels, deviceDpi));
    }
}
