namespace StarAI.PracticeClient.Core;

public static class PracticeSelection
{
    public static int FindMapIndex(IReadOnlyList<MapProfile> maps, string? preferredRelativePath)
    {
        if (maps.Count == 0)
        {
            return -1;
        }

        if (!string.IsNullOrWhiteSpace(preferredRelativePath))
        {
            var preferredIndex = maps
                .ToList()
                .FindIndex(map => string.Equals(map.RelativePath, preferredRelativePath, StringComparison.OrdinalIgnoreCase));
            if (preferredIndex >= 0)
            {
                return preferredIndex;
            }
        }

        var fightingSpiritIndex = maps
            .ToList()
            .FindIndex(map => map.Name.Contains("Fighting Spirit", StringComparison.OrdinalIgnoreCase));

        return fightingSpiritIndex >= 0 ? fightingSpiritIndex : 0;
    }
}
