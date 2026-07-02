namespace Sparring.Client;

internal static class SmokeStartObservationPolicy
{
    public static readonly TimeSpan MinimumEarlyActivityObserveDuration = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan ActivityProbeInterval = TimeSpan.FromSeconds(15);

    public static bool IsStableAfterObserve(
        int observeSeconds,
        bool playerProcessAlive,
        bool aiProcessAlive,
        StarCraftScreenState playerState,
        StarCraftScreenState aiState,
        bool aiLogActivityDetected = false)
    {
        if (observeSeconds <= 0)
        {
            return true;
        }

        return playerProcessAlive &&
               aiProcessAlive &&
               playerState == StarCraftScreenState.InGame &&
               (aiState == StarCraftScreenState.InGame || aiLogActivityDetected);
    }

    public static bool CanStopEarlyAfterActivity(
        int observeSeconds,
        bool requireAiActivity,
        TimeSpan elapsed,
        bool playerProcessAlive,
        bool aiProcessAlive,
        StarCraftScreenState playerState,
        StarCraftScreenState aiState,
        bool aiActivityDetected,
        bool aiLogActivityDetected = false)
    {
        if (!requireAiActivity ||
            observeSeconds <= 0 ||
            elapsed < MinimumEarlyActivityObserveDuration ||
            !aiActivityDetected)
        {
            return false;
        }

        return IsStableAfterObserve(
            observeSeconds,
            playerProcessAlive,
            aiProcessAlive,
            playerState,
            aiState,
            aiLogActivityDetected);
    }
}
