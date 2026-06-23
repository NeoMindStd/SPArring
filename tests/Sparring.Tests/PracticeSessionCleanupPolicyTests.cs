using Sparring.Client;
using Sparring.Core;

namespace Sparring.Tests;

public sealed class PracticeSessionCleanupPolicyTests
{
    [Fact]
    public void ForGameFinalizationTargetsOnlyAiRuntime()
    {
        var plan = new PracticeLaunchPlan(
            Player: new ClientLaunchSettings(
                ClientRuntimeRole.PlayerHost,
                RuntimeRoot: @"C:\sparring\SC116AI",
                CharacterName: "SparringHuman",
                Race: StarCraftRace.Protoss,
                EnemyRace: StarCraftRace.Terran,
                MapFileName: @"maps\Sparring\(4)Fighting Spirit.scx",
                GameName: "Sparring Practice",
                AiModule: string.Empty,
                BotExecutable: string.Empty,
                BotExecutableKind: BotExecutableKind.Unknown,
                SoundEnabled: true,
                WindowedMode: false,
                Borderless: true,
                ClipCursor: false,
                ApmAlertEnabled: false,
                EnableWModePlugin: false,
                CncDdrawMode: CncDdrawMode.BorderlessFullscreen),
            Ai: new ClientLaunchSettings(
                ClientRuntimeRole.AiOpponent,
                RuntimeRoot: @"C:\sparring\SC116AI_ai",
                CharacterName: "SparringBot",
                Race: StarCraftRace.Terran,
                EnemyRace: StarCraftRace.Protoss,
                MapFileName: string.Empty,
                GameName: "JOIN_FIRST",
                AiModule: @"bwapi-data\AI\Sparring\Bots\Dragon\dragon.dll",
                BotExecutable: @"bwapi-data\AI\Sparring\Bots\Dragon\dragon.dll",
                BotExecutableKind: BotExecutableKind.Dll,
                SoundEnabled: false,
                WindowedMode: false,
                Borderless: false,
                ClipCursor: false,
                ApmAlertEnabled: false,
                EnableWModePlugin: false,
                CncDdrawMode: CncDdrawMode.Windowed),
            Bot: new PracticeBot(
                Guid.NewGuid(),
                "Dragon",
                StarCraftRace.Terran,
                "dragon.dll",
                BotExecutableKind.Dll,
                "4.4.0",
                1081,
                false,
                new HashSet<Guid>(),
                null,
                null,
                @"C:\Program Files (x86)\SCHNAIL Client\bots\Dragon"),
            Map: new PracticeMap(
                Guid.NewGuid(),
                "(4)Fighting Spirit",
                "(4)Fighting Spirit.scx",
                null,
                true,
                @"C:\Program Files (x86)\SCHNAIL Client\maps\Fighting_Spirit_1.4.scx"));
        var report = new PracticeSessionLaunchReport(
            Player: new SparringClientLaunchReport(ClientRuntimeRole.PlayerHost, @"C:\sparring\SC116AI", 10, 111, 0, 1),
            Ai: new SparringClientLaunchReport(ClientRuntimeRole.AiOpponent, @"C:\sparring\SC116AI_ai", 20, 222, 0, 1),
            StoppedLocalProcesses: 0);

        var targets = PracticeSessionCleanupPolicy.ForGameFinalization(plan, report);

        Assert.Equal(222, targets.KnownStarCraftProcessId);
        Assert.Equal(@"C:\sparring\SC116AI_ai", targets.RuntimeRoot);
        Assert.True(targets.LeaveGameBeforeTerminate);
    }
}
