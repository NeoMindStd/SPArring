using Sparring.Core;

namespace Sparring.Tests;

public sealed class RuntimeProvisionerTests
{
    [Fact]
    public void CopyRuntimeBaseCopiesGameFilesButSkipsRuntimeArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-provision-tests", Guid.NewGuid().ToString("N"));
        var sourceRoot = Path.Combine(root, "source");
        var targetRoot = Path.Combine(root, "target");
        Directory.CreateDirectory(Path.Combine(sourceRoot, "bwapi-data", "write"));
        Directory.CreateDirectory(Path.Combine(sourceRoot, "bwapi-data", "logs"));
        Directory.CreateDirectory(Path.Combine(sourceRoot, "Errors"));
        Directory.CreateDirectory(Path.Combine(sourceRoot, "maps"));
        File.WriteAllText(Path.Combine(sourceRoot, "StarCraft.exe"), "exe");
        File.WriteAllText(Path.Combine(sourceRoot, "maps", "Fighting.scx"), "map");
        File.WriteAllText(Path.Combine(sourceRoot, "bwapi-data", "write", "game_log.txt"), "log");
        File.WriteAllText(Path.Combine(sourceRoot, "bwapi-data", "logs", "bwapi.log"), "log");
        File.WriteAllText(Path.Combine(sourceRoot, "Errors", "crash.txt"), "crash");
        File.WriteAllText(Path.Combine(sourceRoot, "test.rep"), "replay");

        RuntimeProvisioner.CopyRuntimeBase(sourceRoot, targetRoot);

