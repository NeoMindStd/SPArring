using Sparring.Client;

namespace Sparring.Tests;

public sealed class BotMatchEndMonitorTests
{
    [Fact]
    public void IgnoresNonGameSamplesUntilBothClientsHaveEnteredGame()
    {
        var monitor = new SmokeChecks.BotMatchEndMonitor(stableNonInGameSamples: 2, replayGraceSamples: 1);

        var observation = monitor.Observe(new SmokeChecks.BotMatchEndSample(
            LeftAlive: true,
            RightAlive: true,
            LeftState: StarCraftScreenState.GameRoom,
            RightState: StarCraftScreenState.GameRoom,
            HasNewReplay: false));

        Assert.False(observation.Ended);
        Assert.False(observation.SawBothInGame);
    }

    [Fact]
    public void EndsAfterStableNonGameSamplesOnceGameWasEntered()
    {
        var monitor = new SmokeChecks.BotMatchEndMonitor(stableNonInGameSamples: 2, replayGraceSamples: 1);

        Assert.False(monitor.Observe(InGameSample()).Ended);
        Assert.False(monitor.Observe(EndedScreenSample(hasNewReplay: false)).Ended);

        var observation = monitor.Observe(EndedScreenSample(hasNewReplay: false));

        Assert.True(observation.Ended);
        Assert.Equal("stable-non-ingame", observation.Reason);
        Assert.Equal(2, observation.StableNonInGameSamples);
    }

    [Fact]
    public void EndsImmediatelyWhenReplayAppearsAfterGameWasEntered()
    {
        var monitor = new SmokeChecks.BotMatchEndMonitor(stableNonInGameSamples: 3, replayGraceSamples: 4);

        Assert.False(monitor.Observe(InGameSample()).Ended);
        var observation = monitor.Observe(InGameSample(hasNewReplay: true));

        Assert.True(observation.Ended);
        Assert.Equal("replay-created", observation.Reason);
    }

    [Fact]
    public void EndsWhenAClientProcessExitsAfterGameWasEntered()
    {
        var monitor = new SmokeChecks.BotMatchEndMonitor(stableNonInGameSamples: 3, replayGraceSamples: 4);

        Assert.False(monitor.Observe(InGameSample()).Ended);
        var observation = monitor.Observe(new SmokeChecks.BotMatchEndSample(
            LeftAlive: false,
            RightAlive: true,
            LeftState: StarCraftScreenState.Unknown,
            RightState: StarCraftScreenState.InGame,
            HasNewReplay: false));

        Assert.True(observation.Ended);
        Assert.Equal("process-exited", observation.Reason);
    }

    [Fact]
    public void WaitsForReplayFilesThatAppearJustAfterEndDetection()
    {
        var calls = 0;

        var replayFiles = SmokeChecks.WaitForReplayFilesAfterEnd(
            findReplayFiles: () => ++calls >= 3 ? ["late.rep"] : [],
            timeout: TimeSpan.FromSeconds(5),
            pollInterval: TimeSpan.FromSeconds(1),
            wait: _ => { });

        Assert.Equal(["late.rep"], replayFiles);
        Assert.Equal(3, calls);
    }

    [Theory]
    [InlineData(true, false, true, 0, true)]
    [InlineData(true, true, true, 0, false)]
    [InlineData(true, false, false, 0, false)]
    [InlineData(true, false, true, 1, false)]
    [InlineData(false, false, true, 0, false)]
    public void FlushesReplayOnlyForEndedUntilEndMatchesWithoutReplay(
        bool untilEnd,
        bool keepOpen,
        bool ended,
        int replayCount,
        bool expected)
    {
        Assert.Equal(expected, SmokeChecks.ShouldFlushReplayAfterBotMatchEnd(
            untilEnd,
            keepOpen,
            ended,
            replayCount));
    }

    private static SmokeChecks.BotMatchEndSample InGameSample(bool hasNewReplay = false)
    {
        return new SmokeChecks.BotMatchEndSample(
            LeftAlive: true,
            RightAlive: true,
            LeftState: StarCraftScreenState.InGame,
            RightState: StarCraftScreenState.InGame,
            HasNewReplay: hasNewReplay);
    }

    private static SmokeChecks.BotMatchEndSample EndedScreenSample(bool hasNewReplay)
    {
        return new SmokeChecks.BotMatchEndSample(
            LeftAlive: true,
            RightAlive: true,
            LeftState: StarCraftScreenState.MenuLike,
            RightState: StarCraftScreenState.GameRoom,
            HasNewReplay: hasNewReplay);
    }
}
