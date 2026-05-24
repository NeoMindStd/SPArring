using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public class BwapiIniTests
{
    [Fact]
    public void Set_UpdatesExistingKeyInsideSection()
    {
        var ini = BwapiIni.Parse("""
            [ai]
            ai = old.dll

            [auto_menu]
            map =
            game = OldGame
            """);

        ini.Set("auto_menu", "game", "AIPractice");

        Assert.Equal("AIPractice", ini.Get("auto_menu", "game"));
        Assert.Contains("game = AIPractice", ini.ToString());
    }

    [Fact]
    public void Set_InsertsMissingKeyBeforeNextSection()
    {
        var ini = BwapiIni.Parse("""
            [auto_menu]
            game = AIPractice
            [window]
            windowed = OFF
            """);

        ini.Set("auto_menu", "map", "maps/(4)Fighting Spirit.scx");

        var text = ini.ToString();
        Assert.Contains("map = maps/(4)Fighting Spirit.scx", text);
        Assert.True(text.IndexOf("map =", StringComparison.Ordinal) < text.IndexOf("[window]", StringComparison.Ordinal));
    }

    [Fact]
    public void Set_AppendsMissingSection()
    {
        var ini = BwapiIni.Parse("[ai]\nai = bot.dll\n");

        ini.Set("starcraft", "speed_override", "24");

        Assert.Equal("24", ini.Get("starcraft", "speed_override"));
        Assert.Contains("[starcraft]", ini.ToString());
    }
}
