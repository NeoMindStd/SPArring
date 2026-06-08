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

    [Fact]
    public void CandidatesForMapExcludesBotsThatCannotUseBwapiAiModule()
    {
        var mapId = Guid.NewGuid();
        var dllId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(dllId, "DllBot", StarCraftRace.Terran, 900, BotExecutableKind.Dll, mapId),
                Bot(Guid.NewGuid(), "JarBot", StarCraftRace.Terran, 1000, BotExecutableKind.ClientJar, mapId),
                Bot(Guid.NewGuid(), "ExeBot", StarCraftRace.Terran, 1100, BotExecutableKind.ClientExe, mapId)
            ],
            [
                new PracticeMap(mapId, "Fighting Spirit", "Fighting.scx", null, true)
            ]);

        var candidates = LadderBotSelector.CandidatesForMap(catalog, mapId, StarCraftRace.Terran);

        var bot = Assert.Single(candidates);
        Assert.Equal(dllId, bot.Id);
    }

    [Fact]
    public void PickRandomThrowsWhenOnlyNonDllCandidatesExist()
    {
        var mapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(Guid.NewGuid(), "JarBot", StarCraftRace.Terran, 1000, BotExecutableKind.ClientJar, mapId)
            ],
            [
                new PracticeMap(mapId, "Fighting Spirit", "Fighting.scx", null, true)
            ]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            LadderBotSelector.PickRandom(catalog, mapId, StarCraftRace.Terran, new Random(0)));

        Assert.Contains("No compatible ladder bot", ex.Message);
    }

    [Fact]
    public void PickForRatingStronglyPrefersBotsNearPlayerRating()
    {
        var mapId = Guid.NewGuid();
        var nearId = Guid.NewGuid();
        var middleId = Guid.NewGuid();
        var lowId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(nearId, "Near", StarCraftRace.Terran, 1450, mapId),
                Bot(middleId, "Middle", StarCraftRace.Terran, 1080, mapId),
                Bot(lowId, "Low", StarCraftRace.Terran, 692, mapId)
            ],
            [
                new PracticeMap(mapId, "Fighting Spirit", "Fighting.scx", null, true)
            ]);
        var random = new Random(42);
        var counts = new Dictionary<Guid, int>
        {
            [nearId] = 0,
            [middleId] = 0,
            [lowId] = 0
        };

        for (var i = 0; i < 2000; i++)
        {
            var bot = LadderBotSelector.PickForRating(
                catalog,
                mapId,
                StarCraftRace.Terran,
                playerRating: 1453,
                random);
            counts[bot.Id]++;
        }

        Assert.True(counts[nearId] > counts[middleId], $"near={counts[nearId]}, middle={counts[middleId]}");
        Assert.True(counts[middleId] > counts[lowId], $"middle={counts[middleId]}, low={counts[lowId]}");
        Assert.True(counts[lowId] < 80, $"low={counts[lowId]}");
    }

    [Fact]
    public void PickForRatingAcrossEnabledMapsStillUsesRatingWeight()
    {
        var nearMapId = Guid.NewGuid();
        var lowMapId = Guid.NewGuid();
        var nearId = Guid.NewGuid();
        var lowId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(nearId, "Near", StarCraftRace.Terran, 1450, nearMapId),
                Bot(lowId, "Low", StarCraftRace.Terran, 692, lowMapId)
            ],
            [
                new PracticeMap(nearMapId, "Fighting Spirit", "Fighting.scx", null, true),
                new PracticeMap(lowMapId, "Python", "Python.scx", null, true)
            ]);
        var random = new Random(7);
        var nearCount = 0;
        var lowCount = 0;

        for (var i = 0; i < 1000; i++)
        {
            var bot = LadderBotSelector.PickForRatingAcrossEnabledMaps(
                catalog,
                StarCraftRace.Terran,
                playerRating: 1453,
                random);
            if (bot.Id == nearId)
            {
                nearCount++;
            }
            else if (bot.Id == lowId)
            {
                lowCount++;
            }
        }

        Assert.True(nearCount > 950, $"near={nearCount}, low={lowCount}");
    }

    [Fact]
    public void CandidatesForMapExcludesKnownBadRuntimePairsOnRemasteredCompatibilityMap()
    {
        var schnailFightingSpiritId = Guid.NewGuid();
        var remasteredFightingSpiritId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(Guid.NewGuid(), "LetaBot", StarCraftRace.Terran, 420, schnailFightingSpiritId),
                Bot(Guid.NewGuid(), "Stone", StarCraftRace.Terran, 1500, schnailFightingSpiritId),
                Bot(Guid.NewGuid(), "Dragon", StarCraftRace.Terran, 1081, schnailFightingSpiritId)
            ],
            [
                new PracticeMap(
                    remasteredFightingSpiritId,
                    "(4)Fighting Spirit 1.4 [Remastered Ladder]",
                    "(4)Fighting_Spirit 1.4.scx",
                    null,
                    true,
                    CompatibilityMapIds: new HashSet<Guid> { schnailFightingSpiritId })
            ]);

        var candidates = LadderBotSelector.CandidatesForMap(
            catalog,
            remasteredFightingSpiritId,
            StarCraftRace.Terran);

        var bot = Assert.Single(candidates);
        Assert.Equal("Dragon", bot.Name);
    }

    private static PracticeBot Bot(Guid id, string name, StarCraftRace race, int elo, params Guid[] supportedMaps)
    {
        return Bot(id, name, race, elo, BotExecutableKind.Dll, supportedMaps);
    }

    private static PracticeBot Bot(Guid id, string name, StarCraftRace race, int elo, BotExecutableKind executableKind, params Guid[] supportedMaps)
    {
        return new PracticeBot(
            id,
            name,
            race,
            executableKind == BotExecutableKind.Dll ? $"{name}.dll" : $"{name}.jar",
            executableKind,
            "4.4.0",
            elo,
            false,
            supportedMaps.ToHashSet(),
            null,
            null);
    }
}
