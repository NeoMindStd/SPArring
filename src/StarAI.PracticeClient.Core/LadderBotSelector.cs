namespace StarAI.PracticeClient.Core;

public static class LadderBotSelector
{
    private const double RatingWeightSigma = 250.0;

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

    public static IReadOnlyList<PracticeBot> CandidatesForEnabledMaps(
        PracticeCatalog catalog,
        StarCraftRace? enemyRace)
    {
        return catalog.Bots
            .Where(bot => bot.UsesBwapiIniAiModule)
            .Where(bot => RaceMatches(bot.Race, enemyRace))
            .Where(bot => PracticeCatalogCompatibility.MapsForBot(catalog, bot.Id).Count > 0)
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

    public static PracticeBot PickForRating(
        PracticeCatalog catalog,
        Guid mapId,
        StarCraftRace? enemyRace,
        int playerRating,
        Random random)
    {
        var candidates = CandidatesForMap(catalog, mapId, enemyRace);
        if (candidates.Count == 0)
        {
            var map = catalog.FindMap(mapId);
            var raceText = enemyRace is null ? "any race" : enemyRace.Value.ToString();
            throw new InvalidOperationException($"No compatible ladder bot found for '{map.Name}' and '{raceText}'.");
        }

        return PickWeighted(candidates, playerRating, random);
    }

    public static PracticeBot PickForRatingAcrossEnabledMaps(
        PracticeCatalog catalog,
        StarCraftRace? enemyRace,
        int playerRating,
        Random random)
    {
        var candidates = CandidatesForEnabledMaps(catalog, enemyRace);
        if (candidates.Count == 0)
        {
            var raceText = enemyRace is null ? "any race" : enemyRace.Value.ToString();
            throw new InvalidOperationException($"No compatible ladder bot found for enabled maps and '{raceText}'.");
        }

        return PickWeighted(candidates, playerRating, random);
    }

    public static double RatingWeight(int playerRating, int? botElo)
    {
        var opponentRating = botElo ?? EloRatingCalculator.DefaultRating;
        var distance = opponentRating - playerRating;
        return Math.Exp(-(distance * distance) / (2.0 * RatingWeightSigma * RatingWeightSigma));
    }

    private static PracticeBot PickWeighted(
        IReadOnlyList<PracticeBot> candidates,
        int playerRating,
        Random random)
    {
        var weighted = candidates
            .Select(bot => new
            {
                Bot = bot,
                Weight = RatingWeight(playerRating, bot.Elo)
            })
            .Where(item => item.Weight > 0)
            .ToList();

        var totalWeight = weighted.Sum(item => item.Weight);
        if (totalWeight <= 0)
        {
            return candidates[random.Next(candidates.Count)];
        }

        var target = random.NextDouble() * totalWeight;
        foreach (var item in weighted)
        {
            target -= item.Weight;
            if (target <= 0)
            {
                return item.Bot;
            }
        }

        return weighted[^1].Bot;
    }

    private static bool RaceMatches(StarCraftRace botRace, StarCraftRace? enemyRace)
    {
        return enemyRace is null || botRace == enemyRace.Value;
    }
}
