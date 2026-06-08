namespace StarAI.PracticeClient.Core;

public enum StarCraftRace
{
    Terran,
    Protoss,
    Zerg,
    Random,
    Unknown
}

public enum BotExecutableKind
{
    Dll,
    ClientExe,
    ClientJar,
    Unknown
}

public sealed record PracticeMap(
    Guid Id,
    string Name,
    string FileName,
    string? ImagePath,
    bool Enabled,
    string? SourcePath = null,
    bool IsUserMap = false,
    IReadOnlySet<Guid>? CompatibilityMapIds = null)
{
    public IReadOnlySet<Guid> EffectiveCompatibilityMapIds =>
        CompatibilityMapIds is { Count: > 0 }
            ? CompatibilityMapIds
            : new HashSet<Guid> { Id };
}

public sealed record PracticeBot(
    Guid Id,
    string Name,
    StarCraftRace Race,
    string ExecutableName,
    BotExecutableKind ExecutableKind,
    string BwapiVersion,
    int? Elo,
    bool PracticeOnly,
    IReadOnlySet<Guid> SupportedMapIds,
    string? Description,
    string? Author,
    string? SourceDirectory = null)
{
    public bool SupportsMap(Guid mapId)
    {
        return SupportedMapIds.Count == 0 || SupportedMapIds.Contains(mapId);
    }

    public bool UsesBwapiIniAiModule => ExecutableKind == BotExecutableKind.Dll;
}

public sealed record PracticeCatalog(
    IReadOnlyList<PracticeBot> Bots,
    IReadOnlyList<PracticeMap> Maps)
{
    public PracticeBot FindBot(Guid id)
    {
        return Bots.FirstOrDefault(bot => bot.Id == id)
            ?? throw new InvalidOperationException($"Bot not found: {id}");
    }

    public PracticeMap FindMap(Guid id)
    {
        return Maps.FirstOrDefault(map => map.Id == id)
            ?? throw new InvalidOperationException($"Map not found: {id}");
    }
}
