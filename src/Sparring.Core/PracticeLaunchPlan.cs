namespace Sparring.Core;

public enum ClientRuntimeRole
{
    PlayerHost,
    AiOpponent
}

public sealed record PracticeSelection(
    Guid BotId,
    Guid MapId,
    StarCraftRace PlayerRace,
    string GameName,
    bool PlayerBorderless,
    bool ClipCursor,
    bool AllowApmAlert,
    bool HideAiName = true,
    string? BotBuildId = null);

public sealed record ClientLaunchSettings(
    ClientRuntimeRole Role,
    string RuntimeRoot,
    string CharacterName,
    StarCraftRace Race,
    StarCraftRace EnemyRace,
    string MapFileName,
    string GameName,
    string AiModule,
    string BotExecutable,
    BotExecutableKind BotExecutableKind,
    bool SoundEnabled,
    bool WindowedMode,
    bool Borderless,
    bool ClipCursor,
    bool ApmAlertEnabled,
    bool EnableWModePlugin,
    CncDdrawMode CncDdrawMode,
    string? BotBuildId = null);

public enum CncDdrawMode
{
    Disabled,
    BorderlessFullscreen,
    Windowed
}

public sealed record PracticeLaunchPlan(
    ClientLaunchSettings Player,
    ClientLaunchSettings Ai,
    PracticeBot Bot,
    PracticeMap Map);

public static class PracticeLaunchPlanBuilder
{
    public static PracticeLaunchPlan Build(
        PracticeCatalog catalog,
        PracticePaths paths,
        PracticeSelection selection)
    {
        var issues = RuntimeWritePolicy.ValidateLayout(paths);
        if (issues.Count > 0)
        {
            throw new InvalidOperationException(issues[0].Message);
        }

        var bot = catalog.FindBot(selection.BotId);
        var map = catalog.FindMap(selection.MapId);
        if (!PracticeCatalogCompatibility.IsCompatible(catalog, bot.Id, map.Id))
        {
            throw new InvalidOperationException($"Bot '{bot.Name}' does not support map '{map.Name}'.");
        }

        var botBuildId = NormalizeBotBuildId(selection.BotBuildId);
        if (botBuildId is not null &&
            !bot.AvailableBuildOptions.Any(option => string.Equals(option.Id, botBuildId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Bot '{bot.Name}' does not support build '{botBuildId}'.");
        }

        var player = new ClientLaunchSettings(
            Role: ClientRuntimeRole.PlayerHost,
            RuntimeRoot: paths.PlayerRuntimeRoot,
            CharacterName: "SparringHuman",
            Race: selection.PlayerRace,
            EnemyRace: bot.Race,
            MapFileName: map.FileName,
            GameName: selection.GameName,
            AiModule: string.Empty,
            BotExecutable: string.Empty,
            BotExecutableKind: BotExecutableKind.Unknown,
            SoundEnabled: true,
            WindowedMode: false,
            Borderless: selection.PlayerBorderless,
            ClipCursor: selection.ClipCursor,
            ApmAlertEnabled: selection.AllowApmAlert,
            EnableWModePlugin: false,
            CncDdrawMode: CncDdrawMode.BorderlessFullscreen);

        var ai = new ClientLaunchSettings(
            Role: ClientRuntimeRole.AiOpponent,
            RuntimeRoot: paths.AiRuntimeRoot,
            CharacterName: selection.HideAiName
                ? "SparringBot"
                : PracticeCharacterName.FromBotName(bot.Name),
            Race: bot.Race,
            EnemyRace: selection.PlayerRace,
            MapFileName: string.Empty,
            GameName: "JOIN_FIRST",
            AiModule: bot.UsesBwapiIniAiModule ? bot.ExecutableName : string.Empty,
            BotExecutable: bot.ExecutableName,
            BotExecutableKind: bot.ExecutableKind,
            SoundEnabled: false,
            WindowedMode: false,
            Borderless: false,
            ClipCursor: false,
            ApmAlertEnabled: false,
            EnableWModePlugin: false,
            CncDdrawMode: CncDdrawMode.Windowed,
            BotBuildId: botBuildId);

        return new PracticeLaunchPlan(player, ai, bot, map);
    }

    private static string? NormalizeBotBuildId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "random", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return value.Trim();
    }
}

public static class PracticeCharacterName
{
    private const int MaxCharacterNameLength = 24;

    public static string FromBotName(string botName, string? qualifier = null)
    {
        var sanitized = new string((botName ?? string.Empty)
            .Where(character => !char.IsControl(character))
            .ToArray())
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "SparringBot";
        }

        var suffix = ShortQualifierSuffix(qualifier);
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            var suffixWithSeparator = "-" + suffix;
            var baseLength = Math.Max(1, MaxCharacterNameLength - suffixWithSeparator.Length);
            var baseName = sanitized.Length <= baseLength
                ? sanitized
                : sanitized[..baseLength].Trim();
            return (baseName + suffixWithSeparator).Trim();
        }

        return sanitized.Length <= MaxCharacterNameLength
            ? sanitized
            : sanitized[..MaxCharacterNameLength].Trim();
    }

    private static string? ShortQualifierSuffix(string? qualifier)
    {
        if (string.IsNullOrWhiteSpace(qualifier))
        {
            return null;
        }

        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in qualifier.Trim())
            {
                hash ^= char.ToUpperInvariant(character);
                hash *= 16777619;
            }

            return (hash & 0xFFFFFF).ToString("x6");
        }
    }
}
