using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sparring.Client;

internal enum StarCraftExitKey
{
    F10,
    Q
}

internal sealed record StarCraftAiShutdownResult(
    bool ProcessWasRunning,
    bool LeaveSequenceSent,
    bool Exited,
    bool LeaveConfirmed,
    bool ForcedKillUsed);

internal sealed record StarCraftLeaveGameResult(
    bool SequenceSent,
    bool LeaveConfirmed,
    bool ProcessStillRunning,
    StarCraftScreenState LastState);

internal static class StarCraftGameExitController
{
    internal static readonly TimeSpan ExpectedShutdownCrashCleanupRetryWindow = TimeSpan.FromSeconds(20);

    private static readonly TimeSpan ExpectedShutdownCrashInitialQuietWindow = TimeSpan.FromSeconds(8);

    private static readonly TimeSpan ExpectedShutdownCrashPostActivityQuietWindow = TimeSpan.FromSeconds(2);

    internal static IReadOnlyList<StarCraftExitKey> LeaveGameSequence { get; } =
        [StarCraftExitKey.F10, StarCraftExitKey.Q, StarCraftExitKey.Q];

    internal static bool ShouldSendLeaveSequence(StarCraftScreenState state)
    {
        return true;
    }

    public static StarCraftAiShutdownResult LeaveGameThenCloseProcess(
        int processId,
        TimeSpan leaveWait,
        TimeSpan closeWait)
    {
        if (!TryGetRunningProcess(processId, out var process))
        {
            return new StarCraftAiShutdownResult(false, false, true, true, false);
        }

        using (process)
        {
            var leaveResult = TrySendLeaveGameSequence(processId, leaveWait);
            if (!leaveResult.ProcessStillRunning)
            {
                return new StarCraftAiShutdownResult(
                    ProcessWasRunning: true,
                    LeaveSequenceSent: leaveResult.SequenceSent,
                    Exited: true,
                    LeaveConfirmed: leaveResult.LeaveConfirmed,
                    ForcedKillUsed: false);
            }

            var (exited, forcedKillUsed) = CloseProcess(
                process,
                closeWait,
                allowForceKill: leaveResult.LeaveConfirmed);
            return new StarCraftAiShutdownResult(
                ProcessWasRunning: true,
                LeaveSequenceSent: leaveResult.SequenceSent,
                Exited: exited,
                LeaveConfirmed: leaveResult.LeaveConfirmed,
                ForcedKillUsed: forcedKillUsed);
        }
    }

    public static StarCraftAiShutdownResult DisconnectProcessWithoutBwapiLeave(
        int processId,
        TimeSpan closeWait,
        string? errorDirectory = null)
    {
        if (!TryGetRunningProcess(processId, out var process))
        {
            return new StarCraftAiShutdownResult(false, false, true, true, false);
        }

        var errorBaseline = RuntimeErrorBaseline.Capture(errorDirectory);
        using (process)
        {
            var exited = TryKill(process, entireProcessTree: false, closeWait) ||
                         TryKill(process, entireProcessTree: true, closeWait) ||
                         WaitForProcessExit(processId, TimeSpan.FromSeconds(5)) ||
                         !ProcessExists(processId);
            var runtimeErrorRetryWindow = closeWait < ExpectedShutdownCrashCleanupRetryWindow
                ? ExpectedShutdownCrashCleanupRetryWindow
                : closeWait;
            RuntimeErrorBaseline.RemoveExpectedShutdownCrashes(
                errorDirectory,
                errorBaseline,
                runtimeErrorRetryWindow,
                ExpectedShutdownCrashInitialQuietWindow);
            return new StarCraftAiShutdownResult(
                ProcessWasRunning: true,
                LeaveSequenceSent: false,
                Exited: exited,
                LeaveConfirmed: exited,
                ForcedKillUsed: true);
        }
    }

    public static bool TryLeaveGame(int processId, TimeSpan leaveWait)
    {
        return TrySendLeaveGameSequence(processId, leaveWait).LeaveConfirmed;
    }

    public static void RemoveExpectedShutdownCrashes(string? errorDirectory, TimeSpan retryWindow)
    {
        RemoveExpectedShutdownCrashes(
            errorDirectory,
            retryWindow,
            ExpectedShutdownCrashInitialQuietWindow);
    }

    internal static void RemoveExpectedShutdownCrashes(
        string? errorDirectory,
        TimeSpan retryWindow,
        TimeSpan initialQuietWindow)
    {
        RuntimeErrorBaseline.RemoveExpectedShutdownCrashes(
            errorDirectory,
            RuntimeErrorBaseline.Empty,
            retryWindow,
            initialQuietWindow);
    }

