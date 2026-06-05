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

internal sealed class StarCraftWindowMinimizeKeeper : IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly int _processId;
    private readonly DateTime _expiresAtUtc;
    private int _running;
    private int _stableMatches;
    private bool _disposed;

    public StarCraftWindowMinimizeKeeper(int processId, TimeSpan delay, TimeSpan duration)
    {
        _processId = processId;
        _expiresAtUtc = DateTime.UtcNow + delay + duration;
        _timer = new System.Threading.Timer(_ => Tick(), null, delay, TimeSpan.FromSeconds(1));
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

            if (StarCraftBorderlessWindow.MinimizeProcessWindowWhenReady(_processId, TimeSpan.FromMilliseconds(250)))
            {
                _stableMatches++;
                if (_stableMatches >= 3)
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
