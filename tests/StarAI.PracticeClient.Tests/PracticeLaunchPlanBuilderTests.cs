using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class PracticeLaunchPlanBuilderTests
{
    [Fact]
    public void BuildKeepsPlayerAiModuleEmptyAndMutesAiClient()
    {
        var botId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var catalog = Catalog(botId, mapId, mapId);
        var selection = new PracticeSelection(
            botId,
            mapId,
            StarCraftRace.Terran,
            "StarAI Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false);

        var plan = PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection);

        Assert.Equal(string.Empty, plan.Player.AiModule);
        Assert.Equal("BananaBrain.dll", plan.Ai.AiModule);
        Assert.True(plan.Player.SoundEnabled);
        Assert.False(plan.Ai.SoundEnabled);
        Assert.False(plan.Player.WindowedMode);
        Assert.False(plan.Ai.WindowedMode);
        Assert.True(plan.Player.Borderless);
        Assert.False(plan.Ai.Borderless);
        Assert.False(plan.Ai.ApmAlertEnabled);
        Assert.False(plan.Player.EnableWModePlugin);
        Assert.False(plan.Ai.EnableWModePlugin);
        Assert.Equal(CncDdrawMode.BorderlessFullscreen, plan.Player.CncDdrawMode);
        Assert.Equal(CncDdrawMode.Windowed, plan.Ai.CncDdrawMode);
        Assert.Equal("JOIN_FIRST", plan.Ai.GameName);
    }

    [Fact]
    public void BuildHidesAiCharacterNameByDefault()
    {
        var botId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var catalog = Catalog(botId, mapId, mapId);
        var selection = new PracticeSelection(
            botId,
            mapId,
            StarCraftRace.Terran,
            "StarAI Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false);

        var plan = PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection);

        Assert.Equal("StarAIBot", plan.Ai.CharacterName);
    }

    [Fact]
    public void BuildCanRevealSelectedBotNameAsAiCharacterName()
    {
        var botId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var catalog = Catalog(botId, mapId, mapId);
        var selection = new PracticeSelection(
            botId,
            mapId,
            StarCraftRace.Terran,
            "StarAI Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false,
            HideAiName: false);

        var plan = PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection);

        Assert.Equal("BananaBrain", plan.Ai.CharacterName);
    }

    [Fact]
    public void BuildRejectsUnsupportedBotMapCombination()
    {
        var botId = Guid.NewGuid();
        var unsupportedMapId = Guid.NewGuid();
        var catalog = Catalog(botId, Guid.NewGuid(), unsupportedMapId);
        var selection = new PracticeSelection(
            botId,
            unsupportedMapId,
            StarCraftRace.Protoss,
            "StarAI Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false);

        var exception = Assert.Throws<InvalidOperationException>(
            () => PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection));

        Assert.Contains("does not support map", exception.Message);
    }

    private static PracticeCatalog Catalog(Guid botId, Guid supportedMapId, Guid actualMapId)
    {
        return new PracticeCatalog(
            [
                new PracticeBot(
                    botId,
                    "BananaBrain",
                    StarCraftRace.Zerg,
                    "BananaBrain.dll",
                    BotExecutableKind.Dll,
                    "4.4.0",
                    961,
                    false,
                    new HashSet<Guid> { supportedMapId },
                    null,
                    null)
            ],
            [
                new PracticeMap(actualMapId, "Fighting Spirit", "Fighting.scx", null, true)
            ]);
    }

    private static PracticePaths SafePaths()
    {
        return new PracticePaths(
            @"C:\starai\StarAI.PracticeClient",
            @"C:\starai\Start-StarAI-PracticeClient.cmd",
            @"C:\starai\SC116AI",
            @"C:\starai\SC116AI_ai",
            @"C:\Program Files (x86)\SCHNAIL Client");
    }
}
