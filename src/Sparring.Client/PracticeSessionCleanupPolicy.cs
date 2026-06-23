using Sparring.Core;

namespace Sparring.Client;

internal sealed record PracticeSessionCleanupTargets(
    int? KnownStarCraftProcessId,
    string RuntimeRoot,
    bool LeaveGameBeforeTerminate);

internal static class PracticeSessionCleanupPolicy
{
    public static PracticeSessionCleanupTargets ForGameFinalization(PracticeLaunchPlan plan, PracticeSessionLaunchReport report)
    {
        return new PracticeSessionCleanupTargets(
            report.Ai.StarCraftProcessId,
            plan.Ai.RuntimeRoot,
            LeaveGameBeforeTerminate: true);
    }
}
