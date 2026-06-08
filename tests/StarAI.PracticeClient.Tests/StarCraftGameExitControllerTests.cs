using StarAI.PracticeClient.App;

namespace StarAI.PracticeClient.Tests;

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
}
