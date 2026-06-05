namespace StarAI.PracticeClient.Core;

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
