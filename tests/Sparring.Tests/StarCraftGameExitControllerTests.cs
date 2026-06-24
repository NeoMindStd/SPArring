using Sparring.Client;

namespace Sparring.Tests;

public sealed class StarCraftGameExitControllerTests
{
    [Fact]
    public void LeaveGameSequenceUsesStarCraftQuitMenuHotkeys()
    {
        Assert.Equal(
            [StarCraftExitKey.F10, StarCraftExitKey.Q, StarCraftExitKey.Q],
            StarCraftGameExitController.LeaveGameSequence);
    }

    [Fact]
    public void ShouldSendLeaveSequenceAllowsCoveredWindowStates()
    {
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.InGame));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.PreGameWait));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.BlockedDialog));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.Unknown));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.MenuLike));
        Assert.True(StarCraftGameExitController.ShouldSendLeaveSequence(StarCraftScreenState.GameRoom));
    }

    [Fact]
    public void IsLeaveCompleteStateOnlyAcceptsStableNonGameScreens()
    {
        Assert.True(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.GameRoom));
        Assert.True(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.MenuLike));
        Assert.False(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.InGame));
        Assert.False(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.PreGameWait));
        Assert.False(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.BlockedDialog));
        Assert.False(StarCraftGameExitController.IsLeaveCompleteState(StarCraftScreenState.Unknown));
    }

    [Fact]
    public void DisconnectProcessWithoutBwapiLeaveTreatsAlreadyExitedProcessAsClean()
    {
        var result = StarCraftGameExitController.DisconnectProcessWithoutBwapiLeave(
            int.MaxValue,
            TimeSpan.FromMilliseconds(1));

        Assert.False(result.ProcessWasRunning);
        Assert.False(result.LeaveSequenceSent);
        Assert.True(result.Exited);
        Assert.True(result.LeaveConfirmed);
        Assert.False(result.ForcedKillUsed);
    }

    [Fact]
    public void IsExpectedBwapiShutdownCrashTextOnlyMatchesKnownDestroyGameShutdown()
    {
        var shutdownCrash = """
            EXCEPTION: 0xE06D7363    UNKNOWN
            STACK:
              BWAPI.dll         0x1003597B
              StarCraft.exe     0x004EE909    DestroyGame
              StarCraft.exe     0x004E0801    preLoadGame
            """;
        var unrelatedCrash = """
            EXCEPTION: 0xE06D7363    UNKNOWN
            STACK:
              BWAPI.dll         0x1003597B
              StarCraft.exe     0x004AAAAA    duringMatch
            """;

        Assert.True(StarCraftGameExitController.IsExpectedBwapiShutdownCrashText(shutdownCrash));
        Assert.False(StarCraftGameExitController.IsExpectedBwapiShutdownCrashText(unrelatedCrash));
    }
}
