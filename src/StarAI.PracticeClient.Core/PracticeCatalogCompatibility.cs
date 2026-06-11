namespace StarAI.PracticeClient.Core;

public static class PracticeCatalogCompatibility
{
    public static IReadOnlyList<PracticeMap> MapsForBot(PracticeCatalog catalog, Guid botId)
    {
        var bot = catalog.FindBot(botId);

        return catalog.Maps
            .Where(map => map.Enabled && (map.IsUserMap || IsSupportedRuntimePair(bot, map)))
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<PracticeBot> BotsForMap(PracticeCatalog catalog, Guid mapId)
    {
        var map = catalog.FindMap(mapId);
        return catalog.Bots
            .Where(bot => map.IsUserMap || IsSupportedRuntimePair(bot, map))
            .OrderBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsCompatible(PracticeCatalog catalog, Guid botId, Guid mapId)
    {
        var bot = catalog.FindBot(botId);
        var map = catalog.FindMap(mapId);

        return map.Enabled && (map.IsUserMap || IsSupportedRuntimePair(bot, map));
    }

    private static bool IsSupportedRuntimePair(PracticeBot bot, PracticeMap map)
    {
        return SupportsMap(bot, map) && !IsKnownBadRuntimePair(bot, map);
    }

    private static bool SupportsMap(PracticeBot bot, PracticeMap map)
    {
        return map.EffectiveCompatibilityMapIds.Any(bot.SupportsMap);
    }

    private static bool IsKnownBadRuntimePair(PracticeBot bot, PracticeMap map)
    {
        if (bot.Name.Equals("Stone", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsFightingSpiritVariant(map))
        {
            return bot.Name.Equals("ICELab", StringComparison.OrdinalIgnoreCase) ||
                   bot.Name.Equals("CUBOT", StringComparison.OrdinalIgnoreCase) ||
                   IsSteamhammerFamily(bot) ||
                   bot.Name.Equals("Feint", StringComparison.OrdinalIgnoreCase) ||
                   bot.Name.Equals("LetaBot", StringComparison.OrdinalIgnoreCase) ||
                   bot.Name.Equals("RedRum", StringComparison.OrdinalIgnoreCase);
        }

        if (IsJadeVariant(map))
        {
            return bot.Name.Equals("RedRum", StringComparison.OrdinalIgnoreCase);
        }

        if (IsAndromedaVariant(map))
        {
            return bot.Name.Equals("Yuanheng Zhu", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsFightingSpiritVariant(PracticeMap map)
    {
        return ContainsFightingSpiritToken(map.Name) ||
               ContainsFightingSpiritToken(map.FileName);
    }

    private static bool IsSteamhammerFamily(PracticeBot bot)
    {
        return string.Equals(
            Path.GetFileName(bot.ExecutableName),
            "Steamhammer.dll",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJadeVariant(PracticeMap map)
    {
        return ContainsToken(map.Name, "Jade") ||
               ContainsToken(map.FileName, "Jade");
    }

    private static bool IsAndromedaVariant(PracticeMap map)
    {
        return ContainsToken(map.Name, "Andromeda") ||
               ContainsToken(map.FileName, "Andromeda");
    }

    private static bool ContainsFightingSpiritToken(string value)
    {
        return ContainsToken(value, "Fighting Spirit");
    }

    private static bool ContainsToken(string value, string token)
    {
        var normalized = value
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);
        return normalized.Contains(token, StringComparison.OrdinalIgnoreCase);
    }
}