    internal static bool IsLeaveCompleteState(StarCraftScreenState state)
    {
        return state is StarCraftScreenState.GameRoom or StarCraftScreenState.MenuLike;
    }

    internal static bool IsGracefulAiShutdown(
        StarCraftAiShutdownResult? shutdown,
        bool processGoneAfterCleanup,
        StarCraftScreenState playerStateAfterAiShutdown,
        bool runtimeErrorsClean)
    {
        return shutdown is not null &&
               shutdown.LeaveSequenceSent &&
               (shutdown.LeaveConfirmed || shutdown.Exited && !shutdown.ForcedKillUsed) &&
               processGoneAfterCleanup &&
               playerStateAfterAiShutdown != StarCraftScreenState.BlockedDialog &&
               runtimeErrorsClean;
    }

    internal static bool IsExpectedBwapiShutdownCrashText(string text)
    {
        return text.Contains("EXCEPTION: 0xE06D7363", StringComparison.OrdinalIgnoreCase) &&
               text.Contains("BWAPI", StringComparison.OrdinalIgnoreCase) &&
               text.Contains("DestroyGame", StringComparison.OrdinalIgnoreCase) &&
               text.Contains("preLoadGame", StringComparison.OrdinalIgnoreCase);
    }

    private static StarCraftLeaveGameResult TrySendLeaveGameSequence(int processId, TimeSpan leaveWait)
    {
        _ = StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(processId, TimeSpan.FromSeconds(2));
        if (!StarCraftBorderlessWindow.TryFindBroodWarWindow(processId, out var windowHandle))
        {
            return new StarCraftLeaveGameResult(false, false, ProcessStillRunning: IsProcessRunning(processId), StarCraftScreenState.Unknown);
        }

        var state = StarCraftScreenDetector.Detect(processId);
        if (!ShouldSendLeaveSequence(state))
        {
            return new StarCraftLeaveGameResult(false, IsLeaveCompleteState(state), ProcessStillRunning: IsProcessRunning(processId), state);
        }

        var sequenceSent = false;
        var lastState = state;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (!IsProcessRunning(processId))
            {
                return new StarCraftLeaveGameResult(sequenceSent, true, ProcessStillRunning: false, lastState);
            }

            if (IsLeaveCompleteState(lastState))
            {
                return new StarCraftLeaveGameResult(sequenceSent, true, ProcessStillRunning: true, lastState);
            }

            foreach (var key in LeaveGameSequence)
            {
                PostKey(windowHandle, key);
                sequenceSent = true;
                Thread.Sleep(key == StarCraftExitKey.F10 ? 450 : 650);
            }

            if (WaitForLeaveComplete(processId, leaveWait, out lastState, out var processStillRunning))
            {
                return new StarCraftLeaveGameResult(sequenceSent, true, processStillRunning, lastState);
            }
        }

