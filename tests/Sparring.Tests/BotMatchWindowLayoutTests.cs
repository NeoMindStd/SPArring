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

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1366, 768)]
    [InlineData(1280, 720)]
    public void RowPairKeepsThreeParallelMatchesVisibleWithoutOverlap(int width, int height)
    {
        var workingArea = new Rectangle(0, 0, width, height);
        var previousBottom = workingArea.Top;

        for (var row = 0; row < 3; row++)
        {
            var (left, right) = BotMatchWindowLayout.RowPair(workingArea, row, 3);

            Assert.True(workingArea.Contains(left), left.ToString());
            Assert.True(workingArea.Contains(right), right.ToString());
            Assert.True(left.Right <= right.Left, $"{left} overlaps {right}");
            Assert.True(left.Top >= previousBottom);
            AssertAspectRatio(left, 4.0 / 3.0);
            AssertAspectRatio(right, 4.0 / 3.0);
            previousBottom = left.Bottom;
        }
    }

    private static void AssertAspectRatio(Rectangle bounds, double expected)
    {
        var actual = (double)bounds.Width / bounds.Height;
        Assert.InRange(Math.Abs(actual - expected), 0, 0.02);
    }
}
