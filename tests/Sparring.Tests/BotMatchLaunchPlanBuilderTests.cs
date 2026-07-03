using Sparring.Core;

namespace Sparring.Tests;

public sealed class BotMatchLaunchPlanBuilderTests
{
    [Fact]
    public void BuildCreatesTwoBotClientsWithLeftHostAndRightJoiner()
    {
        var leftBotId = Guid.NewGuid();
        var rightBotId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var catalog = Catalog(leftBotId, rightBotId, mapId);

        var plan = BotMatchLaunchPlanBuilder.Build(
            catalog,
            SafePaths(),
            new BotMatchSelection(leftBotId, rightBotId, mapId, "Sparring Bot Match"));

        Assert.Equal(ClientRuntimeRole.AiOpponent, plan.Left.Role);
        Assert.Equal(ClientRuntimeRole.AiOpponent, plan.Right.Role);
        Assert.Equal("Sparring Bot Match", plan.Left.GameName);
        Assert.StartsWith("LeftBot-", plan.Left.CharacterName);
        Assert.StartsWith("RightBot-", plan.Right.CharacterName);
        Assert.Equal(plan.Left.CharacterName, plan.Right.GameName);
        Assert.True(plan.Left.CharacterName.Length <= 24);
        Assert.True(plan.Right.CharacterName.Length <= 24);
        Assert.Equal("Fighting.scx", plan.Left.MapFileName);
        Assert.Equal(string.Empty, plan.Right.MapFileName);
        Assert.Equal("LeftBot.dll", plan.Left.AiModule);
        Assert.Equal("RightBot.dll", plan.Right.AiModule);
        Assert.False(plan.Left.SoundEnabled);
        Assert.False(plan.Right.SoundEnabled);
        Assert.Equal(CncDdrawMode.Windowed, plan.Left.CncDdrawMode);
        Assert.Equal(CncDdrawMode.Windowed, plan.Right.CncDdrawMode);
    }

    [Fact]
    public void BuildRejectsUnsupportedBotMapPairByDefault()
    {
        var leftBotId = Guid.NewGuid();
        var rightBotId = Guid.NewGuid();
        var supportedMapId = Guid.NewGuid();
        var unsupportedMapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(leftBotId, "LeftBot", "LeftBot.dll", StarCraftRace.Protoss, supportedMapId),
                Bot(rightBotId, "RightBot", "RightBot.dll", StarCraftRace.Terran, unsupportedMapId)
            ],
            [new PracticeMap(supportedMapId, "Fighting", "Fighting.scx", null, true)]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            BotMatchLaunchPlanBuilder.Build(
                catalog,
                SafePaths(),
                new BotMatchSelection(leftBotId, rightBotId, supportedMapId, "Sparring Bot Match")));

        Assert.Contains("does not support map", exception.Message);
    }

    [Fact]
    public void BuildCanAllowIncompatiblePairsForRuntimeVerification()
    {
        var leftBotId = Guid.NewGuid();
        var rightBotId = Guid.NewGuid();
        var supportedMapId = Guid.NewGuid();
        var unsupportedMapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(leftBotId, "LeftBot", "LeftBot.dll", StarCraftRace.Protoss, unsupportedMapId),
                Bot(rightBotId, "RightBot", "RightBot.dll", StarCraftRace.Terran, unsupportedMapId)
            ],
            [new PracticeMap(supportedMapId, "Fighting", "Fighting.scx", null, true)]);

        var plan = BotMatchLaunchPlanBuilder.Build(
            catalog,
            SafePaths(),
            new BotMatchSelection(
                leftBotId,
                rightBotId,
                supportedMapId,
                "Sparring Bot Match",
                AllowIncompatible: true));

        Assert.Equal("LeftBot", plan.LeftBot.Name);
        Assert.Equal("RightBot", plan.RightBot.Name);
    }

    [Fact]
    public void BuildUsesGameNameToDisambiguateParallelBotMatchHosts()
    {
        var leftBotId = Guid.NewGuid();
        var rightBotId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var catalog = Catalog(leftBotId, rightBotId, mapId);

        var first = BotMatchLaunchPlanBuilder.Build(
            catalog,
            SafePaths(),
            new BotMatchSelection(leftBotId, rightBotId, mapId, "rating-001"));
        var second = BotMatchLaunchPlanBuilder.Build(
            catalog,
            SafePaths(),
            new BotMatchSelection(leftBotId, rightBotId, mapId, "rating-002"));

        Assert.NotEqual(first.Left.CharacterName, second.Left.CharacterName);
        Assert.Equal(first.Left.CharacterName, first.Right.GameName);
        Assert.Equal(second.Left.CharacterName, second.Right.GameName);
    }

    private static PracticeCatalog Catalog(Guid leftBotId, Guid rightBotId, Guid mapId)
    {
        return new PracticeCatalog(
            [
                Bot(leftBotId, "LeftBot", "LeftBot.dll", StarCraftRace.Protoss, mapId),
                Bot(rightBotId, "RightBot", "RightBot.dll", StarCraftRace.Terran, mapId)
            ],
            [new PracticeMap(mapId, "Fighting", "Fighting.scx", null, true)]);
    }

    private static PracticeBot Bot(
        Guid id,
        string name,
        string executable,
        StarCraftRace race,
        Guid supportedMapId)
    {
        return new PracticeBot(
            id,
            name,
            race,
            executable,
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            new HashSet<Guid> { supportedMapId },
            null,
            null);
    }

    private static PracticePaths SafePaths()
    {
        return new PracticePaths(
            @"C:\sparring\Sparring",
            @"C:\sparring\bot-match\left",
            @"C:\sparring\bot-match\right",
            @"C:\Program Files (x86)\SCHNAIL Client");
    }
}
