namespace Sparring.Client;

internal static class SmokeStartObservationPolicy
{
    public static bool IsStableAfterObserve(
        int observeSeconds,
        bool playerProcessAlive,
        bool aiProcessAlive,
        StarCraftScreenState playerState,
        StarCraftScreenState aiState)
    {
        if (observeSeconds <= 0)
        {
            return true;
        }

        return playerProcessAlive &&
               aiProcessAlive &&
               playerState == StarCraftScreenState.InGame &&
               aiState == StarCraftScreenState.InGame;
    }
}
