using StarAI.PracticeClient.App;
using System.Drawing;

namespace StarAI.PracticeClient.Tests;

public sealed class StarCraftStartupTraceTests
{
    [Fact]
    public void BuildChatCropRectangleIncludesTopLeftChatAreaWithoutExceedingSource()
    {
        var crop = StarCraftStartupTrace.BuildChatCropRectangle(new Size(640, 480));

        Assert.Equal(new Rectangle(0, 0, 640, 480), crop);
    }

    [Fact]
    public void BuildChatCropRectangleCapsLargeBorderlessFrame()
    {
        var crop = StarCraftStartupTrace.BuildChatCropRectangle(new Size(2560, 1440));

        Assert.Equal(new Rectangle(0, 0, 1600, 760), crop);
    }

    [Fact]
    public void CountRedErrorPixelsDetectsRedBwapiStyleTextArea()
    {
        using var bitmap = new Bitmap(640, 480);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Black);
            using var brush = new SolidBrush(Color.FromArgb(230, 15, 15));
            graphics.FillRectangle(brush, 120, 40, 120, 12);
        }

        Assert.True(StarCraftStartupTrace.CountRedErrorPixels(bitmap) >= 300);
    }

    [Fact]
    public void CountRedErrorPixelsIncludesScaledStartupChatHeight()
    {
        using var bitmap = new Bitmap(2560, 1440);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Black);
            using var brush = new SolidBrush(Color.FromArgb(230, 15, 15));
            graphics.FillRectangle(brush, 330, 610, 900, 16);
        }

        Assert.True(StarCraftStartupTrace.CountRedErrorPixels(bitmap) >= 1500);
    }
}
