using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class SchnailCatalogReaderTests
{
    [Fact]
    public void PracticeAssetCatalogReaderReadsBundledStarAiAssetRoot()
    {
        var mapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        using var fixture = CatalogFixture.Create(
            $$"""
            [
              {
                "name": "Dragon",
                "guid": "{{botId}}",
                "enabled": true,
                "race": "Terran",
                "botExecutable": "dragon.dll",
                "botType": "DLL",
                "bwapiVersion": "4.4.0",
                "enabledByUser": true,
                "practiceOnly": false,
                "elo": "1081",
                "mapGuids": ["{{mapId}}"]
              }
            ]
            """,
            $$"""
            [
              {
                "name": "(4)Fighting Spirit",
                "fileName": "(4)Fighting Spirit.scx",
                "guid": "{{mapId}}",
                "enabled": true
              }
            ]
            """,
            createSchnailLayout: true);
        Directory.CreateDirectory(Path.Combine(fixture.Root, "bots", "Dragon"));
        File.WriteAllText(Path.Combine(fixture.Root, "bots", "Dragon", "dragon.dll"), "bot");
        File.WriteAllText(Path.Combine(fixture.Root, "maps", "(4)Fighting Spirit.scx"), "map");

        var catalog = PracticeAssetCatalogReader.Read(fixture.Root);

        var bot = Assert.Single(catalog.Bots);
        var map = Assert.Single(catalog.Maps);
        Assert.Equal("Dragon", bot.Name);
        Assert.Equal(Path.Combine(fixture.Root, "bots", "Dragon"), bot.SourceDirectory);
        Assert.Equal(Path.Combine(fixture.Root, "maps", "(4)Fighting Spirit.scx"), map.SourcePath);
    }

    [Fact]
    public void ReadFilesParsesEnabledBotsAndMaps()
    {
        var mapId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        using var fixture = CatalogFixture.Create(
            $$"""
            [
              {
                "name": "BananaBrain",
                "guid": "{{botId}}",
                "enabled": true,
                "race": "Zerg",
                "description": "Macro Zerg",
                "author": "Johan",
                "botExecutable": "BananaBrain.dll",
                "botType": "DLL",
                "bwapiVersion": "4.4.0",
                "enabledByUser": true,
                "practiceOnly": false,
                "elo": "961",
                "mapGuids": ["{{mapId}}"]
              },
              {
                "name": "Disabled",
                "guid": "{{Guid.NewGuid()}}",
                "enabled": false,
                "enabledByUser": true
              }
            ]
            """,
            $$"""
            [
              {
                "name": "(4) Fighting Spirit",
                "fileName": "(4)Fighting Spirit.scx",
                "imagePath": "maps\\(4)Fighting Spirit.scx.jpg",
                "guid": "{{mapId}}",
                "enabled": true
              },
              {
                "name": "Disabled Map",
                "fileName": "Disabled.scx",
                "guid": "{{Guid.NewGuid()}}",
                "enabled": false
              }
            ]
            """);

        var catalog = SchnailCatalogReader.ReadFiles(fixture.BotsDatPath, fixture.MapsDatPath);

        var bot = Assert.Single(catalog.Bots);
        Assert.Equal(botId, bot.Id);
        Assert.Equal(StarCraftRace.Zerg, bot.Race);
        Assert.Equal(BotExecutableKind.Dll, bot.ExecutableKind);
        Assert.Equal(961, bot.Elo);
        Assert.Contains(mapId, bot.SupportedMapIds);

        var map = Assert.Single(catalog.Maps);
        Assert.Equal(mapId, map.Id);
        Assert.Equal("(4)Fighting Spirit.scx", map.FileName);
    }

    [Fact]
    public void ReadFilesResolvesSchnailMapAliasToExistingMapFile()
    {
        var mapId = Guid.NewGuid();
        using var fixture = CatalogFixture.Create(
            "[]",
            $$"""
            [
              {
                "name": "(4)Fighting Spirit",
                "fileName": "(4)Fighting Spirit.scx",
                "imagePath": "maps\\(4)Fighting Spirit.scx.jpg",
                "guid": "{{mapId}}",
                "enabled": true
              }
            ]
            """,
            createSchnailLayout: true);
        var actualMap = Path.Combine(fixture.Root, "maps", "Fighting_Spirit_1.4.scx");
        File.WriteAllText(actualMap, "map");

        var catalog = SchnailCatalogReader.ReadFiles(fixture.BotsDatPath, fixture.MapsDatPath, fixture.Root);

        var map = Assert.Single(catalog.Maps);
        Assert.Equal(actualMap, map.SourcePath);
    }

    [Fact]
    public void ReadFilesExcludesSchnailMapWhenSourceFileCannotBeResolved()
    {
        var mapId = Guid.NewGuid();
        using var fixture = CatalogFixture.Create(
            "[]",
            $$"""
            [
              {
                "name": "(4)Old Missing Map",
                "fileName": "(4)Old Missing Map.scx",
                "guid": "{{mapId}}",
                "enabled": true
              }
            ]
            """,
            createSchnailLayout: true);

        var catalog = SchnailCatalogReader.ReadFiles(fixture.BotsDatPath, fixture.MapsDatPath, fixture.Root);

        Assert.Empty(catalog.Maps);
    }

    private sealed class CatalogFixture : IDisposable
    {
        private readonly string _root;

        private CatalogFixture(string root, bool createSchnailLayout)
        {
            _root = root;
            if (createSchnailLayout)
            {
                Directory.CreateDirectory(Path.Combine(root, "bots"));
                Directory.CreateDirectory(Path.Combine(root, "maps"));
                BotsDatPath = Path.Combine(root, "bots", "bots.dat");
                MapsDatPath = Path.Combine(root, "maps", "maps.dat");
            }
            else
            {
                BotsDatPath = Path.Combine(root, "bots.dat");
                MapsDatPath = Path.Combine(root, "maps.dat");
            }
        }

        public string Root => _root;

        public string BotsDatPath { get; }

        public string MapsDatPath { get; }

        public static CatalogFixture Create(string botsJson, string mapsJson, bool createSchnailLayout = false)
        {
            var root = Path.Combine(Path.GetTempPath(), "starai-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var fixture = new CatalogFixture(root, createSchnailLayout);

            File.WriteAllText(fixture.BotsDatPath, botsJson);
            File.WriteAllText(fixture.MapsDatPath, mapsJson);
            return fixture;
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
