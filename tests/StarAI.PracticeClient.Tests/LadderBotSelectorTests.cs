using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class LadderBotSelectorTests
{
    [Fact]
    public void CandidatesForMapFiltersByCompatibilityAndRace()
    {
        var mapId = Guid.NewGuid();
        var otherMapId = Guid.NewGuid();
        var terranId = Guid.NewGuid();
        var zergId = Guid.NewGuid();
        var incompatibleId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(terranId, "TerranBot", StarCraftRace.Terran, 900, mapId),
                Bot(zergId, "ZergBot", StarCraftRace.Zerg, 800, mapId),
                Bot(incompatibleId, "OtherMapBot", StarCraftRace.Terran, 700, otherMapId)
            ],
            [
                new PracticeMap(mapId, "Fighting Spirit", "Fighting.scx", null, true),
                new PracticeMap(otherMapId, "Python", "Python.scx", null, true)
            ]);

        var candidates = LadderBotSelector.CandidatesForMap(catalog, mapId, StarCraftRace.Terran);

        var bot = Assert.Single(candidates);
        Assert.Equal(terranId, bot.Id);
    }

    [Fact]
    public void PickRandomThrowsWhenNoCandidateExists()
    {
        var mapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(Guid.NewGuid(), "ZergBot", StarCraftRace.Zerg, 800, mapId)
            ],
            [
                new PracticeMap(mapId, "Fighting Spirit", "Fighting.scx", null, true)
            ]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            LadderBotSelector.PickRandom(catalog, mapId, StarCraftRace.Terran, new Random(0)));

        Assert.Contains("No compatible ladder bot", ex.Message);
    }

    private static PracticeBot Bot(Guid id, string name, StarCraftRace race, int elo, params Guid[] supportedMaps)
    {
        return new PracticeBot(
            id,
            name,
            race,
            $"{name}.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            elo,
            false,
            supportedMaps.ToHashSet(),
            null,
            null);
    }
}
