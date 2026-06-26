using Sparring.Core;

namespace Sparring.Tests;

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
            "Sparring Practice",
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
            "Sparring Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false);

        var plan = PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection);

        Assert.Equal("SparringBot", plan.Ai.CharacterName);
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
            "Sparring Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false,
            HideAiName: false);

        var plan = PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection);

        Assert.Equal("BananaBrain", plan.Ai.CharacterName);
    }

    [Fact]
    public void BuildPassesSelectedBotBuildToAiClient()
    {
        var botId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var catalog = Catalog(
            botId,
            mapId,
            mapId,
            [
                new PracticeBotBuildOption("fast_power_dragoon", "빠른 파워드라군", "All", "초반 드라군 압박")
            ]);
        var selection = new PracticeSelection(
            botId,
            mapId,
            StarCraftRace.Terran,
            "Sparring Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false,
            BotBuildId: "fast_power_dragoon");

        var plan = PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection);

        Assert.Equal("fast_power_dragoon", plan.Ai.BotBuildId);
        Assert.Null(plan.Player.BotBuildId);
    }

    [Fact]
    public void BuildRejectsUnknownBotBuild()
    {
        var botId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var catalog = Catalog(
            botId,
            mapId,
            mapId,
            [
                new PracticeBotBuildOption("1012", "1012", "All", "질럿 러시")
            ]);
        var selection = new PracticeSelection(
            botId,
            mapId,
            StarCraftRace.Terran,
            "Sparring Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false,
            BotBuildId: "not_a_build");

        var exception = Assert.Throws<InvalidOperationException>(
            () => PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection));

        Assert.Contains("does not support build", exception.Message);
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
            "Sparring Practice",
            PlayerBorderless: true,
            ClipCursor: false,
            AllowApmAlert: false);

        var exception = Assert.Throws<InvalidOperationException>(
            () => PracticeLaunchPlanBuilder.Build(catalog, SafePaths(), selection));

        Assert.Contains("does not support map", exception.Message);
    }

    private static PracticeCatalog Catalog(
        Guid botId,
        Guid supportedMapId,
        Guid actualMapId,
        IReadOnlyList<PracticeBotBuildOption>? buildOptions = null)
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
                    null,
                    BuildOptions: buildOptions)
            ],
            [
                new PracticeMap(actualMapId, "Fighting Spirit", "Fighting.scx", null, true)
            ]);
    }

    private static PracticePaths SafePaths()
    {
        return new PracticePaths(
            @"C:\sparring\Sparring",
            @"C:\sparring\SC116AI",
            @"C:\sparring\SC116AI_ai",
            @"C:\Program Files (x86)\SCHNAIL Client");
    }
}
