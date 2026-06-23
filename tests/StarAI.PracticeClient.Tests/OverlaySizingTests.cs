using StarAI.PracticeClient.App;

namespace StarAI.PracticeClient.Tests;

public sealed class OverlaySizingTests
{
    [Fact]
    public void MeasureOverlaySizeGrowsForLongApmText()
    {
        using var font = new Font("Segoe UI", 16F, FontStyle.Bold);

        var normal = OverlaySizing.MeasureOverlaySize("00:34  APM 126", font);
        var longText = OverlaySizing.MeasureOverlaySize("124:34  APM 1234", font);

        Assert.True(longText.Width > normal.Width);
        Assert.True(longText.Width >= 220);
    }

    [Fact]
    public void ChooseLocationKeepsOverlayInsideGameBounds()
    {
        var bounds = new Rectangle(120, 80, 1280, 720);
        var size = new Size(260, 44);

        var location = OverlaySizing.ChooseLocation(bounds, size);

        Assert.True(location.X >= bounds.Left);
        Assert.True(location.Y >= bounds.Top);
        Assert.True(location.X + size.Width <= bounds.Right);
        Assert.True(location.Y + size.Height <= bounds.Bottom);
    }
}
