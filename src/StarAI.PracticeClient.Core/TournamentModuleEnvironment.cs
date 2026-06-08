namespace StarAI.PracticeClient.Core;

public static class TournamentModuleEnvironment
{
    public static IReadOnlyDictionary<string, string> ForPlayerRuntime()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TM_DISABLE_USER_INPUT"] = "false",
            ["TM_DISABLE_DRAW_GAME_TIMER"] = "true",
            ["TM_DRAW_UNIT_INFO"] = "false",
            ["TM_DRAW_BOT_NAMES"] = "false",
            ["TM_DRAW_TOURNAMENT_INFO"] = "false",
            ["TM_DISABLE_USER_INPUT_LOCAL_SPEED"] = "true",
            ["TM_LOCAL_SPEED"] = "42",
            ["TM_AUTO_OBS"] = "false",
            ["TM_STATE_FILE"] = @"bwapi-data\gameState.txt"
        };
    }

    public static IDisposable ApplyFor(ClientRuntimeRole role)
    {
        return role == ClientRuntimeRole.PlayerHost
            ? new EnvironmentVariableScope(ForPlayerRuntime())
            : NoopScope.Instance;
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues;
        private bool _disposed;

        public EnvironmentVariableScope(IReadOnlyDictionary<string, string> variables)
        {
            _originalValues = variables.Keys.ToDictionary(
                key => key,
                Environment.GetEnvironmentVariable,
                StringComparer.OrdinalIgnoreCase);

            foreach (var (key, value) in variables)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var (key, value) in _originalValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();

        public void Dispose()
        {
        }
    }
}
