using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class PracticeSessionClockTests
{
    [Fact]
    public void ElapsedAtUsesRealUtcTimeFromGameStart()
    {
        var gameStartedAt = new DateTime(2026, 6, 6, 11, 0, 0, DateTimeKind.Utc);
        var clock = new PracticeSessionClock(gameStartedAt);

        Assert.Equal(TimeSpan.FromSeconds(74), clock.ElapsedAt(gameStartedAt.AddSeconds(74)));
    }

    [Fact]
    public void ElapsedAtIgnoresLauncherOrRoomWaitTimeBeforeGameStart()
    {
        var launcherClickedAt = new DateTime(2026, 6, 6, 10, 59, 0, DateTimeKind.Utc);
        var gameStartedAt = launcherClickedAt.AddSeconds(45);
        var clock = new PracticeSessionClock(gameStartedAt);

        Assert.Equal(TimeSpan.FromSeconds(2), clock.ElapsedAt(gameStartedAt.AddSeconds(2)));
    }

    [Fact]
    public void ElapsedAtClampsNegativeClockSkew()
    {
        var gameStartedAt = new DateTime(2026, 6, 6, 11, 0, 0, DateTimeKind.Utc);
        var clock = new PracticeSessionClock(gameStartedAt);

        Assert.Equal(TimeSpan.Zero, clock.ElapsedAt(gameStartedAt.AddSeconds(-1)));
    }

    [Fact]
    public void FormatOverlayTextUsesElapsedRealTimeAndApm()
    {
        var gameStartedAt = new DateTime(2026, 6, 6, 11, 0, 0, DateTimeKind.Utc);
        var clock = new PracticeSessionClock(gameStartedAt);
        var counter = new ActionRateCounter();
        for (var index = 0; index < 6; index++)
        {
            counter.RecordAction();
        }

        Assert.Equal("01:15  APM 5", clock.FormatOverlayText(gameStartedAt.AddSeconds(75), counter));
    }
}
