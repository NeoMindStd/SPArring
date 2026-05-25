using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public class PracticeConfiguratorTests
{
    [Fact]
    public void Apply_WritesExpectedBwapiSettings()
    {
        var root = CreateFakeStarCraftRoot();
        var bot = new BotProfile(
            "test",
            "Test Bot",
            Race.Terran,
            DifficultyTier.Main,
            "bwapi-data/AI/TestBot.dll",
            "style",
            "hints",
            "risk",
            Array.Empty<BuildOption>());
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "TestBot.dll"), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = new PracticeSettings(
            root,
            bot,
            new MapProfile("Fighting Spirit", "maps/(4)Fighting Spirit.scx", 4),
            Race.Protoss,
            "AIPractice",
            WindowedMode: true,
            SpeedOverrideMs: 24,
            BuildOption: null,
            EnableCoachAi: false);

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
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "TestBot.dll"), "");
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
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "TestBot.dll"), "");
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
    public void ApplyCoachClient_WritesCoachAiSettingsAndCreatesDefaultJson()
    {
        var root = CreateFakeStarCraftRoot();
        var coachDir = Path.Combine(root, "bwapi-data", "AI", "CoachAI");
        Directory.CreateDirectory(coachDir);
        var coachDll = Path.Combine(coachDir, "AnyRace_CoachAI.dll");
        File.WriteAllText(coachDll, "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, CreateFakeBot("bwapi-data/AI/MissingBot.dll", Race.Terran), buildOption: null) with
        {
            EnableCoachAi = true
        };

        var path = new PracticeConfigurator(Path.Combine(root, "replays")).ApplyCoachClient(settings, coachDll);
        var ini = BwapiIni.Load(path);
        var coachConfig = Path.Combine(root, "bwapi-data", "AnyRace_CoachAI.json");

        Assert.Equal("bwapi-data/AI/CoachAI/AnyRace_CoachAI.dll", ini.Get("ai", "ai"));
        Assert.Equal("", ini.Get("auto_menu", "map"));
        Assert.Equal("Protoss", ini.Get("auto_menu", "race"));
        Assert.Equal("Terran", ini.Get("auto_menu", "enemy_race"));
        Assert.Equal("ON", ini.Get("starcraft", "sound"));
        Assert.True(File.Exists(coachConfig));
        Assert.Contains("\"autoTrainWorkers\": false", File.ReadAllText(coachConfig));
        Assert.Contains("\"autoMine\": false", File.ReadAllText(coachConfig));
        Assert.Contains("\"autoBuildSuppliesBeforeBlocked\": -200", File.ReadAllText(coachConfig));
        Assert.Contains("\"maxProductionBuildingQueue\": 999999", File.ReadAllText(coachConfig));
        Assert.Contains("\"workerCutWarningEvery\": 60", File.ReadAllText(coachConfig));
        Assert.Contains("\"idleWorkerWarningEvery\": 60", File.ReadAllText(coachConfig));
        Assert.Contains("\"idleProductionBuildingWarningEvery\": 60", File.ReadAllText(coachConfig));
        Assert.Contains("\"idleFightingUnitWarningEvery\": 60", File.ReadAllText(coachConfig));
        Assert.Contains("\"workersCutCalculationPeriod\": 600", File.ReadAllText(coachConfig));
        Assert.Contains("\"TimedBo1\"", File.ReadAllText(coachConfig));
    }

    [Fact]
    public void ApplyCoachAiBuildPreset_WritesTimedBoWithoutJsonOptionsError()
    {
        var root = CreateFakeStarCraftRoot();
        var preset = CoachAiBuildPresets.DefaultForRace(Race.Protoss);

        PracticeConfigurator.ApplyCoachAiBuildPreset(root, preset);

        var coachConfig = Path.Combine(root, "bwapi-data", "AnyRace_CoachAI.json");
        var text = File.ReadAllText(coachConfig);
        Assert.Contains(preset.TitleForOverlay, text);
        Assert.Contains("autoTrainWorkers", text);
        Assert.Contains("\"autoMine\": false", text);
        Assert.Contains("\"maxProductionBuildingQueue\": 999999", text);
    }

    [Fact]
    public void Validate_DoesNotWarnAboutCoachAiWhenDllExists()
    {
        var root = CreateFakeStarCraftRoot();
        var coachDir = Path.Combine(root, "bwapi-data", "AI", "CoachAI");
        Directory.CreateDirectory(coachDir);
        File.WriteAllText(Path.Combine(coachDir, "AnyRace_CoachAI.dll"), "");
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "TestBot.dll"), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran), buildOption: null) with
        {
            EnableCoachAi = true
        };

        var issues = new PracticeConfigurator().Validate(settings);

        Assert.DoesNotContain(issues, issue => issue.Message.Contains("CoachAI", StringComparison.OrdinalIgnoreCase));
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
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "XIAOYI.dll"), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null);

        var issues = new PracticeConfigurator().Validate(settings);

        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("차단", StringComparison.OrdinalIgnoreCase));
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
    public void ApplyPlayerHost_CreatesRoomWithPlayerRaceAndSelectedMap()
    {
        var root = CreateFakeStarCraftRoot();
        var coachDir = Path.Combine(root, "bwapi-data", "AI", "CoachAI");
        Directory.CreateDirectory(coachDir);
        var coachDll = Path.Combine(coachDir, "AnyRace_CoachAI.dll");
        File.WriteAllText(coachDll, "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, CreateFakeBot("bwapi-data/AI/MissingBot.dll", Race.Terran), buildOption: null) with
        {
            EnableCoachAi = true
        };

        var path = new PracticeConfigurator(Path.Combine(root, "replays")).ApplyPlayerHost(settings, coachDll);
        var ini = BwapiIni.Load(path);

        Assert.Equal("bwapi-data/AI/CoachAI/AnyRace_CoachAI.dll", ini.Get("ai", "ai"));
        Assert.Equal("maps/(4)Fighting Spirit.scx", ini.Get("auto_menu", "map"));
        Assert.Equal("Protoss", ini.Get("auto_menu", "race"));
        Assert.Equal("Terran", ini.Get("auto_menu", "enemy_race"));
        Assert.Equal("ON", ini.Get("starcraft", "sound"));
        Assert.Equal("2", ini.Get("auto_menu", "wait_for_max_players"));
        Assert.Equal("5000", ini.Get("auto_menu", "wait_for_time"));
        Assert.Contains("StarAI_%BOTNAME6%_%MAP%_$H$M$S.rep", ini.Get("auto_menu", "save_replay"));
    }

    [Fact]
    public void ApplyMultiInstanceSparring_WritesPlayerAndBotIntoSingleBwapiIni()
    {
        var root = CreateFakeStarCraftRoot();
        var coachDir = Path.Combine(root, "bwapi-data", "AI", "CoachAI");
        Directory.CreateDirectory(coachDir);
        var coachDll = Path.Combine(coachDir, "AnyRace_CoachAI.dll");
        File.WriteAllText(coachDll, "");
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "TestBot.dll"), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null) with
        {
            EnableCoachAi = true
        };

        var path = new PracticeConfigurator(Path.Combine(root, "replays")).ApplyMultiInstanceSparring(settings, coachDll);
        var ini = BwapiIni.Load(path);

        Assert.Equal("bwapi-data/AI/CoachAI/AnyRace_CoachAI.dll,bwapi-data/AI/TestBot.dll", ini.Get("ai", "ai"));
        Assert.Equal("maps/(4)Fighting Spirit.scx", ini.Get("auto_menu", "map"));
        Assert.Equal("AIPractice", ini.Get("auto_menu", "game"));
        Assert.Equal("Protoss,Terran", ini.Get("auto_menu", "race"));
        Assert.Equal("Terran", ini.Get("auto_menu", "enemy_race"));
        Assert.Equal("ON", ini.Get("starcraft", "sound"));
        Assert.Equal("2", ini.Get("auto_menu", "wait_for_max_players"));
    }

    [Fact]
    public void ApplyBotJoin_WithFirstInstanceAiWritesOnlyBotForJoinClient()
    {
        var root = CreateFakeStarCraftRoot();
        var coachDir = Path.Combine(root, "bwapi-data", "AI", "CoachAI");
        Directory.CreateDirectory(coachDir);
        var coachDll = Path.Combine(coachDir, "AnyRace_CoachAI.dll");
        File.WriteAllText(coachDll, "");
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "TestBot.dll"), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null);

        var path = new PracticeConfigurator(Path.Combine(root, "replays")).ApplyBotJoin(settings, coachDll);
        var ini = BwapiIni.Load(path);

        Assert.Equal("bwapi-data/AI/TestBot.dll", ini.Get("ai", "ai"));
        Assert.Equal("Terran", ini.Get("auto_menu", "race"));
        Assert.Equal("StarAIBot", ini.Get("auto_menu", "character_name"));
    }

    [Fact]
    public void ApplyBotJoin_JoinsExistingRoomWithBotRace()
    {
        var root = CreateFakeStarCraftRoot();
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "TestBot.dll"), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null);

        var path = new PracticeConfigurator(Path.Combine(root, "replays")).ApplyBotJoin(settings);
        var ini = BwapiIni.Load(path);

        Assert.Equal("bwapi-data/AI/TestBot.dll", ini.Get("ai", "ai"));
        Assert.Equal("", ini.Get("auto_menu", "map"));
        Assert.Equal("Terran", ini.Get("auto_menu", "race"));
        Assert.Equal("Protoss", ini.Get("auto_menu", "enemy_race"));
        Assert.Equal("OFF", ini.Get("starcraft", "sound"));
    }

    [Fact]
    public void SharedRuntimeRoleFlow_SwitchesFromPlayerHostToBotJoin()
    {
        var root = CreateFakeStarCraftRoot();
        var coachDir = Path.Combine(root, "bwapi-data", "AI", "CoachAI");
        Directory.CreateDirectory(coachDir);
        var coachDll = Path.Combine(coachDir, "AnyRace_CoachAI.dll");
        File.WriteAllText(coachDll, "");
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(Path.Combine(root, "bwapi-data", "AI", "TestBot.dll"), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");

        var settings = CreateSettings(root, bot, buildOption: null) with
        {
            EnableCoachAi = true
        };
        var configurator = new PracticeConfigurator(Path.Combine(root, "replays"));

        var hostPath = configurator.ApplyPlayerHost(settings, coachDll);
        var hostIni = BwapiIni.Load(hostPath);

        Assert.Equal("bwapi-data/AI/CoachAI/AnyRace_CoachAI.dll", hostIni.Get("ai", "ai"));
        Assert.Equal("StarAIHuman", hostIni.Get("auto_menu", "character_name"));
        Assert.Equal("maps/(4)Fighting Spirit.scx", hostIni.Get("auto_menu", "map"));
        Assert.Equal("Protoss", hostIni.Get("auto_menu", "race"));
        Assert.Equal("Terran", hostIni.Get("auto_menu", "enemy_race"));
        Assert.Equal("ON", hostIni.Get("starcraft", "sound"));

        var botPath = configurator.ApplyBotJoin(settings);
        var botIni = BwapiIni.Load(botPath);

        Assert.Equal(hostPath, botPath);
        Assert.Equal("bwapi-data/AI/TestBot.dll", botIni.Get("ai", "ai"));
        Assert.Equal("StarAIBot", botIni.Get("auto_menu", "character_name"));
        Assert.Equal("", botIni.Get("auto_menu", "map"));
        Assert.Equal("AIPractice", botIni.Get("auto_menu", "game"));
        Assert.Equal("Terran", botIni.Get("auto_menu", "race"));
        Assert.Equal("Protoss", botIni.Get("auto_menu", "enemy_race"));
        Assert.Equal("OFF", botIni.Get("starcraft", "sound"));
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
    public void EnsureAiRoot_CopiesStarCraftRuntimeFilesToSeparateFolder()
    {
        var root = CreateFakeStarCraftRoot();
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "map");
        File.WriteAllText(Path.Combine(root, "patch_rt.mpq"), "patch");
        Directory.CreateDirectory(Path.Combine(root, "Errors"));
        File.WriteAllText(Path.Combine(root, "Errors", "crash.ERR"), "local crash log");

        var aiRoot = StarCraftRuntimeRoot.EnsureAiRoot(root);

        Assert.NotEqual(root, aiRoot);
        Assert.True(File.Exists(Path.Combine(aiRoot, "StarCraft.exe")));
        Assert.Equal("map", File.ReadAllText(Path.Combine(aiRoot, "maps", "(4)Fighting Spirit.scx")));
        Assert.Equal("patch", File.ReadAllText(Path.Combine(aiRoot, "patch_rt.mpq")));
        Assert.False(File.Exists(Path.Combine(aiRoot, "Errors", "crash.ERR")));
    }

    [Fact]
    public void EnsureAiRoot_SkipsLockedRuntimeFileWhenAiCopyAlreadyExists()
    {
        var root = CreateFakeStarCraftRoot();
        var patch = Path.Combine(root, "patch_rt.mpq");
        File.WriteAllText(patch, "locked source");
        var aiRoot = StarCraftRuntimeRoot.GetAiRoot(root);
        Directory.CreateDirectory(aiRoot);
        File.WriteAllText(Path.Combine(aiRoot, "patch_rt.mpq"), "existing ai copy");

        using var lockedPatch = new FileStream(patch, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = StarCraftRuntimeRoot.EnsureAiRoot(root);

        Assert.Equal(aiRoot, result);
        Assert.Equal("existing ai copy", File.ReadAllText(Path.Combine(aiRoot, "patch_rt.mpq")));
    }

    [Fact]
    public void SplitRuntimeFlow_KeepsPlayerAndBotIniSeparate()
    {
        var root = CreateFakeStarCraftRoot();
        var bot = CreateFakeBot("bwapi-data/AI/TestBot.dll", Race.Terran);
        File.WriteAllText(bot.DllPath(root), "");
        File.WriteAllText(Path.Combine(root, "maps", "(4)Fighting Spirit.scx"), "");
        var coachDir = Path.Combine(root, "bwapi-data", "AI", "CoachAI");
        Directory.CreateDirectory(coachDir);
        var coachDll = Path.Combine(coachDir, "AnyRace_CoachAI.dll");
        File.WriteAllText(coachDll, "");

        var settings = CreateSettings(root, bot, buildOption: null) with
        {
            EnableCoachAi = true
        };
        var configurator = new PracticeConfigurator(Path.Combine(root, "replays"));
        var aiRoot = StarCraftRuntimeRoot.EnsureAiRoot(root);

        var playerIniPath = configurator.ApplyPlayerHost(settings, coachDll);
        var botIniPath = configurator.ApplyBotJoin(settings with { StarCraftRoot = aiRoot, EnableCoachAi = false });
        var playerIni = BwapiIni.Load(playerIniPath);
        var botIni = BwapiIni.Load(botIniPath);

        Assert.NotEqual(playerIniPath, botIniPath);
        Assert.Equal("bwapi-data/AI/CoachAI/AnyRace_CoachAI.dll", playerIni.Get("ai", "ai"));
        Assert.Equal("maps/(4)Fighting Spirit.scx", playerIni.Get("auto_menu", "map"));
        Assert.Equal("Protoss", playerIni.Get("auto_menu", "race"));
        Assert.Equal("ON", playerIni.Get("starcraft", "sound"));

        Assert.Equal("bwapi-data/AI/TestBot.dll", botIni.Get("ai", "ai"));
        Assert.Equal("", botIni.Get("auto_menu", "map"));
        Assert.Equal("Terran", botIni.Get("auto_menu", "race"));
        Assert.Equal("OFF", botIni.Get("starcraft", "sound"));
    }

    [Fact]
    public void WModeConfigurator_WritesCursorClipAndKeepsWindowMoveEnabled()
    {
        var root = CreateFakeStarCraftRoot();
        File.WriteAllText(Path.Combine(root, "wmode.ini"), """
            [W-MODE]
            WindowClientX=10
            WindowClientY=20
            ClipCursor=1
            EnableWindowMove=0
            """);

        var path = WModeConfigurator.Apply(root, clipCursor: false);
        var ini = BwapiIni.Load(path);

        Assert.Equal("0", ini.Get("W-MODE", "ClipCursor"));
        Assert.Equal("0", ini.Get("W-MODE", "SaveClipCursor"));
        Assert.Equal("1", ini.Get("W-MODE", "EnableWindowMove"));
        Assert.Equal("0", ini.Get("W-MODE", "AlwaysOnTop"));
        Assert.Equal("0", ini.Get("W-MODE", "DisableControls"));
        Assert.Equal("10", ini.Get("W-MODE", "WindowClientX"));
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
            BuildOption: buildOption,
            EnableCoachAi: false);
    }
}
