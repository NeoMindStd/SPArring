namespace Sparring.Core;

public sealed record BotMatchSelection(
    Guid LeftBotId,
    Guid RightBotId,
    Guid MapId,
    string GameName,
    string? LeftBotBuildId = null,
    string? RightBotBuildId = null,
    bool AllowIncompatible = false);

public sealed record BotMatchLaunchPlan(
    ClientLaunchSettings Left,
    ClientLaunchSettings Right,
    PracticeBot LeftBot,
    PracticeBot RightBot,
    PracticeMap Map);

public static class BotMatchLaunchPlanBuilder
{
    public static BotMatchLaunchPlan Build(
        PracticeCatalog catalog,
        PracticePaths paths,
        BotMatchSelection selection)
    {
        var issues = RuntimeWritePolicy.ValidateLayout(paths);
        if (issues.Count > 0)
        {
            throw new InvalidOperationException(issues[0].Message);
        }

        var leftBot = catalog.FindBot(selection.LeftBotId);
        var rightBot = catalog.FindBot(selection.RightBotId);
        var map = catalog.FindMap(selection.MapId);

        if (!selection.AllowIncompatible)
        {
            EnsureCompatible(catalog, leftBot, map);
            EnsureCompatible(catalog, rightBot, map);
        }

        var leftBuildId = ValidateBuild(leftBot, selection.LeftBotBuildId);
        var rightBuildId = ValidateBuild(rightBot, selection.RightBotBuildId);
        var gameName = string.IsNullOrWhiteSpace(selection.GameName)
            ? "Sparring Bot Match"
            : selection.GameName.Trim();
        var leftCharacterName = PracticeCharacterName.FromBotName(leftBot.Name, gameName);
        var rightCharacterName = PracticeCharacterName.FromBotName(rightBot.Name, gameName);

        var left = new ClientLaunchSettings(
            Role: ClientRuntimeRole.AiOpponent,
            RuntimeRoot: paths.PlayerRuntimeRoot,
            CharacterName: leftCharacterName,
            Race: leftBot.Race,
            EnemyRace: rightBot.Race,
            MapFileName: map.FileName,
            GameName: gameName,
            AiModule: leftBot.UsesBwapiIniAiModule ? leftBot.ExecutableName : string.Empty,
            BotExecutable: leftBot.ExecutableName,
            BotExecutableKind: leftBot.ExecutableKind,
            SoundEnabled: false,
            WindowedMode: false,
            Borderless: false,
            ClipCursor: false,
            ApmAlertEnabled: false,
            EnableWModePlugin: false,
            CncDdrawMode: CncDdrawMode.Windowed,
            BotBuildId: leftBuildId);

        var right = new ClientLaunchSettings(
            Role: ClientRuntimeRole.AiOpponent,
            RuntimeRoot: paths.AiRuntimeRoot,
            CharacterName: rightCharacterName,
            Race: rightBot.Race,
            EnemyRace: leftBot.Race,
            MapFileName: string.Empty,
            GameName: left.CharacterName,
            AiModule: rightBot.UsesBwapiIniAiModule ? rightBot.ExecutableName : string.Empty,
            BotExecutable: rightBot.ExecutableName,
            BotExecutableKind: rightBot.ExecutableKind,
            SoundEnabled: false,
            WindowedMode: false,
            Borderless: false,
            ClipCursor: false,
            ApmAlertEnabled: false,
            EnableWModePlugin: false,
            CncDdrawMode: CncDdrawMode.Windowed,
            BotBuildId: rightBuildId);

        return new BotMatchLaunchPlan(left, right, leftBot, rightBot, map);
    }

    private static void EnsureCompatible(PracticeCatalog catalog, PracticeBot bot, PracticeMap map)
    {
        if (!PracticeCatalogCompatibility.IsCompatible(catalog, bot.Id, map.Id))
        {
            throw new InvalidOperationException($"Bot '{bot.Name}' does not support map '{map.Name}'.");
        }
    }

    private static string? ValidateBuild(PracticeBot bot, string? buildId)
    {
        var normalized = NormalizeBuildId(buildId);
        if (normalized is null)
        {
            return null;
        }

        if (!bot.AvailableBuildOptions.Any(option =>
                string.Equals(option.Id, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Bot '{bot.Name}' does not support build '{normalized}'.");
        }

        return normalized;
    }

    private static string? NormalizeBuildId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "random", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return value.Trim();
    }
}
