namespace StarAI.PracticeClient.App;

internal sealed class StarCraftBorderlessKeeper : IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly int _processId;
    private readonly Rectangle _targetBounds;
    private readonly DateTime _expiresAtUtc;
    private int _running;
    private int _stableMatches;
    private bool _disposed;

    public StarCraftBorderlessKeeper(int processId, Rectangle targetBounds, TimeSpan duration)
    {
        _processId = processId;
        _targetBounds = targetBounds;
        _expiresAtUtc = DateTime.UtcNow + duration;
        _timer = new System.Threading.Timer(_ => Tick(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(1000));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
    }

    private void Tick()
    {
        if (_disposed || Interlocked.Exchange(ref _running, 1) == 1)
        {
            return;
        }

        try
        {
            if (DateTime.UtcNow > _expiresAtUtc)
            {
                Dispose();
                return;
            }

            if (StarCraftBorderlessWindow.EnsureProcessBorderless(_processId, _targetBounds).Applied)
            {
                _stableMatches++;
                if (_stableMatches >= 4)
                {
                    Dispose();
                }
            }
            else
            {
                _stableMatches = 0;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}

internal enum AiWindowMinimizeDecision
{
    Wait,
    MinimizeOnce,
    StopWithoutMinimizing
}

internal static class AiWindowMinimizePolicy
{
    public static AiWindowMinimizeDecision Decide(StarCraftScreenState state)
    {
        return state switch
        {
            StarCraftScreenState.PreGameWait => AiWindowMinimizeDecision.MinimizeOnce,
            StarCraftScreenState.InGame or StarCraftScreenState.BlockedDialog => AiWindowMinimizeDecision.StopWithoutMinimizing,
            _ => AiWindowMinimizeDecision.Wait
        };
    }
}

internal sealed class StarCraftWindowMinimizeOnceWhenReady : IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly int _processId;
    private readonly Func<int, StarCraftScreenState> _detect;
    private readonly Func<int, TimeSpan, bool> _minimize;
    private readonly DateTime _expiresAtUtc;
    private int _running;
    private bool _disposed;

    public StarCraftWindowMinimizeOnceWhenReady(int processId, TimeSpan timeout)
        : this(
            processId,
            timeout,
            StarCraftScreenDetector.Detect,
            StarCraftBorderlessWindow.MinimizeProcessWindowWhenReady,
            startTimer: true)
    {
    }

    internal StarCraftWindowMinimizeOnceWhenReady(
        int processId,
        TimeSpan timeout,
        Func<int, StarCraftScreenState> detect,
        Func<int, TimeSpan, bool> minimize,
        bool startTimer)
    {
        _processId = processId;
        _detect = detect;
        _minimize = minimize;
        _expiresAtUtc = DateTime.UtcNow + timeout;
        _timer = new System.Threading.Timer(
            _ => Tick(),
            null,
            startTimer ? TimeSpan.Zero : Timeout.InfiniteTimeSpan,
            TimeSpan.FromMilliseconds(500));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
    }

    internal bool StepOnce()
    {
        if (_disposed)
        {
            return true;
        }

        if (DateTime.UtcNow > _expiresAtUtc)
        {
            Dispose();
            return true;
        }

        var decision = AiWindowMinimizePolicy.Decide(_detect(_processId));
        if (decision == AiWindowMinimizeDecision.Wait)
        {
            return false;
        }

        if (decision == AiWindowMinimizeDecision.MinimizeOnce)
        {
            _ = _minimize(_processId, TimeSpan.FromMilliseconds(250));
        }

        Dispose();
        return true;
    }

    private void Tick()
    {
        if (_disposed || Interlocked.Exchange(ref _running, 1) == 1)
        {
            return;
        }

        try
        {
            StepOnce();
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
