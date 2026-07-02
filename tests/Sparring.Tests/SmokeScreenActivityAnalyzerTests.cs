using Sparring.Client;
using System.Drawing;

namespace Sparring.Tests;

public sealed class SmokeScreenActivityAnalyzerTests
{
    [Fact]
    public void IdenticalScreensAreNotActive()
    {
        using var before = CreateBaseScreen();
        using var after = CreateBaseScreen();

        var summary = SmokeScreenActivityAnalyzer.Compare(before, after);

        Assert.False(summary.HasMeaningfulActivity);
        Assert.Equal(0, summary.ChangedSamples);
    }

    [Fact]
    public void TinyPointerLikeChangeIsIgnored()
    {
        using var before = CreateBaseScreen();
        using var after = CreateBaseScreen();
        using (var graphics = Graphics.FromImage(after))
        {
            graphics.FillRectangle(Brushes.White, 110, 70, 4, 4);
        }

        var summary = SmokeScreenActivityAnalyzer.Compare(before, after);

        Assert.False(summary.HasMeaningfulActivity);
    }

    [Fact]
    public void WorldMovementIsActive()
    {
        using var before = CreateBaseScreen();
        using var after = CreateBaseScreen();
        using (var graphics = Graphics.FromImage(after))
        {
            graphics.FillRectangle(Brushes.DarkOliveGreen, 60, 52, 44, 32);
            graphics.FillRectangle(Brushes.LightGray, 118, 82, 36, 28);
        }

        var summary = SmokeScreenActivityAnalyzer.Compare(before, after);

        Assert.True(summary.HasMeaningfulActivity);
        Assert.True(summary.ChangedRatio > 0.01);
    }

    private static Bitmap CreateBaseScreen()
    {
        var bitmap = new Bitmap(240, 180);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(24, 48, 32));
        using var terrainBrush = new SolidBrush(Color.FromArgb(32, 80, 48));
        using var hudBrush = new SolidBrush(Color.FromArgb(12, 12, 12));
        graphics.FillRectangle(terrainBrush, 10, 14, 210, 110);
        graphics.FillRectangle(hudBrush, 0, 135, 240, 45);
        return bitmap;
    }
}
