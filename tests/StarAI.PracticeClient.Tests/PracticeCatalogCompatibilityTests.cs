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

    private static PracticeBot Bot(Guid id, params Guid[] supportedMaps)
    {
        return new PracticeBot(
            id,
            "TestBot",
            StarCraftRace.Zerg,
            "TestBot.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            supportedMaps.ToHashSet(),
            null,
            null);
    }
}
