namespace StarAI.PracticeClient.Core;

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
    bool AllowApmAlert);

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
    CncDdrawMode CncDdrawMode);

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

        var player = new ClientLaunchSettings(
            Role: ClientRuntimeRole.PlayerHost,
            RuntimeRoot: paths.PlayerRuntimeRoot,
            CharacterName: "StarAIHuman",
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
            CharacterName: "StarAIBot",
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
            CncDdrawMode: CncDdrawMode.Windowed);

        return new PracticeLaunchPlan(player, ai, bot, map);
    }
}
