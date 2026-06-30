using Sparring.Client;
using System.Drawing;

namespace Sparring.Tests;

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
    public void AnalyzeTreatsZergHudAsInGame()
    {
        using var bitmap = new Bitmap(640, 480);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.FromArgb(70, 44, 74));

        FillRectangle(bitmap, 0, 346, 640, 134, Color.FromArgb(82, 48, 34));
        FillRectangle(bitmap, 6, 372, 134, 102, Color.FromArgb(4, 3, 4));
        FillRectangle(bitmap, 154, 394, 260, 78, Color.FromArgb(5, 4, 5));
        FillRectangle(bitmap, 430, 354, 206, 118, Color.FromArgb(96, 55, 38));
        FillRectangle(bitmap, 490, 432, 124, 38, Color.FromArgb(6, 4, 5));
        FillRectangle(bitmap, 450, 18, 36, 18, Color.FromArgb(38, 170, 45));
        FillRectangle(bitmap, 520, 18, 28, 18, Color.FromArgb(176, 142, 50));

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
    public void AnalyzeTreatsInGameHudWithCentralBrightTerrainAsInGame()
    {
        using var bitmap = new Bitmap(640, 480);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.FromArgb(54, 74, 38));

        FillRectangle(bitmap, 0, 346, 640, 134, Color.FromArgb(38, 58, 82));
        FillRectangle(bitmap, 6, 372, 134, 102, Color.FromArgb(3, 3, 5));
        FillRectangle(bitmap, 154, 394, 260, 78, Color.FromArgb(5, 5, 7));
        FillRectangle(bitmap, 430, 354, 206, 118, Color.FromArgb(30, 52, 78));
        FillRectangle(bitmap, 420, 330, 42, 20, Color.FromArgb(176, 142, 50));

        FillRectangle(bitmap, 226, 72, 20, 20, Color.FromArgb(225, 225, 225));
        FillRectangle(bitmap, 272, 86, 26, 18, Color.FromArgb(224, 224, 224));
        FillRectangle(bitmap, 296, 138, 36, 28, Color.FromArgb(220, 216, 176));
        FillRectangle(bitmap, 270, 144, 60, 16, Color.FromArgb(155, 18, 18));
        FillRectangle(bitmap, 282, 164, 28, 24, Color.FromArgb(35, 190, 35));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsHudWithBrightMineralsAsInGame()
    {
        using var bitmap = CreateDarkBitmap();

        FillRectangle(bitmap, 0, 350, 640, 130, Color.FromArgb(48, 58, 74));
        FillRectangle(bitmap, 35, 390, 120, 72, Color.FromArgb(8, 8, 8));
        FillRectangle(bitmap, 170, 390, 240, 72, Color.FromArgb(8, 8, 8));
        FillRectangle(bitmap, 510, 370, 100, 90, Color.FromArgb(8, 8, 8));
        FillRectangle(bitmap, 430, 90, 80, 80, Color.FromArgb(228, 235, 246));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsCroppedWorldWithGreenSelectionAsInGame()
    {
        using var bitmap = new Bitmap(666, 551);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.FromArgb(72, 65, 46));
        FillRectangle(bitmap, 260, 70, 180, 150, Color.FromArgb(180, 140, 70));
        FillRectangle(bitmap, 290, 245, 120, 72, Color.FromArgb(30, 210, 35));
        FillRectangle(bitmap, 305, 260, 90, 44, Color.FromArgb(72, 65, 46));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsWorldWithCorruptedHudCaptureAsInGame()
    {
        using var bitmap = new Bitmap(666, 551);
        FillRectangle(bitmap, 0, 0, bitmap.Width, bitmap.Height, Color.FromArgb(72, 65, 46));
        FillRectangle(bitmap, 35, 125, 190, 220, Color.FromArgb(58, 86, 52));
        FillRectangle(bitmap, 250, 120, 130, 120, Color.FromArgb(182, 134, 64));
        FillRectangle(bitmap, 272, 246, 96, 48, Color.FromArgb(32, 205, 38));
        FillRectangle(bitmap, 444, 58, 34, 18, Color.FromArgb(40, 170, 44));
        FillRectangle(bitmap, 520, 58, 28, 18, Color.FromArgb(176, 142, 50));

        FillRectangle(bitmap, 0, 380, 666, 171, Color.FromArgb(28, 48, 74));
        FillRectangle(bitmap, 10, 390, 150, 112, Color.FromArgb(4, 4, 7));
        FillRectangle(bitmap, 180, 420, 250, 80, Color.FromArgb(5, 5, 8));
        FillRectangle(bitmap, 485, 410, 150, 78, Color.FromArgb(6, 6, 9));
        FillRectangle(bitmap, 0, 380, 666, 14, Color.FromArgb(180, 18, 18));
        FillRectangle(bitmap, 145, 438, 280, 22, Color.FromArgb(210, 22, 22));
        FillRectangle(bitmap, 436, 466, 120, 20, Color.FromArgb(195, 24, 24));

        Assert.Equal(StarCraftScreenState.InGame, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsDropPlayersOverlayAsBlocked()
    {
        using var bitmap = new Bitmap(640, 480);
        FillRectangle(bitmap, 0, 0, bitmap.Width, 346, Color.FromArgb(90, 74, 46));
        FillRectangle(bitmap, 0, 346, bitmap.Width, 134, Color.FromArgb(48, 58, 74));
        FillRectangle(bitmap, 6, 372, 134, 102, Color.FromArgb(4, 4, 7));
        FillRectangle(bitmap, 154, 394, 260, 78, Color.FromArgb(5, 5, 8));
        FillRectangle(bitmap, 430, 354, 206, 118, Color.FromArgb(30, 52, 78));

        FillRectangle(bitmap, 170, 40, 300, 250, Color.FromArgb(28, 36, 70));
        FillRectangle(bitmap, 190, 58, 210, 12, Color.FromArgb(226, 226, 232));
        FillRectangle(bitmap, 260, 82, 130, 12, Color.FromArgb(226, 226, 232));
        FillRectangle(bitmap, 220, 238, 220, 28, Color.FromArgb(58, 61, 76));
        FillRectangle(bitmap, 260, 246, 120, 12, Color.FromArgb(226, 226, 232));

        Assert.Equal(StarCraftScreenState.BlockedDialog, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeKeepsCreateScreenWithRedFramesAndDarkBottomAsGameRoom()
    {
        using var bitmap = CreateDarkBitmap();

        FillRectangle(bitmap, 95, 55, 3, 320, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 520, 55, 3, 320, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 95, 55, 428, 3, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 95, 375, 428, 3, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 390, 420, 140, 28, Color.FromArgb(14, 16, 18));
        FillRectangle(bitmap, 410, 424, 90, 16, Color.FromArgb(35, 180, 35));
        FillRectangle(bitmap, 0, 350, 640, 130, Color.FromArgb(4, 4, 4));

        Assert.Equal(StarCraftScreenState.GameRoom, StarCraftScreenAnalyzer.Analyze(bitmap));
    }

    [Fact]
    public void AnalyzeTreatsScenarioErrorDialogAsBlockedInsteadOfInGame()
    {
        using var bitmap = CreateDarkBitmap();

        FillRectangle(bitmap, 0, 350, 640, 130, Color.FromArgb(4, 4, 4));
        FillRectangle(bitmap, 95, 55, 3, 320, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 520, 55, 3, 320, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 95, 55, 428, 3, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 95, 375, 428, 3, Color.FromArgb(130, 20, 20));
        FillRectangle(bitmap, 220, 165, 200, 74, Color.FromArgb(10, 10, 14));
        FillRectangle(bitmap, 220, 165, 200, 2, Color.FromArgb(150, 12, 12));
        FillRectangle(bitmap, 220, 238, 200, 2, Color.FromArgb(150, 12, 12));
        FillRectangle(bitmap, 220, 165, 2, 74, Color.FromArgb(150, 12, 12));
        FillRectangle(bitmap, 418, 165, 2, 74, Color.FromArgb(150, 12, 12));
        FillRectangle(bitmap, 245, 190, 150, 8, Color.FromArgb(226, 226, 226));
        FillRectangle(bitmap, 280, 210, 80, 10, Color.FromArgb(226, 226, 226));

        Assert.Equal(StarCraftScreenState.BlockedDialog, StarCraftScreenAnalyzer.Analyze(bitmap));
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
