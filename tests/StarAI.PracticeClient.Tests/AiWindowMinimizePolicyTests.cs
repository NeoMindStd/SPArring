using StarAI.PracticeClient.App;

namespace StarAI.PracticeClient.Tests;

public sealed class AiWindowMinimizePolicyTests
{
    [Fact]
    public void DecideMinimizesOnlyBeforeGameStart()
    {
        Assert.Equal(AiWindowMinimizeDecision.MinimizeOnce, AiWindowMinimizePolicy.Decide(StarCraftScreenState.PreGameWait));
    }

    [Fact]
    public void DecideStopsWithoutMinimizingAfterGameStartOrDialog()
    {
        Assert.Equal(AiWindowMinimizeDecision.StopWithoutMinimizing, AiWindowMinimizePolicy.Decide(StarCraftScreenState.InGame));
        Assert.Equal(AiWindowMinimizeDecision.StopWithoutMinimizing, AiWindowMinimizePolicy.Decide(StarCraftScreenState.BlockedDialog));
    }

    [Fact]
    public void DecideKeepsWaitingWhenRoomJoinIsNotConfirmed()
    {
        Assert.Equal(AiWindowMinimizeDecision.Wait, AiWindowMinimizePolicy.Decide(StarCraftScreenState.Unknown));
        Assert.Equal(AiWindowMinimizeDecision.Wait, AiWindowMinimizePolicy.Decide(StarCraftScreenState.MenuLike));
        Assert.Equal(AiWindowMinimizeDecision.Wait, AiWindowMinimizePolicy.Decide(StarCraftScreenState.GameRoom));
    }
}
