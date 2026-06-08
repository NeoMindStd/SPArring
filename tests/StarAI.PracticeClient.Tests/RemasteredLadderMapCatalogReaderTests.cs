using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class RemasteredLadderMapCatalogReaderTests
{
    [Fact]
    public void ReadDirectoryKeepsOnlyMapsThatMatchKnownSchnailCompatibilityNames()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-ladder-maps", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "(4)Fighting_Spirit 1.4.scx"), "map");
        File.WriteAllText(Path.Combine(root, "(4)Unknown Ladder Map.scx"), "map");
        var schnailMapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [Bot(Guid.NewGuid(), schnailMapId)],
            [new PracticeMap(schnailMapId, "(4)Fighting Spirit", "(4)Fighting Spirit.scx", null, true)]);

        var maps = RemasteredLadderMapCatalogReader.ReadDirectory(root, catalog);

        var map = Assert.Single(maps);
        Assert.Equal("(4)Fighting Spirit 1.4 [Remastered Ladder]", map.Name);
        Assert.Contains(schnailMapId, map.EffectiveCompatibilityMapIds);
        Assert.False(map.IsUserMap);
    }

    [Fact]
    public void RemasteredLadderMapUsesCompatibilityIdsForBotFiltering()
    {
        var schnailMapId = Guid.NewGuid();
        var remasteredMapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [Bot(botId, schnailMapId)],
            [
                new PracticeMap(
                    remasteredMapId,
                    "Fighting Spirit [Remastered Ladder]",
                    "Fighting.scx",
                    null,
                    true,
                    CompatibilityMapIds: new HashSet<Guid> { schnailMapId })
            ]);

        Assert.True(PracticeCatalogCompatibility.IsCompatible(catalog, botId, remasteredMapId));
    }

    private static PracticeBot Bot(Guid id, params Guid[] supportedMaps)
    {
        return new PracticeBot(
            id,
            "Bot",
            StarCraftRace.Terran,
            "Bot.dll",
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            supportedMaps.ToHashSet(),
            null,
            null);
    }
}
