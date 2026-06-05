using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class UserMapCatalogReaderTests
{
    [Fact]
    public void ReadDirectoryLoadsScmAndScxWithStableIds()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-user-maps", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "Fighting Spirit.scx"), "map");
        File.WriteAllText(Path.Combine(root, "notes.txt"), "ignore");
        Directory.CreateDirectory(Path.Combine(root, "asl"));
        File.WriteAllText(Path.Combine(root, "asl", "Polypoid.scm"), "map");

        var first = UserMapCatalogReader.ReadDirectory(root);
        var second = UserMapCatalogReader.ReadDirectory(root);

        Assert.Equal(2, first.Count);
        Assert.All(first, map => Assert.True(map.IsUserMap));
        Assert.Equal(first.Select(map => map.Id), second.Select(map => map.Id));
    }

    [Fact]
    public void UserMapsRemainSelectableWhenBotHasKnownSchnailMapRestrictions()
    {
        var userMap = new PracticeMap(
            Guid.NewGuid(),
            "Custom",
            "Custom.scx",
            ImagePath: null,
            Enabled: true,
            SourcePath: @"C:\maps\Custom.scx",
            IsUserMap: true);
        var knownMapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            Bots:
            [
                new PracticeBot(
                    Guid.NewGuid(),
                    "Bot",
                    StarCraftRace.Terran,
                    "Bot.dll",
                    BotExecutableKind.Dll,
                    "4.4.0",
                    1500,
                    PracticeOnly: true,
                    SupportedMapIds: new HashSet<Guid> { knownMapId },
                    Description: null,
                    Author: null,
                    SourceDirectory: @"C:\bots\Bot")
            ],
            Maps:
            [
                new PracticeMap(knownMapId, "Known", "Known.scx", null, true, @"C:\maps\Known.scx"),
                userMap
            ]);

        var maps = PracticeCatalogCompatibility.MapsForBot(catalog, catalog.Bots[0].Id);

        Assert.Contains(maps, map => map.Id == userMap.Id);
        Assert.True(PracticeCatalogCompatibility.IsCompatible(catalog, catalog.Bots[0].Id, userMap.Id));
    }
}
