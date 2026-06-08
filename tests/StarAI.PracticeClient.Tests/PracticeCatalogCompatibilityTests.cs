using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class PracticeCatalogCompatibilityTests
{
    [Fact]
    public void MapsForBotReturnsOnlySupportedEnabledMaps()
    {
        var supportedMapId = Guid.NewGuid();
        var unsupportedMapId = Guid.NewGuid();
        var disabledMapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(botId, supportedMapId, disabledMapId)
            ],
            [
                new PracticeMap(supportedMapId, "Fighting Spirit", "Fighting.scx", null, true),
                new PracticeMap(unsupportedMapId, "Python", "Python.scx", null, true),
                new PracticeMap(disabledMapId, "Disabled", "Disabled.scx", null, false)
            ]);

        var maps = PracticeCatalogCompatibility.MapsForBot(catalog, botId);

        var map = Assert.Single(maps);
        Assert.Equal(supportedMapId, map.Id);
    }

    [Fact]
    public void BotsForMapReturnsOnlyCompatibleBots()
    {
        var mapId = Guid.NewGuid();
        var compatibleBotId = Guid.NewGuid();
        var incompatibleBotId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(compatibleBotId, mapId),
                Bot(incompatibleBotId, Guid.NewGuid())
            ],
            [
                new PracticeMap(mapId, "Fighting Spirit", "Fighting.scx", null, true)
            ]);

        var bots = PracticeCatalogCompatibility.BotsForMap(catalog, mapId);

        var bot = Assert.Single(bots);
        Assert.Equal(compatibleBotId, bot.Id);
    }

    [Theory]
    [InlineData("ICELab", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("ICELab", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Feint", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Feint", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("LetaBot", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("LetaBot", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Stone", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Stone", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Stone", "(4)Jade", "(4)Jade.scx")]
    public void KnownBadRuntimePairsAreNotCompatible(string botName, string mapName, string fileName)
    {
        var schnailMapId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(botId, botName, schnailMapId)
            ],
            [
                new PracticeMap(
                    mapId,
                    mapName,
                    fileName,
                    null,
                    true,
                    CompatibilityMapIds: new HashSet<Guid> { schnailMapId })
            ]);

        Assert.False(PracticeCatalogCompatibility.IsCompatible(catalog, botId, mapId));
        Assert.Empty(PracticeCatalogCompatibility.MapsForBot(catalog, botId));
        Assert.Empty(PracticeCatalogCompatibility.BotsForMap(catalog, mapId));
    }

    [Fact]
    public void OtherBotsCanStillUseFightingSpiritWhenDeclaredCompatible()
    {
        var mapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(botId, "Dragon", mapId)
            ],
            [
                new PracticeMap(mapId, "(4)Fighting Spirit", "(4)Fighting Spirit.scx", null, true)
            ]);

        Assert.True(PracticeCatalogCompatibility.IsCompatible(catalog, botId, mapId));
    }

    private static PracticeBot Bot(Guid id, params Guid[] supportedMaps)
    {
        return Bot(id, "TestBot", supportedMaps);
    }

    private static PracticeBot Bot(Guid id, string name, params Guid[] supportedMaps)
    {
        return new PracticeBot(
            id,
            name,
            StarCraftRace.Zerg,
            $"{name}.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            supportedMaps.ToHashSet(),
            null,
            null);
    }
}
