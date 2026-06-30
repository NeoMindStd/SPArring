using Sparring.Core;

namespace Sparring.Tests;

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

    [Fact]
    public void CompatibilityAliasesCanUseOriginalMapSupportListForUpdatedMapVariant()
    {
        var originalMapId = Guid.NewGuid();
        var remasteredMapId = Guid.NewGuid();
        var updatedMapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(botId, "Dragon", originalMapId)
            ],
            [
                new PracticeMap(
                    updatedMapId,
                    "(4)Polypoid 1.75",
                    "(4)Polypoid_1.75.scx",
                    null,
                    true,
                    CompatibilityMapIds: new HashSet<Guid> { remasteredMapId, originalMapId })
            ]);

        Assert.True(PracticeCatalogCompatibility.IsCompatible(catalog, botId, updatedMapId));
        Assert.Equal(updatedMapId, Assert.Single(PracticeCatalogCompatibility.MapsForBot(catalog, botId)).Id);
        Assert.Equal(botId, Assert.Single(PracticeCatalogCompatibility.BotsForMap(catalog, updatedMapId)).Id);
    }

    [Theory]
    [InlineData("ICELab", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("ICELab", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("CUBOT", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("CUBOT", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Feint", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Feint", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Crazyhammer", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Crazyhammer", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Randomhammer", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Randomhammer", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Steamhammer", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Steamhammer", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("KillAlll", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("KillAlll", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Iron bot", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Iron bot", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("XIAOYICOG2019", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("XIAOYICOG2019", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Zia bot", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Zia bot", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("LetaBot", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("LetaBot", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Chris Coxe", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Chris Coxe", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Pineapple Cactus", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Pineapple Cactus", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Sijia Xu", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Sijia Xu", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Crona", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Crona", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("BananaBrain", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("BananaBrain", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Locutus", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Locutus", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("ZNZZBot", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("ZNZZBot", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("DaQin", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("DaQin", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Stone", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("Stone", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("Stone", "(4)Jade", "(4)Jade.scx")]
    [InlineData("Stone", "(2)Benzene", "(2)Benzene.scx")]
    [InlineData("RedRum", "(4)Fighting Spirit", "(4)Fighting Spirit.scx")]
    [InlineData("RedRum", "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx")]
    [InlineData("RedRum", "(4)Jade", "(4)Jade.scx")]
    [InlineData("Yuanheng Zhu", "(4)Andromeda", "(4)Andromeda.scx")]
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

    [Theory]
    [InlineData("Crazyhammer", "(4)Empire of the Sun", "(4)Empire of the Sun.scm")]
    [InlineData("McRaveZ", "(4)La Mancha1.1", "(4)La Mancha1.1.scx")]
    [InlineData("PurpleWave", "(4)Polypoid 1.65", "(4)Polypoid1.65_BW1.16.1.scx")]
    public void ShortUnresolvedHistoryPairsWithoutNormalResultsAreNotCompatible(
        string botName,
        string mapName,
        string fileName)
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
    }

    [Fact]
    public void StoneIsExcludedFromEveryDeclaredMapUntilRuntimeSafetyIsProven()
    {
        var benzeneId = Guid.NewGuid();
        var pythonId = Guid.NewGuid();
        var circuitBreakerId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(botId, "Stone", benzeneId, pythonId, circuitBreakerId)
            ],
            [
                new PracticeMap(benzeneId, "(2)Benzene", "(2)Benzene.scx", null, true),
                new PracticeMap(pythonId, "(4)Python", "(4)Python.scx", null, true),
                new PracticeMap(circuitBreakerId, "(4)Circuit Breaker", "(4)CircuitBreaker.scx", null, true)
            ]);

        Assert.Empty(PracticeCatalogCompatibility.MapsForBot(catalog, botId));
        Assert.All(catalog.Maps, map =>
            Assert.DoesNotContain(PracticeCatalogCompatibility.BotsForMap(catalog, map.Id), bot => bot.Id == botId));
    }

    [Fact]
    public void RedRumIsExcludedUntilAValidatedSupportedMapWhitelistExists()
    {
        var fightingSpiritId = Guid.NewGuid();
        var fightingSpirit14Id = Guid.NewGuid();
        var benzeneId = Guid.NewGuid();
        var pythonId = Guid.NewGuid();
        var jadeId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(botId, "RedRum", fightingSpiritId, fightingSpirit14Id, benzeneId, pythonId, jadeId)
            ],
            [
                new PracticeMap(fightingSpiritId, "(4)Fighting Spirit", "(4)Fighting Spirit.scx", null, true),
                new PracticeMap(fightingSpirit14Id, "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx", null, true),
                new PracticeMap(benzeneId, "(2)Benzene", "(2)Benzene.scx", null, true),
                new PracticeMap(pythonId, "(4)Python", "(4)Python.scx", null, true),
                new PracticeMap(jadeId, "(4)Jade", "(4)Jade.scx", null, true)
            ]);

        Assert.Empty(PracticeCatalogCompatibility.MapsForBot(catalog, botId));
        Assert.All(catalog.Maps, map =>
            Assert.DoesNotContain(PracticeCatalogCompatibility.BotsForMap(catalog, map.Id), bot => bot.Id == botId));
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

    [Fact]
    public void SapphireCanStillUseFightingSpiritAfterConfigSidecarProvisioningFix()
    {
        var mapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(botId, "Sapphire", mapId)
            ],
            [
                new PracticeMap(mapId, "(4)Fighting Spirit 1.4 [Remastered Ladder]", "(4)Fighting_Spirit 1.4.scx", null, true)
            ]);

        Assert.True(PracticeCatalogCompatibility.IsCompatible(catalog, botId, mapId));
    }

    [Fact]
    public void BundledCatalogAppliesReportedFightingSpiritFeedback()
    {
        var repo = FindRepositoryRoot();
        var catalog = PracticeAssetCatalogReader.Read(Path.Combine(repo, "data"));
        var fightingSpiritMaps = catalog.Maps
            .Where(map => IsFightingSpiritVariant(map))
            .ToArray();

        Assert.Contains(fightingSpiritMaps, map => map.Name == "(4)Fighting Spirit");
        Assert.Contains(fightingSpiritMaps, map => map.Name == "(4)Fighting Spirit 1.4");

        foreach (var botName in ReportedFightingSpiritBadBots)
        {
            var bot = Assert.Single(catalog.Bots, bot => bot.Name == botName);
            foreach (var map in fightingSpiritMaps)
            {
                Assert.False(PracticeCatalogCompatibility.IsCompatible(catalog, bot.Id, map.Id), $"{botName} + {map.Name}");
                Assert.DoesNotContain(PracticeCatalogCompatibility.MapsForBot(catalog, bot.Id), candidate => candidate.Id == map.Id);
                Assert.DoesNotContain(PracticeCatalogCompatibility.BotsForMap(catalog, map.Id), candidate => candidate.Id == bot.Id);
            }
        }

        var fightingSpirit = Assert.Single(catalog.Maps, map => map.Name == "(4)Fighting Spirit");
        foreach (var botName in new[] { "Stardust", "skyFORKnet" })
        {
            var bot = Assert.Single(catalog.Bots, bot => bot.Name == botName);
            Assert.True(PracticeCatalogCompatibility.IsCompatible(catalog, bot.Id, fightingSpirit.Id), botName);
        }
    }

    [Theory]
    [InlineData("NeoProtossF")]
    [InlineData("NeoTerranF")]
    [InlineData("NeoZergF")]
    public void NeoPracticeBotsRemainSelectableOnFightingSpirit(string botName)
    {
        var mapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot(botId, botName, mapId)
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
            SteamhammerFamilyBotNames.Contains(name, StringComparer.OrdinalIgnoreCase)
                ? "Steamhammer.dll"
                : $"{name}.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            supportedMaps.ToHashSet(),
            null,
            null);
    }

    private static readonly string[] SteamhammerFamilyBotNames =
    [
        "Crazyhammer",
        "Feint",
        "Randomhammer",
        "Steamhammer"
    ];

    private static readonly string[] ReportedFightingSpiritBadBots =
    [
        "Chris Coxe",
        "Pineapple Cactus",
        "Sijia Xu",
        "Crona",
        "BananaBrain",
        "Locutus",
        "ZNZZBot",
        "DaQin"
    ];

    private static bool IsFightingSpiritVariant(PracticeMap map)
    {
        return map.Name.Contains("Fighting Spirit", StringComparison.OrdinalIgnoreCase) ||
            map.FileName.Replace("_", " ", StringComparison.Ordinal).Contains("Fighting Spirit", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sparring.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "data")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Sparring repository root.");
    }
}
