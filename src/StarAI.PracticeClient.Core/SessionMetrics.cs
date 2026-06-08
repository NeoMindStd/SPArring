namespace StarAI.PracticeClient.Core;

public readonly record struct PracticeSessionClock(DateTime StartedAtUtc)
{
    public TimeSpan ElapsedAt(DateTime utcNow)
    {
        var elapsed = utcNow - StartedAtUtc;
        return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
    }

    public static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        var minutes = (int)elapsed.TotalMinutes;
        var seconds = elapsed.Seconds;
        return $"{minutes:00}:{seconds:00}";
    }

    public string FormatOverlayText(DateTime utcNow, ActionRateCounter counter)
    {
        var elapsed = ElapsedAt(utcNow);
        return $"{FormatElapsed(elapsed)}  APM {counter.ActionsPerMinute(elapsed)}";
    }
}

public sealed class ActionRateCounter
{
    private int _actions;

    public int ActionCount => Volatile.Read(ref _actions);

    public void RecordAction()
    {
        Interlocked.Increment(ref _actions);
    }

    public int ActionsPerMinute(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds <= 0)
        {
            return 0;
        }

        return (int)Math.Round(ActionCount * 60.0 / elapsed.TotalSeconds, MidpointRounding.AwayFromZero);
    }
}
