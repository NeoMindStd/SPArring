using StarAI.PracticeClient.App;
using System.Drawing;

namespace StarAI.PracticeClient.Tests;

public sealed class StarCraftScreenAnalyzerTests
{
    [Fact]
    public void AnalyzeTreatsHudScreenWithGreenSelectionAsInGame()
    {
        using var bitmap = CreateDarkBitmap();
        FillRectangle(bitmap, 0, 360, 120, 18, Color.FromArgb(20, 80, 100));
        FillRectangle(bitmap, 250, 150, 120, 120, Color.FromArgb(0, 180, 0));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeKeepsGreenTextScreenAsMenuWhenHudIsAbsent()
    {
        using var bitmap = CreateDarkBitmap();
        FillRectangle(bitmap, 180, 70, 260, 36, Color.FromArgb(35, 180, 35));
        FillRectangle(bitmap, 180, 130, 260, 36, Color.FromArgb(35, 180, 35));

        Assert.Equal(StarCraftScreenState.MenuLike, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeKeepsRedFramedRoomAsGameRoomEvenWithBottomHudColors()
    {
        using var bitmap = CreateDarkBitmap();
        FillRectangle(bitmap, 0, 360, 640, 120, Color.FromArgb(20, 42, 70));
        FillRectangle(bitmap, 100, 40, 3, 320, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 500, 40, 3, 320, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 100, 40, 400, 3, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 100, 360, 400, 3, Color.FromArgb(130, 20, 20));

        Assert.Equal(StarCraftScreenState.GameRoom, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsBorderlessScaledHudAsInGame()
    {
        using var bitmap = new Bitmap(2560, 1440);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.Black);

        const int left = 320;
        const int top = 0;
        const int width = 1920;
        const int height = 1440;
        FillRectangle(bitmap, left, top, width, height, Color.FromArgb(44, 64, 32));
        FillRectangle(bitmap, left, top + 1040, width, 400, Color.FromArgb(48, 58, 74));
        FillRectangle(bitmap, left + 80, top + 1110, 320, 260, Color.FromArgb(39, 54, 82));
        FillRectangle(bitmap, left + 620, top + 1160, 580, 210, Color.FromArgb(31, 42, 60));
        FillRectangle(bitmap, left + 1330, top + 1080, 420, 310, Color.FromArgb(33, 50, 67));
        FillRectangle(bitmap, left + 1490, top + 40, 32, 24, Color.FromArgb(35, 190, 45));
        FillRectangle(bitmap, left + 1700, top + 40, 42, 24, Color.FromArgb(170, 135, 50));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsDesertTerrainWithHudAsInGame()
    {
        using var bitmap = new Bitmap(2560, 1440);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.Black);

        const int left = 320;
        const int width = 1920;
        FillRectangle(bitmap, left, 0, width, 1040, Color.FromArgb(100, 40, 20));
        FillRectangle(bitmap, left, 1040, width, 400, Color.FromArgb(48, 58, 74));
        FillRectangle(bitmap, left + 80, 1110, 320, 260, Color.FromArgb(39, 54, 82));
        FillRectangle(bitmap, left + 620, 1160, 580, 210, Color.FromArgb(31, 42, 60));
        FillRectangle(bitmap, left + 1330, 1080, 420, 310, Color.FromArgb(33, 50, 67));
        FillRectangle(bitmap, left + 1490, 40, 32, 24, Color.FromArgb(35, 190, 45));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsPlatformTerrainWithHudAsInGame()
    {
        using var bitmap = new Bitmap(2560, 1440);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.Black);

        const int left = 320;
        const int width = 1920;
        FillRectangle(bitmap, left, 0, width, 1040, Color.FromArgb(74, 84, 98));
        FillRectangle(bitmap, left, 1040, width, 400, Color.FromArgb(48, 58, 74));
        FillRectangle(bitmap, left + 80, 1110, 320, 260, Color.FromArgb(39, 54, 82));
        FillRectangle(bitmap, left + 620, 1160, 580, 210, Color.FromArgb(31, 42, 60));
        FillRectangle(bitmap, left + 1330, 1080, 420, 310, Color.FromArgb(33, 50, 67));
        FillRectangle(bitmap, left + 1490, 40, 32, 24, Color.FromArgb(35, 190, 45));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsInGameHudWithRedStartupChatAsInGame()
    {
        using var bitmap = new Bitmap(2560, 1440);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.Black);

        const int left = 320;
        const int width = 1920;
        FillRectangle(bitmap, left, 0, width, 1040, Color.FromArgb(64, 78, 46));
        FillRectangle(bitmap, left, 1040, width, 400, Color.FromArgb(48, 58, 74));
        FillRectangle(bitmap, left + 80, 1110, 320, 260, Color.FromArgb(39, 54, 82));
        FillRectangle(bitmap, left + 620, 1160, 580, 210, Color.FromArgb(4, 4, 4));
        FillRectangle(bitmap, left + 1330, 1080, 420, 310, Color.FromArgb(33, 50, 67));

        FillRectangle(bitmap, left + 20, 590, 1040, 18, Color.FromArgb(220, 20, 20));
        FillRectangle(bitmap, left + 20, 640, 660, 18, Color.FromArgb(220, 20, 20));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsBlueCentralPanelWithoutHudAsPreGameWait()
    {
        using var bitmap = CreateDarkBitmap();
        FillRectangle(bitmap, 180, 80, 280, 220, Color.FromArgb(70, 82, 112));

        Assert.Equal(StarCraftScreenState.PreGameWait, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    private static Bitmap CreateDarkBitmap()
    {
        var bitmap = new Bitmap(640, 480);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.FromArgb(8, 8, 8));
        return bitmap;
    }

    private static void FillRectangle(Bitmap bitmap, int x, int y, int width, int height, Color color)
    {
        using var graphics = Graphics.FromImage(bitmap);
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, x, y, width, height);
    }
}
