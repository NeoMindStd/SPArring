using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class RuntimeProvisionerTests
{
    [Fact]
    public void PrepareRuntimeAssetsCopiesMapToBothRuntimesAndBotToAiRuntime()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-provision-tests", Guid.NewGuid().ToString("N"));
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

        Assert.True(File.Exists(Path.Combine(playerRoot, "maps", "StarAI", "(2)Test.scx")));
        Assert.True(File.Exists(Path.Combine(aiRoot, "maps", "StarAI", "(2)Test.scx")));
        Assert.True(File.Exists(Path.Combine(aiRoot, prepared.Ai.BotExecutable)));
        Assert.Equal(prepared.Ai.BotExecutable, prepared.Ai.AiModule);
        Assert.Equal(Path.Combine("maps", "StarAI", "(2)Test.scx"), prepared.Player.MapFileName);
    }

    [Fact]
    public void ProvisionMapAddsStableSuffixForUserMapRuntimeCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-provision-tests", Guid.NewGuid().ToString("N"));
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

        Assert.Equal(Path.Combine("maps", "StarAI", "Custom_00112233.scx"), provisioned.RelativeMapPath);
        Assert.True(File.Exists(provisioned.FullMapPath));
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
