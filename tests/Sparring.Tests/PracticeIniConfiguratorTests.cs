using Sparring.Core;

namespace Sparring.Tests;

public sealed class PracticeIniConfiguratorTests
{
    [Fact]
    public void ApplyPlayerHostRemovesAiModuleKeyAndUsesTournamentModule()
    {
        var root = NewTempRoot();
        var settings = ClientSettings(root, ClientRuntimeRole.PlayerHost, aiModule: string.Empty, sound: true);

        var path = PracticeIniConfigurator.Apply(settings, PracticeRuntimeOptions.Defaults());
        var ini = IniDocument.Parse(File.ReadAllText(path));

        Assert.Null(ini.Get("ai", "ai"));
        Assert.Equal(PracticeIniConfigurator.PlayerTournamentModule, ini.Get("ai", "tournament"));
        Assert.Equal("SparringHuman", ini.Get("auto_menu", "character_name"));
        Assert.Equal("Fighting.scx", ini.Get("auto_menu", "map"));
        Assert.Equal("ON", ini.Get("starcraft", "sound"));
    }

    [Fact]
    public void ApplyAiOpponentWritesBotModuleAndMutesSound()
    {
        var root = NewTempRoot();
        var settings = ClientSettings(root, ClientRuntimeRole.AiOpponent, @"bwapi-data\AI\Sparring\Bot.dll", sound: false);

        var path = PracticeIniConfigurator.Apply(settings, PracticeRuntimeOptions.Defaults());
        var ini = IniDocument.Parse(File.ReadAllText(path));

        Assert.Equal(@"bwapi-data\AI\Sparring\Bot.dll", ini.Get("ai", "ai"));
        Assert.Equal(string.Empty, ini.Get("ai", "tournament"));
        Assert.Equal(string.Empty, ini.Get("auto_menu", "map"));
        Assert.Equal("OFF", ini.Get("starcraft", "sound"));
    }

    [Fact]
    public void ApplyWritesConfiguredGameSpeedOverride()
    {
        var root = NewTempRoot();
        var settings = ClientSettings(root, ClientRuntimeRole.PlayerHost, aiModule: string.Empty, sound: true);

        var path = PracticeIniConfigurator.Apply(
            settings,
            PracticeRuntimeOptions.Defaults() with { SpeedOverrideMs = 42 });
        var ini = IniDocument.Parse(File.ReadAllText(path));

        Assert.Equal("42", ini.Get("starcraft", "speed_override"));
    }

    [Fact]
    public void DisableAutoMenuTurnsOffPostGameAutomationWithoutChangingAiModule()
    {
        var root = NewTempRoot();
        var settings = ClientSettings(root, ClientRuntimeRole.AiOpponent, @"bwapi-data\AI\Sparring\Bot.dll", sound: false);
        PracticeIniConfigurator.Apply(settings, PracticeRuntimeOptions.Defaults());

        var path = PracticeIniConfigurator.DisableAutoMenu(root);
        var ini = IniDocument.Parse(File.ReadAllText(path));

        Assert.Equal(@"bwapi-data\AI\Sparring\Bot.dll", ini.Get("ai", "ai"));
        Assert.Equal("OFF", ini.Get("auto_menu", "auto_menu"));
        Assert.Equal(string.Empty, ini.Get("auto_menu", "map"));
        Assert.Equal(string.Empty, ini.Get("auto_menu", "game"));
    }

    private static ClientLaunchSettings ClientSettings(string root, ClientRuntimeRole role, string aiModule, bool sound)
    {
        return new ClientLaunchSettings(
            role,
            root,
            role == ClientRuntimeRole.PlayerHost ? "SparringHuman" : "SparringBot",
            role == ClientRuntimeRole.PlayerHost ? StarCraftRace.Terran : StarCraftRace.Zerg,
            role == ClientRuntimeRole.PlayerHost ? StarCraftRace.Zerg : StarCraftRace.Terran,
            role == ClientRuntimeRole.PlayerHost ? "Fighting.scx" : string.Empty,
            "Sparring Practice",
            aiModule,
            "Bot.dll",
            BotExecutableKind.Dll,
            sound,
            WindowedMode: true,
            Borderless: role == ClientRuntimeRole.PlayerHost,
            ClipCursor: false,
            ApmAlertEnabled: false,
            EnableWModePlugin: false,
            CncDdrawMode: role == ClientRuntimeRole.PlayerHost
                ? CncDdrawMode.BorderlessFullscreen
                : CncDdrawMode.Windowed);
    }

    private static string NewTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-ini-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "bwapi-data"));
        File.WriteAllText(Path.Combine(root, "bwapi-data", "bwapi.ini"), "[ai]\r\nai = old.dll\r\n");
        return root;
    }
}
