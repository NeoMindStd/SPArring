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

    [Fact]
    public void ObserveWindowAcceptsAiLogActivityWhenAiScreenIsBlocked()
    {
        Assert.True(SmokeStartObservationPolicy.IsStableAfterObserve(
            60,
            playerProcessAlive: true,
            aiProcessAlive: true,
            StarCraftScreenState.InGame,
            StarCraftScreenState.BlockedDialog,
            aiLogActivityDetected: true));

        Assert.False(SmokeStartObservationPolicy.IsStableAfterObserve(
            60,
            playerProcessAlive: true,
            aiProcessAlive: false,
            StarCraftScreenState.InGame,
            StarCraftScreenState.BlockedDialog,
            aiLogActivityDetected: true));
    }

    [Fact]
    public void ActivityObserveDoesNotStopBeforeMinimumWindow()
    {
        Assert.False(SmokeStartObservationPolicy.CanStopEarlyAfterActivity(
            observeSeconds: 120,
            requireAiActivity: true,
            elapsed: TimeSpan.FromSeconds(20),
            playerProcessAlive: true,
            aiProcessAlive: true,
            StarCraftScreenState.InGame,
            StarCraftScreenState.InGame,
            aiActivityDetected: true));
    }

    [Fact]
    public void ActivityObserveStopsAfterMinimumWhenClientsAreStableAndAiMoved()
    {
        Assert.True(SmokeStartObservationPolicy.CanStopEarlyAfterActivity(
            observeSeconds: 120,
            requireAiActivity: true,
            elapsed: TimeSpan.FromSeconds(31),
            playerProcessAlive: true,
            aiProcessAlive: true,
            StarCraftScreenState.InGame,
            StarCraftScreenState.InGame,
            aiActivityDetected: true));
    }

    [Fact]
    public void ActivityObserveKeepsWaitingWhenAiIsNotInGame()
    {
        Assert.False(SmokeStartObservationPolicy.CanStopEarlyAfterActivity(
            observeSeconds: 120,
            requireAiActivity: true,
            elapsed: TimeSpan.FromSeconds(45),
            playerProcessAlive: true,
            aiProcessAlive: true,
            StarCraftScreenState.InGame,
            StarCraftScreenState.GameRoom,
            aiActivityDetected: true));
    }

    [Fact]
    public void ActivityObserveKeepsWaitingWithoutActivityRequirement()
    {
        Assert.False(SmokeStartObservationPolicy.CanStopEarlyAfterActivity(
            observeSeconds: 120,
            requireAiActivity: false,
            elapsed: TimeSpan.FromSeconds(45),
            playerProcessAlive: true,
            aiProcessAlive: true,
            StarCraftScreenState.InGame,
            StarCraftScreenState.InGame,
            aiActivityDetected: true));
    }
}
