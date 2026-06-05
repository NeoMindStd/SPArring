using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class ActionRateCounterTests
{
    [Fact]
    public void ActionsPerMinuteScalesRecordedActionsByElapsedTime()
    {
        var counter = new ActionRateCounter();
        for (var i = 0; i < 12; i++)
        {
            counter.RecordAction();
        }

        Assert.Equal(144, counter.ActionsPerMinute(TimeSpan.FromSeconds(5)));
        Assert.Equal(12, counter.ActionCount);
    }

    [Fact]
    public void ActionsPerMinuteIsZeroBeforeTimerStarts()
    {
        var counter = new ActionRateCounter();
        counter.RecordAction();

        Assert.Equal(0, counter.ActionsPerMinute(TimeSpan.Zero));
    }
}