        Assert.True(File.Exists(Path.Combine(targetRoot, "StarCraft.exe")));
        Assert.True(File.Exists(Path.Combine(targetRoot, "maps", "Fighting.scx")));
        Assert.False(File.Exists(Path.Combine(targetRoot, "bwapi-data", "write", "game_log.txt")));
        Assert.False(File.Exists(Path.Combine(targetRoot, "bwapi-data", "logs", "bwapi.log")));
        Assert.False(File.Exists(Path.Combine(targetRoot, "Errors", "crash.txt")));
        Assert.False(File.Exists(Path.Combine(targetRoot, "test.rep")));
    }

    [Fact]
    public void PrepareBotMatchRuntimeAssetsCopiesMapAndBothBotsToSeparateRuntimes()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-provision-tests", Guid.NewGuid().ToString("N"));
        var leftRoot = Path.Combine(root, "left");
        var rightRoot = Path.Combine(root, "right");
        var mapSource = Path.Combine(root, "maps", "(2)Test.scx");
        var leftBotSource = Path.Combine(root, "bots", "LeftBot");
        var rightBotSource = Path.Combine(root, "bots", "RightBot");
        Directory.CreateDirectory(Path.GetDirectoryName(mapSource)!);
        Directory.CreateDirectory(leftBotSource);
        Directory.CreateDirectory(rightBotSource);
        File.WriteAllText(mapSource, "map");
        File.WriteAllText(Path.Combine(leftBotSource, "LeftBot.dll"), "left");
        File.WriteAllText(Path.Combine(rightBotSource, "RightBot.dll"), "right");
        var mapId = Guid.NewGuid();
        var leftBot = new PracticeBot(
            Guid.NewGuid(),
            "LeftBot",
            StarCraftRace.Protoss,
            "LeftBot.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            new HashSet<Guid> { mapId },
            null,
            null,
            leftBotSource);
        var rightBot = new PracticeBot(
            Guid.NewGuid(),
            "RightBot",
            StarCraftRace.Terran,
            "RightBot.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            new HashSet<Guid> { mapId },
            null,
            null,
            rightBotSource);
        var map = new PracticeMap(mapId, "(2)Test", "(2)Test.scx", null, true, mapSource);
        var plan = new BotMatchLaunchPlan(
            Client(leftRoot, ClientRuntimeRole.AiOpponent, "LeftBot.dll", "(2)Test.scx"),
            Client(rightRoot, ClientRuntimeRole.AiOpponent, "RightBot.dll", string.Empty),
            leftBot,
            rightBot,
            map);

        var prepared = RuntimeProvisioner.PrepareBotMatchRuntimeAssets(plan);

        Assert.True(File.Exists(Path.Combine(leftRoot, "maps", "Sparring", "(2)Test.scx")));
        Assert.True(File.Exists(Path.Combine(rightRoot, "maps", "Sparring", "(2)Test.scx")));
        Assert.True(File.Exists(Path.Combine(leftRoot, prepared.Left.BotExecutable)));
        Assert.True(File.Exists(Path.Combine(rightRoot, prepared.Right.BotExecutable)));
        Assert.Equal(prepared.Left.BotExecutable, prepared.Left.AiModule);
        Assert.Equal(prepared.Right.BotExecutable, prepared.Right.AiModule);
        Assert.Equal(Path.Combine("maps", "Sparring", "(2)Test.scx"), prepared.Left.MapFileName);
        Assert.Equal(string.Empty, prepared.Right.MapFileName);
        Assert.True(Directory.Exists(Path.Combine(leftRoot, "bwapi-data", "write")));
        Assert.True(Directory.Exists(Path.Combine(rightRoot, "bwapi-data", "write")));
    }

    [Fact]
    public void PrepareRuntimeAssetsCopiesMapToBothRuntimesAndBotToAiRuntime()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-provision-tests", Guid.NewGuid().ToString("N"));
        var playerRoot = Path.Combine(root, "player");
        var aiRoot = Path.Combine(root, "ai");
        var mapSource = Path.Combine(root, "schnail", "maps", "(2)Test.scx");
        var botSource = Path.Combine(root, "schnail", "bots", "TestBot");
        Directory.CreateDirectory(Path.GetDirectoryName(mapSource)!);
        Directory.CreateDirectory(botSource);
        File.WriteAllText(mapSource, "map");
        File.WriteAllText(Path.Combine(botSource, "TestBot.dll"), "bot");
        var mapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var bot = new PracticeBot(
            botId,
            "TestBot",
            StarCraftRace.Zerg,
            "TestBot.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            new HashSet<Guid> { mapId },
            null,
            null,
            botSource);
        var map = new PracticeMap(mapId, "(2)Test", "(2)Test.scx", null, true, mapSource);
        var plan = new PracticeLaunchPlan(
            Client(playerRoot, ClientRuntimeRole.PlayerHost, string.Empty, "placeholder.scx"),
            Client(aiRoot, ClientRuntimeRole.AiOpponent, "TestBot.dll", string.Empty),
            bot,
            map);

        var prepared = RuntimeProvisioner.PrepareRuntimeAssets(plan);

        Assert.True(File.Exists(Path.Combine(playerRoot, "maps", "Sparring", "(2)Test.scx")));
        Assert.True(File.Exists(Path.Combine(aiRoot, "maps", "Sparring", "(2)Test.scx")));
        Assert.True(File.Exists(Path.Combine(aiRoot, prepared.Ai.BotExecutable)));
        Assert.Equal(prepared.Ai.BotExecutable, prepared.Ai.AiModule);
        Assert.Equal(Path.Combine("maps", "Sparring", "(2)Test.scx"), prepared.Player.MapFileName);
        Assert.True(Directory.Exists(Path.Combine(playerRoot, "bwapi-data", "write")));
        Assert.True(Directory.Exists(Path.Combine(playerRoot, "bwapi-data", "logs")));
        Assert.True(Directory.Exists(Path.Combine(playerRoot, "Errors")));
        Assert.True(Directory.Exists(Path.Combine(aiRoot, "bwapi-data", "write")));
        Assert.True(Directory.Exists(Path.Combine(aiRoot, "bwapi-data", "logs")));
        Assert.True(Directory.Exists(Path.Combine(aiRoot, "Errors")));
    }

    [Fact]
    public void ProvisionMapAddsStableSuffixForUserMapRuntimeCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-provision-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "Custom.scx");
        File.WriteAllText(source, "map");
        var map = new PracticeMap(
            Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
            "Custom",
            "Custom.scx",
            ImagePath: null,
            Enabled: true,
            SourcePath: source,
            IsUserMap: true);

        var provisioned = RuntimeProvisioner.ProvisionMap(map, root);

        Assert.Equal(Path.Combine("maps", "Sparring", "Custom_00112233.scx"), provisioned.RelativeMapPath);
        Assert.True(File.Exists(provisioned.FullMapPath));
    }

    [Fact]
    public void ProvisionBotMirrorsConfigSidecarsToLegacyBwapiAiRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-provision-tests", Guid.NewGuid().ToString("N"));
        var aiRoot = Path.Combine(root, "ai");
        var botSource = Path.Combine(root, "schnail", "bots", "Sapphire");
        Directory.CreateDirectory(botSource);
        File.WriteAllText(Path.Combine(botSource, "Gems.dll"), "bot");
        File.WriteAllText(Path.Combine(botSource, "Gems_config.json"), "{}");
        File.WriteAllText(Path.Combine(botSource, "Sapphire.zip"), "archive");
        var bot = new PracticeBot(
            Guid.NewGuid(),
            "Sapphire",
            StarCraftRace.Terran,
            "Gems.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            457,
            false,
            new HashSet<Guid> { Guid.NewGuid() },
            null,
            null,
            botSource);

        RuntimeProvisioner.ProvisionBot(bot, aiRoot);

        Assert.True(File.Exists(Path.Combine(aiRoot, "bwapi-data", "AI", "Sparring", "Bots", "Sapphire", "Gems_config.json")));
        Assert.True(File.Exists(Path.Combine(aiRoot, "bwapi-data", "AI", "Gems_config.json")));
        Assert.False(File.Exists(Path.Combine(aiRoot, "bwapi-data", "AI", "Sapphire.zip")));
    }

    [Fact]
    public void PrepareRuntimeAssetsWritesSelectedBuildConfigForSelectableBot()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-provision-tests", Guid.NewGuid().ToString("N"));
        var playerRoot = Path.Combine(root, "player");
        var aiRoot = Path.Combine(root, "ai");
        var mapSource = Path.Combine(root, "maps", "(2)Test.scx");
        var botSource = Path.Combine(root, "bots", "NeoProtossF");
        Directory.CreateDirectory(Path.GetDirectoryName(mapSource)!);
        Directory.CreateDirectory(botSource);
        File.WriteAllText(mapSource, "map");
        File.WriteAllText(Path.Combine(botSource, "NeoProtossF.dll"), "bot");

        var mapId = Guid.NewGuid();
        var bot = new PracticeBot(
            Guid.NewGuid(),
            "NeoProtossF",
            StarCraftRace.Protoss,
            "NeoProtossF.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            950,
            false,
            new HashSet<Guid> { mapId },
            null,
            null,
            botSource,
            [
                new PracticeBotBuildOption("1012", "1012", "All", "질럿 러시"),
                new PracticeBotBuildOption("23_nexus", "23넥", "vs Terran", "앞마당 운영")
            ]);
        var map = new PracticeMap(mapId, "(2)Test", "(2)Test.scx", null, true, mapSource);
        var plan = new PracticeLaunchPlan(
            Client(playerRoot, ClientRuntimeRole.PlayerHost, string.Empty, "placeholder.scx"),
            Client(aiRoot, ClientRuntimeRole.AiOpponent, "NeoProtossF.dll", string.Empty) with
            {
                BotBuildId = "23_nexus"
            },
            bot,
            map);

        RuntimeProvisioner.PrepareRuntimeAssets(plan);

        var botConfig = Path.Combine(aiRoot, "bwapi-data", "AI", "Sparring", "Bots", "NeoProtossF", "sparring-bot.ini");
        var legacyConfig = Path.Combine(aiRoot, "bwapi-data", "AI", "sparring-bot.ini");
        Assert.Equal("build=23_nexus" + Environment.NewLine, File.ReadAllText(botConfig));
        Assert.Equal("build=23_nexus" + Environment.NewLine, File.ReadAllText(legacyConfig));
    }

    [Fact]
    public void PrepareRuntimeAssetsRefreshesLegacyBuildConfigWhenSwitchingSelectableBots()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-provision-tests", Guid.NewGuid().ToString("N"));
        var playerRoot = Path.Combine(root, "player");
        var aiRoot = Path.Combine(root, "ai");
        var mapSource = Path.Combine(root, "maps", "(2)Test.scx");
        var protossSource = Path.Combine(root, "bots", "NeoProtossF");
        var terranSource = Path.Combine(root, "bots", "NeoTerranF");
        Directory.CreateDirectory(Path.GetDirectoryName(mapSource)!);
        Directory.CreateDirectory(protossSource);
        Directory.CreateDirectory(terranSource);
        File.WriteAllText(mapSource, "map");
        File.WriteAllText(Path.Combine(protossSource, "NeoProtossF.dll"), "bot");
        File.WriteAllText(Path.Combine(terranSource, "NeoTerranF.dll"), "bot");

        var mapId = Guid.NewGuid();
        var map = new PracticeMap(mapId, "(2)Test", "(2)Test.scx", null, true, mapSource);
        var protoss = new PracticeBot(
            Guid.NewGuid(),
            "NeoProtossF",
            StarCraftRace.Protoss,
            "NeoProtossF.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            950,
            false,
            new HashSet<Guid> { mapId },
            null,
            null,
            protossSource,
            [new PracticeBotBuildOption("23_nexus", "23넥", "vs Terran", "앞마당 운영")]);
        var terran = new PracticeBot(
            Guid.NewGuid(),
            "NeoTerranF",
            StarCraftRace.Terran,
            "NeoTerranF.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            950,
            false,
            new HashSet<Guid> { mapId },
            null,
            null,
            terranSource,
            [new PracticeBotBuildOption("factory_expand", "팩더블", "vs Protoss", "팩토리 이후 앞마당")]);

        RuntimeProvisioner.PrepareRuntimeAssets(new PracticeLaunchPlan(
            Client(playerRoot, ClientRuntimeRole.PlayerHost, string.Empty, "placeholder.scx"),
            Client(aiRoot, ClientRuntimeRole.AiOpponent, "NeoProtossF.dll", string.Empty) with
            {
                BotBuildId = "23_nexus"
            },
            protoss,
            map));
        RuntimeProvisioner.PrepareRuntimeAssets(new PracticeLaunchPlan(
            Client(playerRoot, ClientRuntimeRole.PlayerHost, string.Empty, "placeholder.scx"),
            Client(aiRoot, ClientRuntimeRole.AiOpponent, "NeoTerranF.dll", string.Empty) with
            {
                BotBuildId = "factory_expand"
            },
            terran,
            map));

        var legacyConfig = Path.Combine(aiRoot, "bwapi-data", "AI", "sparring-bot.ini");
        Assert.Equal("build=factory_expand" + Environment.NewLine, File.ReadAllText(legacyConfig));
    }

    private static ClientLaunchSettings Client(string root, ClientRuntimeRole role, string aiModule, string map)
    {
        return new ClientLaunchSettings(
            role,
            root,
            "Name",
            StarCraftRace.Terran,
            StarCraftRace.Zerg,
            map,
            "Game",
            aiModule,
            aiModule,
            BotExecutableKind.Dll,
            SoundEnabled: role == ClientRuntimeRole.PlayerHost,
            WindowedMode: true,
            Borderless: false,
            ClipCursor: false,
            ApmAlertEnabled: false,
            EnableWModePlugin: false,
            CncDdrawMode: CncDdrawMode.Disabled);
    }
}
