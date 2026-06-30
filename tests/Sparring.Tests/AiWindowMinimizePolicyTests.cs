using Sparring.Client;

namespace Sparring.Tests;

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
        Assert.Equal(AiWindowMinimizeDecision.StopWithoutMinimizing, AiWindowMinimizePolicy.Decide(StarCraftScreenState.BlockedDialog));
    }

    [Fact]
    public void DecideStillMinimizesWhenAiAlreadyReachedInGame()
    {
        Assert.Equal(AiWindowMinimizeDecision.MinimizeOnce, AiWindowMinimizePolicy.Decide(StarCraftScreenState.InGame));
    }

    [Fact]
    public void DecideKeepsWaitingWhenRoomJoinIsNotConfirmed()
    {
        Assert.Equal(AiWindowMinimizeDecision.Wait, AiWindowMinimizePolicy.Decide(StarCraftScreenState.Unknown));
        Assert.Equal(AiWindowMinimizeDecision.Wait, AiWindowMinimizePolicy.Decide(StarCraftScreenState.MenuLike));
        Assert.Equal(AiWindowMinimizeDecision.Wait, AiWindowMinimizePolicy.Decide(StarCraftScreenState.GameRoom));
    }

    [Fact]
    public void StepOnceMinimizesWhenAiAlreadyReachedInGame()
    {
        var minimizeCalls = 0;
        using var minimizer = new StarCraftWindowMinimizeOnceWhenReady(
            123,
            TimeSpan.FromSeconds(5),
            _ => StarCraftScreenState.InGame,
            (_, _) =>
            {
                minimizeCalls++;
                return true;
            },
            startTimer: false);

        Assert.True(minimizer.StepOnce());
        Assert.Equal(1, minimizeCalls);
    }

    [Fact]
    public void StepOnceRetriesWhenWindowIsNotReadyYet()
    {
        var minimizeCalls = 0;
        using var minimizer = new StarCraftWindowMinimizeOnceWhenReady(
            123,
            TimeSpan.FromSeconds(5),
            _ => StarCraftScreenState.InGame,
            (_, _) => ++minimizeCalls >= 2,
            startTimer: false);

        Assert.False(minimizer.StepOnce());
        Assert.True(minimizer.StepOnce());
        Assert.Equal(2, minimizeCalls);
    }
}
