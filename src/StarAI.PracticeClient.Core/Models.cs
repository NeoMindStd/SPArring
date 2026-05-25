namespace StarAI.PracticeClient.Core;

public enum Race
{
    Random,
    Terran,
    Protoss,
    Zerg
}

public enum DifficultyTier
{
    Recovery,
    Main,
    Challenge,
    Drill,
    Experimental
}

public enum BuildPatchKind
{
    None,
    UAlbertaRaceStrategy,
    MatchupWeightedStrategy
}

public sealed record BuildPatch(BuildPatchKind Kind, string ConfigRelativePath, string StrategyId);

public sealed record BuildOption(
    string Id,
    string Name,
    string Description,
    BuildPatch? Patch = null);

public sealed record BotProfile(
    string Id,
    string Name,
    Race Race,
    DifficultyTier Tier,
    string RelativeDllPath,
    string Style,
    string BuildHints,
    string MicroRisk,
    IReadOnlyList<BuildOption> BuildOptions,
    int? Elo = null,
    IReadOnlyList<string>? Tags = null)
{
    public string TierLabel => Tier switch
    {
        DifficultyTier.Recovery => "Recovery",
        DifficultyTier.Main => "Main",
        DifficultyTier.Challenge => "Challenge",
        DifficultyTier.Drill => "Drill",
        DifficultyTier.Experimental => "Experimental",
        _ => Tier.ToString()
    };

    public IReadOnlyList<string> SearchTags => Tags ?? Array.Empty<string>();

    public string DllPath(string starCraftRoot) => Path.Combine(starCraftRoot, RelativeDllPath.Replace('/', Path.DirectorySeparatorChar));
}

public sealed record MapProfile(string Name, string RelativePath, int? Players)
{
    public override string ToString() => Players is null ? Name : $"({Players}) {Name}";
}

public sealed record PracticeSettings(
    string StarCraftRoot,
    BotProfile Bot,
    MapProfile Map,
    Race PlayerRace,
    string GameName,
    bool WindowedMode,
    int? SpeedOverrideMs,
    BuildOption? BuildOption);

public sealed record ValidationIssue(string Message, bool IsError);