        return new StarCraftLeaveGameResult(sequenceSent, false, ProcessStillRunning: IsProcessRunning(processId), lastState);
    }

    private static bool WaitForLeaveComplete(
        int processId,
        TimeSpan timeout,
        out StarCraftScreenState lastState,
        out bool processStillRunning)
    {
        var deadline = DateTime.UtcNow + timeout;
        var stableSamples = 0;
        lastState = StarCraftScreenState.Unknown;
        processStillRunning = true;
        while (DateTime.UtcNow < deadline)
        {
            if (!IsProcessRunning(processId))
            {
                processStillRunning = false;
                return true;
            }

            var currentState = StarCraftScreenDetector.Detect(processId);
            lastState = currentState;
            if (IsLeaveCompleteState(currentState))
            {
                stableSamples++;
                if (stableSamples >= 4)
                {
                    Thread.Sleep(2000);
                    return true;
                }
            }
            else
            {
                stableSamples = 0;
            }

            Thread.Sleep(300);
        }

        processStillRunning = IsProcessRunning(processId);
        return false;
    }

    private static bool TryGetRunningProcess(int processId, out Process process)
    {
        try
        {
            process = Process.GetProcessById(processId);
            if (HasExited(process))
            {
                process.Dispose();
                process = null!;
                return false;
            }

            return true;
        }
        catch
        {
            process = null!;
            return false;
        }
    }

    private static bool IsProcessRunning(int processId)
    {
        return TryGetRunningProcess(processId, out var process) && DisposeAndReturnRunning(process);
    }

    private static bool DisposeAndReturnRunning(Process process)
    {
        using (process)
        {
            return !HasExited(process);
        }
    }

    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static (bool Exited, bool ForcedKillUsed) CloseProcess(
        Process process,
        TimeSpan closeWait,
        bool allowForceKill)
    {
        try
        {
            if (process.CloseMainWindow() && process.WaitForExit((int)closeWait.TotalMilliseconds))
            {
                return (true, false);
            }
        }
        catch
        {
            // Fall through to a final kill only after the in-game leave attempt.
        }

        if (!allowForceKill)
        {
            return (!ProcessExists(process.Id), false);
        }

        if (TryKill(process, entireProcessTree: true, closeWait))
        {
            return (true, true);
        }

        if (TryKill(process, entireProcessTree: false, closeWait))
        {
            return (true, true);
        }

        return (!ProcessExists(process.Id), true);
    }

    private static bool TryKill(Process process, bool entireProcessTree, TimeSpan closeWait)
    {
        try
        {
            process.Refresh();
            if (HasExited(process))
            {
                return true;
            }

            process.Kill(entireProcessTree);
            if (process.WaitForExit((int)closeWait.TotalMilliseconds))
            {
                return true;
            }
        }
        catch
        {
            // Try the next shutdown path. Smoke/app cleanup will make one final local-runtime pass.
        }

        return !ProcessExists(process.Id);
    }

    private static bool WaitForProcessExit(int processId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (!ProcessExists(processId))
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return !ProcessExists(processId);
    }

    private static bool ProcessExists(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !HasExited(process);
        }
        catch
        {
            return false;
        }
    }

    private static void PostKey(IntPtr windowHandle, StarCraftExitKey key)
    {
        var virtualKey = key switch
        {
            StarCraftExitKey.F10 => VirtualKeyF10,
            StarCraftExitKey.Q => VirtualKeyQ,
            _ => (byte)0
        };

        if (virtualKey == 0)
        {
            return;
        }

        PostMessage(windowHandle, WindowMessageKeyDown, (UIntPtr)virtualKey, IntPtr.Zero);
        Thread.Sleep(40);
        PostMessage(windowHandle, WindowMessageKeyUp, (UIntPtr)virtualKey, IntPtr.Zero);
    }

    private const byte VirtualKeyF10 = 0x79;
    private const byte VirtualKeyQ = 0x51;
    private const uint WindowMessageKeyDown = 0x0100;
    private const uint WindowMessageKeyUp = 0x0101;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr windowHandle, uint message, UIntPtr wParam, IntPtr lParam);

    private sealed record RuntimeErrorBaseline(IReadOnlyDictionary<string, RuntimeErrorFileState> Files)
    {
        public static RuntimeErrorBaseline Empty { get; } =
            new(new Dictionary<string, RuntimeErrorFileState>(StringComparer.OrdinalIgnoreCase));

        public static RuntimeErrorBaseline Capture(string? errorDirectory)
        {
            if (string.IsNullOrWhiteSpace(errorDirectory) || !Directory.Exists(errorDirectory))
            {
                return Empty;
            }

            var files = Directory.EnumerateFiles(errorDirectory)
                .Where(IsRuntimeErrorFile)
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new KeyValuePair<string, RuntimeErrorFileState>(
                        info.FullName,
                        new RuntimeErrorFileState(info.Length, info.LastWriteTimeUtc));
                })
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            return new RuntimeErrorBaseline(files);
        }

        public static void RemoveExpectedShutdownCrashes(
            string? errorDirectory,
            RuntimeErrorBaseline baseline,
            TimeSpan retryWindow,
            TimeSpan initialQuietWindow)
        {
            if (string.IsNullOrWhiteSpace(errorDirectory) || !Directory.Exists(errorDirectory))
            {
                return;
            }

            var knownExpectedCrashTimes = new List<DateTime>();
            var deadline = DateTime.UtcNow + retryWindow;
            var lastActivity = DateTime.UtcNow;
            var quietWindow = initialQuietWindow;
            do
            {
                var cleanupActivity = RemoveExpectedShutdownCrashesOnce(errorDirectory, baseline, knownExpectedCrashTimes);
                if (retryWindow <= TimeSpan.Zero)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                if (cleanupActivity.HadCandidates || cleanupActivity.DeletedFiles > 0)
                {
                    lastActivity = now;
                    quietWindow = ExpectedShutdownCrashPostActivityQuietWindow;
                }

                if (now - lastActivity >= quietWindow)
                {
                    return;
                }

                Thread.Sleep(250);
            }
            while (DateTime.UtcNow < deadline);

            RemoveExpectedShutdownCrashesOnce(errorDirectory, baseline, knownExpectedCrashTimes);
        }

        private static RuntimeErrorCleanupActivity RemoveExpectedShutdownCrashesOnce(
            string errorDirectory,
            RuntimeErrorBaseline baseline,
            List<DateTime> knownExpectedCrashTimes)
        {
            var candidates = Directory.EnumerateFiles(errorDirectory)
                .Where(IsRuntimeErrorFile)
                .Select(path => new RuntimeErrorCandidate(path, new FileInfo(path)))
                .Where(candidate => !baseline.Files.TryGetValue(candidate.Path, out var previous) ||
                                    previous.Length != candidate.Length ||
                                    previous.LastWriteTimeUtc != candidate.LastWriteTimeUtc)
                .ToArray();

            var expectedTextCrashes = candidates
                .Where(candidate => candidate.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                .Where(candidate => IsExpectedShutdownCrash(candidate.Path))
                .ToArray();
            if (expectedTextCrashes.Length == 0)
            {
                if (knownExpectedCrashTimes.Count == 0)
                {
                    return new RuntimeErrorCleanupActivity(candidates.Length > 0, 0);
                }
            }

            foreach (var crash in expectedTextCrashes)
            {
                if (!knownExpectedCrashTimes.Any(time => IsWithinExpectedCrashTime(time, crash.LastWriteTimeUtc)))
                {
                    knownExpectedCrashTimes.Add(crash.LastWriteTimeUtc);
                }
            }

            var expectedCrashTimes = knownExpectedCrashTimes.Concat(expectedTextCrashes.Select(crash => crash.LastWriteTimeUtc)).ToArray();
            var deletedFiles = 0;
            foreach (var candidate in candidates.Where(candidate => IsExpectedShutdownCrashCompanionErr(candidate, expectedCrashTimes)))
            {
                if (TryDelete(candidate.Path))
                {
                    deletedFiles++;
                }
            }

            foreach (var candidate in expectedTextCrashes)
            {
                if (TryDelete(candidate.Path))
                {
                    deletedFiles++;
                }
            }

            return new RuntimeErrorCleanupActivity(candidates.Length > 0, deletedFiles);
        }

        private static bool IsRuntimeErrorFile(string path)
        {
            return Path.GetExtension(path) is ".txt" or ".ERR" or ".err";
        }

        private static bool IsExpectedShutdownCrash(string path)
        {
            try
            {
                var text = File.ReadAllText(path);
                return IsExpectedBwapiShutdownCrashText(text);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsExpectedShutdownCrashCompanionErr(
            RuntimeErrorCandidate candidate,
            IReadOnlyList<DateTime> expectedCrashTimes)
        {
            if (!candidate.Extension.Equals(".ERR", StringComparison.OrdinalIgnoreCase) &&
                !candidate.Extension.Equals(".err", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!expectedCrashTimes.Any(time => IsWithinExpectedCrashTime(time, candidate.LastWriteTimeUtc)))
            {
                return false;
            }

            try
            {
                var text = File.ReadAllText(candidate.Path);
                return text.Contains("Exception code: E06D7363", StringComparison.OrdinalIgnoreCase) &&
                       text.Contains("KERNELBASE.dll", StringComparison.OrdinalIgnoreCase) &&
                       text.Contains("BWAPI.dll", StringComparison.OrdinalIgnoreCase) &&
                       text.Contains("StarCraft.exe", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWithinExpectedCrashTime(DateTime left, DateTime right)
        {
            return Math.Abs((left - right).TotalSeconds) <= 5;
        }

        private static bool TryDelete(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch
            {
                // Cleanup should never block shutdown. Smoke/audit will catch files that remain.
                return false;
            }
        }
    }

    private sealed record RuntimeErrorFileState(long Length, DateTime LastWriteTimeUtc);

    private sealed record RuntimeErrorCleanupActivity(bool HadCandidates, int DeletedFiles);

    private sealed record RuntimeErrorCandidate(
        string Path,
        long Length,
        DateTime LastWriteTimeUtc,
        string Extension)
    {
        public RuntimeErrorCandidate(string path, FileInfo info)
            : this(info.FullName, info.Length, info.LastWriteTimeUtc, System.IO.Path.GetExtension(path))
        {
        }
    }
}
