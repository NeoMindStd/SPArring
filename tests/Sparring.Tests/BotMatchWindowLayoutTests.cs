using Sparring.Client;

namespace Sparring.Tests;

public sealed class BotMatchWindowLayoutTests
{
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1366, 768)]
    [InlineData(1280, 720)]
    public void SideBySideKeepsWindowsInsideWorkingAreaWithoutOverlap(int width, int height)
    {
        var workingArea = new Rectangle(0, 0, width, height);

        var (left, right) = BotMatchWindowLayout.SideBySide(workingArea);

        Assert.True(workingArea.Contains(left), left.ToString());
        Assert.True(workingArea.Contains(right), right.ToString());
        Assert.True(left.Right <= right.Left, $"{left} overlaps {right}");
        Assert.True(left.Width >= 320);
        Assert.True(right.Width >= 320);
    }
}
