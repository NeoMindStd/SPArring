using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public class PracticeConfiguratorTests
{
    [Fact]
    public void Apply_WritesBotSettingsForSingleAiClient()
    {
        var root = CreateFakeStarCraftRoot();
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(bot.DllPath(root), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null);

        var path = new PracticeConfigurator(Path.Combine(root, "replays")).Apply(settings);
        var ini = BwapiIni.Load(path);

        Assert.Equal("bwapi-data/AI/TestBot.dll", ini.Get("ai", "ai"));
        Assert.Equal("maps/(4)Fighting Spirit.scx", ini.Get("auto_menu", "map"));
        Assert.Equal("AIPractice", ini.Get("auto_menu", "game"));
        Assert.Equal("Terran", ini.Get("auto_menu", "race"));
        Assert.Equal("Protoss", ini.Get("auto_menu", "enemy_race"));
        Assert.Equal("ON", ini.Get("window", "windowed"));
        Assert.Equal("OFF", ini.Get("starcraft", "sound"));
        Assert.Equal("24", ini.Get("starcraft", "speed_override"));
        Assert.Contains(Path.Combine(root, "replays"), ini.Get("auto_menu", "save_replay"));
        Assert.True(File.Exists(path + ".starai-original"));
    }

    [Fact]
    public void ApplyPlayerHost_CreatesHumanRoomWithoutAiModule()
    {
        var root = CreateFakeStarCraftRoot();
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(bot.DllPath(root), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null);

        var path = new PracticeConfigurator(Path.Combine(root, "replays")).ApplyPlayerHost(settings);
        var ini = BwapiIni.Load(path);

        Assert.Equal("", ini.Get("ai", "ai"));
        Assert.Equal("StarAIHuman", ini.Get("auto_menu", "character_name"));
        Assert.Equal("maps/(4)Fighting Spirit.scx", ini.Get("auto_menu", "map"));
        Assert.Equal("Protoss", ini.Get("auto_menu", "race"));
        Assert.Equal("Terran", ini.Get("auto_menu", "enemy_race"));
        Assert.Equal("ON", ini.Get("starcraft", "sound"));
        Assert.Equal("2", ini.Get("auto_menu", "wait_for_min_players"));
        Assert.Equal("2", ini.Get("auto_menu", "wait_for_max_players"));
        Assert.Equal("5000", ini.Get("auto_menu", "wait_for_time"));
    }

    [Fact]
    public void ApplyBotJoin_JoinsExistingRoomWithOnlyBotAi()
    {
        var root = CreateFakeStarCraftRoot();
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(bot.DllPath(root), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null);

        var path = new PracticeConfigurator(Path.Combine(root, "replays")).ApplyBotJoin(settings);
        var ini = BwapiIni.Load(path);

        Assert.Equal("bwapi-data/AI/TestBot.dll", ini.Get("ai", "ai"));
        Assert.Equal("StarAIBot", ini.Get("auto_menu", "character_name"));
        Assert.Equal("", ini.Get("auto_menu", "map"));
        Assert.Equal("AIPractice", ini.Get("auto_menu", "game"));
        Assert.Equal("Terran", ini.Get("auto_menu", "race"));
        Assert.Equal("Protoss", ini.Get("auto_menu", "enemy_race"));
        Assert.Equal("OFF", ini.Get("starcraft", "sound"));
    }

    [Fact]
    public void SeparateRuntimeRoleFlow_KeepsHumanHostIniUnchangedWhenBotJoinIsApplied()
    {
        var root = CreateFakeStarCraftRoot();
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(bot.DllPath(root), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");
        var settings = CreateSettings(root, bot, buildOption: null);
        var aiRoot = StarCraftRuntimeRoot.EnsureAiRoot(root);
        var botSettings = settings with { StarCraftRoot = aiRoot };
        var configurator = new PracticeConfigurator(Path.Combine(root, "replays"));

        var hostPath = configurator.ApplyPlayerHost(settings);
        var hostIni = BwapiIni.Load(hostPath);

        Assert.Equal("", hostIni.Get("ai", "ai"));
        Assert.Equal("StarAIHuman", hostIni.Get("auto_menu", "character_name"));

        var botPath = configurator.ApplyBotJoin(botSettings);
        var botIni = BwapiIni.Load(botPath);

        hostIni = BwapiIni.Load(hostPath);
        Assert.NotEqual(hostPath, botPath);
        Assert.Equal("", hostIni.Get("ai", "ai"));
        Assert.Equal("StarAIHuman", hostIni.Get("auto_menu", "character_name"));
        Assert.Equal("bwapi-data/AI/TestBot.dll", botIni.Get("ai", "ai"));
        Assert.Equal("StarAIBot", botIni.Get("auto_menu", "character_name"));
        Assert.Equal("", botIni.Get("auto_menu", "map"));
    }

    [Fact]
    public void Apply_ForcesUAlbertaStrategyWhenBuildPatchIsSelected()
    {
        var root = CreateFakeStarCraftRoot();
        var configPath = Path.Combine(root, "bwapi-data", "AI", "Flash_Config.txt");
        File.WriteAllText(configPath, """
            {
              "Strategy": {
                "Protoss": "Protoss_ZealotRush",
                "UseEnemySpecificStrategy": true
              }
            }
            """);

        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Protoss);
        File.WriteAllText(bot.DllPath(root), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, new BuildOption(
            "dragoon",
            "Dragoon",
            "Force dragoon",
            new BuildPatch(BuildPatchKind.UAlbertaRaceStrategy, "bwapi-data/AI/Flash_Config.txt", "Protoss_DragoonRush")));

        new PracticeConfigurator(Path.Combine(root, "replays")).Apply(settings);

        var text = File.ReadAllText(configPath);
        Assert.Contains("\"Protoss\": \"Protoss_DragoonRush\"", text);
        Assert.Contains("\"UseEnemySpecificStrategy\": false", text);
        Assert.True(File.Exists(configPath + ".starai-original"));
    }

    [Fact]
    public void Apply_ForcesWeightedMatchupStrategyForPlayerRace()
    {
        var root = CreateFakeStarCraftRoot();
        var configPath = Path.Combine(root, "bwapi-data", "AI", "Steamhammer.json");
        File.WriteAllText(configPath, """
            {
              "Strategy": {
                "ZvP": {
                  "Zerg": [
                    { "Weight": 5, "Strategy": "FastPool" },
                    { "Weight": 10, "Strategy": "OverpoolSpeed" }
                  ]
                },
                "ZvT": {
                  "Zerg": [
                    { "Weight": 5, "Strategy": "FastPool" }
                  ]
                }
              }
            }
            """);

        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Zerg);
        File.WriteAllText(bot.DllPath(root), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, new BuildOption(
            "hydra",
            "Hydra",
            "Force hydra",
            new BuildPatch(BuildPatchKind.MatchupWeightedStrategy, "bwapi-data/AI/Steamhammer.json", "3HatchHydraExpo")));

        new PracticeConfigurator(Path.Combine(root, "replays")).Apply(settings);

        var text = File.ReadAllText(configPath);
        Assert.Contains("\"ZvP\"", text);
        Assert.Contains("\"Strategy\": \"3HatchHydraExpo\"", text);
        Assert.Contains("\"Weight\": 100", text);
        Assert.Contains("\"ZvT\"", text);
    }

    [Fact]
    public void ApplyBotJoin_CopiesSteamhammerConfigToRuntimeLocation()
    {
        var root = CreateFakeStarCraftRoot();
        var steamhammerDir = Path.Combine(root, "bwapi-data", "AI", "practice-bots", "Steamhammer");
        Directory.CreateDirectory(steamhammerDir);
        File.WriteAllText(Path.Combine(steamhammerDir, "Steamhammer.dll"), "");
        File.WriteAllText(Path.Combine(steamhammerDir, "Steamhammer_5.2.3.json"), "{}");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var bot = CreateFakeBot("bwapi-data/AI/practice-bots/Steamhammer/Steamhammer.dll", Race.Zerg);
        var settings = CreateSettings(root, bot, buildOption: null);

        new PracticeConfigurator(Path.Combine(root, "replays")).ApplyBotJoin(settings);

        Assert.True(File.Exists(Path.Combine(root, "bwapi-data", "AI", "Steamhammer_5.2.3.json")));
    }

    [Fact]
    public void Validate_BlocksKnownAccessViolationBots()
    {
        var root = CreateFakeStarCraftRoot();
        var bot = new BotProfile(
            "xiaoyicog2019",
            "XIAOYICOG2019",
            Race.Terran,
            DifficultyTier.Main,
            "bwapi-data/AI/XIAOYI.dll",
            "style",
            "hints",
            "risk",
            Array.Empty<BuildOption>());
        File.WriteAllText(bot.DllPath(root), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null);

        var issues = new PracticeConfigurator().Validate(settings);

        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("access-violation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetAvailableBots_ExcludesKnownAccessViolationBots()
    {
        var root = CreateFakeStarCraftRoot();
        var sampleIds = new[] { "xiaoyicog2019", "stone", "nitekatt" };
        foreach (var bot in PracticeCatalog.GetDefaultBots().Where(bot => sampleIds.Contains(bot.Id)))
        {
            var dllPath = bot.DllPath(root);
            Directory.CreateDirectory(Path.GetDirectoryName(dllPath)!);
            File.WriteAllText(dllPath, "");
        }

        var available = PracticeCatalog.GetAvailableBots(root);

        Assert.DoesNotContain(available, bot => bot.Id.Equals("xiaoyicog2019", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(available, bot => bot.Id.Equals("stone", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(available, bot => bot.Id.Equals("nitekatt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WModeConfigurator_WritesFullscreenWithoutCursorClipWhenWindowedIsOff()
    {
        var root = CreateFakeStarCraftRoot();
        File.WriteAllText(Path.Combine(root, "wmode.ini"), """
            [W-MODE]
            WindowClientX=10
            WindowClientY=20
            ClipCursor=1
            EnableWindowMove=0
            """);

        var path = WModeConfigurator.Apply(root, windowedMode: false, clipCursor: true);
        var ini = BwapiIni.Load(path);

        Assert.Equal("0", ini.Get("W-MODE", "Windowed"));
        Assert.Equal("0", ini.Get("W-MODE", "ClipCursor"));
        Assert.Equal("1", ini.Get("W-MODE", "SaveClipCursor"));
        Assert.Equal("1", ini.Get("W-MODE", "EnableWindowMove"));
        Assert.Equal("0", ini.Get("W-MODE", "AlwaysOnTop"));
        Assert.Equal("0", ini.Get("W-MODE", "DisableControls"));
        Assert.Equal("10", ini.Get("W-MODE", "WindowClientX"));
    }

    [Fact]
    public void WModeConfigurator_WritesWindowedCursorClipOnlyWhenRequested()
    {
        var root = CreateFakeStarCraftRoot();

        var path = WModeConfigurator.Apply(root, windowedMode: true, clipCursor: false);
        var ini = BwapiIni.Load(path);

        Assert.Equal("1", ini.Get("W-MODE", "Windowed"));
        Assert.Equal("0", ini.Get("W-MODE", "ClipCursor"));
        Assert.Equal("1", ini.Get("W-MODE", "SaveClipCursor"));

        WModeConfigurator.Apply(root, windowedMode: true, clipCursor: true);
        ini = BwapiIni.Load(path);

        Assert.Equal("1", ini.Get("W-MODE", "Windowed"));
        Assert.Equal("1", ini.Get("W-MODE", "ClipCursor"));
        Assert.Equal("1", ini.Get("W-MODE", "SaveClipCursor"));
    }

    [Fact]
    public void CrashLogInspector_DetectsRecentApmAlertCrash()
    {
        var root = CreateFakeStarCraftRoot();
        var errorDirectory = Path.Combine(root, "Errors");
        Directory.CreateDirectory(errorDirectory);
        File.WriteAllText(Path.Combine(errorDirectory, "test.ERR"), """
            Call stack:
            068751E3 C:\starai\SC116AI\Plugins\APMAlert.bwl
            """);

        Assert.True(CrashLogInspector.HasRecentApmAlertCrash(root, TimeSpan.FromDays(1)));
    }

    [Fact]
    public void CrashLogInspector_IgnoresOldApmAlertCrash()
    {
        var root = CreateFakeStarCraftRoot();
        var errorDirectory = Path.Combine(root, "Errors");
        Directory.CreateDirectory(errorDirectory);
        var errorPath = Path.Combine(errorDirectory, "old.ERR");
        File.WriteAllText(errorPath, "Plugins\\APMAlert.bwl");
        File.SetLastWriteTime(errorPath, DateTime.Now.AddDays(-30));

        Assert.False(CrashLogInspector.HasRecentApmAlertCrash(root, TimeSpan.FromDays(1)));
    }

    private static string CreateFakeStarCraftRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "bwapi-data", "AI"));
        Directory.CreateDirectory(Path.Combine(root, "maps"));
        File.WriteAllText(Path.Combine(root, "StarCraft.exe"), "");
        File.WriteAllText(Path.Combine(root, "bwapi-data", "bwapi.ini"), """
            [ai]
            ai =
            [auto_menu]
            auto_menu = OFF
            [window]
            windowed = OFF
            [starcraft]
            speed_override = -1
            """);
        return root;
    }

    private static BotProfile CreateFakeBot(string dllPath, Race race)
    {
        return new BotProfile(
            "test",
            "Test Bot",
            race,
            DifficultyTier.Main,
            dllPath,
            "style",
            "hints",
            "risk",
            Array.Empty<BuildOption>());
    }

    private static PracticeSettings CreateSettings(string root, BotProfile bot, BuildOption? buildOption)
    {
        return new PracticeSettings(
            root,
            bot,
            new MapProfile("Fighting Spirit", "maps/(4)Fighting Spirit.scx", 4),
            Race.Protoss,
            "AIPractice",
            WindowedMode: true,
            SpeedOverrideMs: 24,
            BuildOption: buildOption);
    }
}
