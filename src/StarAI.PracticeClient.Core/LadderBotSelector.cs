namespace StarAI.PracticeClient.Core;

public static class LadderBotSelector
{
    public static IReadOnlyList<PracticeBot> CandidatesForMap(
        PracticeCatalog catalog,
        Guid mapId,
        StarCraftRace? enemyRace)
    {
        return PracticeCatalogCompatibility.BotsForMap(catalog, mapId)
            .Where(bot => bot.UsesBwapiIniAiModule)
            .Where(bot => RaceMatches(bot.Race, enemyRace))
            .OrderByDescending(bot => bot.Elo ?? int.MinValue)
            .ThenBy(bot => bot.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static PracticeBot PickRandom(
        PracticeCatalog catalog,
        Guid mapId,
        StarCraftRace? enemyRace,
        Random random)
    {
        var candidates = CandidatesForMap(catalog, mapId, enemyRace);
        if (candidates.Count == 0)
        {
            var map = catalog.FindMap(mapId);
            var raceText = enemyRace is null ? "any race" : enemyRace.Value.ToString();
            throw new InvalidOperationException($"No compatible ladder bot found for '{map.Name}' and '{raceText}'.");
        }

        return candidates[random.Next(candidates.Count)];
    }

    private static bool RaceMatches(StarCraftRace botRace, StarCraftRace? enemyRace)
    {
        return enemyRace is null || botRace == enemyRace.Value;
    }
}
