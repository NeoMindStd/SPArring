using Sparring.Client;

namespace Sparring.Tests;

public sealed class SmokeStartObservationPolicyTests
{
    [Fact]
    public void NoObserveWindowDoesNotRequirePostObserveState()
    {
        Assert.True(SmokeStartObservationPolicy.IsStableAfterObserve(
            0,
            playerProcessAlive: false,
            aiProcessAlive: false,
            StarCraftScreenState.Unknown,
            StarCraftScreenState.Unknown));
    }

    [Fact]
    public void ObserveWindowRequiresBothClientsStillInGame()
    {
        Assert.True(SmokeStartObservationPolicy.IsStableAfterObserve(
            60,
            playerProcessAlive: true,
            aiProcessAlive: true,
            StarCraftScreenState.InGame,
            StarCraftScreenState.InGame));

        Assert.False(SmokeStartObservationPolicy.IsStableAfterObserve(
            60,
            playerProcessAlive: true,
            aiProcessAlive: false,
            StarCraftScreenState.InGame,
            StarCraftScreenState.InGame));

        Assert.False(SmokeStartObservationPolicy.IsStableAfterObserve(
            60,
            playerProcessAlive: true,
            aiProcessAlive: true,
            StarCraftScreenState.InGame,
            StarCraftScreenState.GameRoom));
    }
}
