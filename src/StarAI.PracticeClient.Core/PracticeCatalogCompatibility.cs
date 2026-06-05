namespace StarAI.PracticeClient.Core;

public static class PracticeCatalogCompatibility
{
    public static IReadOnlyList<PracticeMap> MapsForBot(PracticeCatalog catalog, Guid botId)
    {
        var bot = catalog.FindBot(botId);

        return catalog.Maps
            .Where(map => map.Enabled && (map.IsUserMap || bot.SupportsMap(map.Id)))
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<PracticeBot> BotsForMap(PracticeCatalog catalog, Guid mapId)
    {
        var map = catalog.FindMap(mapId);
        return catalog.Bots
            .Where(bot => map.IsUserMap || bot.SupportsMap(mapId))
            .OrderBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsCompatible(PracticeCatalog catalog, Guid botId, Guid mapId)
    {
        var bot = catalog.FindBot(botId);
        var map = catalog.FindMap(mapId);

        return map.Enabled && (map.IsUserMap || bot.SupportsMap(mapId));
    }
}
