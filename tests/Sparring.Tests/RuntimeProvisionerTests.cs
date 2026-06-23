using Sparring.Core;

namespace Sparring.Tests;

public sealed class RuntimeProvisionerTests
{
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
