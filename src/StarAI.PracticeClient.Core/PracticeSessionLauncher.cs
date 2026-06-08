using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace StarAI.PracticeClient.Core;

public sealed record PracticeSessionLaunchOptions(
    TimeSpan StarCraftStartupTimeout,
    TimeSpan AiLaunchDelay,
    bool StopExistingLocalRuntime,
    Action<ClientLaunchSettings>? AfterClientStarted = null)
{
    public static PracticeSessionLaunchOptions Defaults()
    {
        return new PracticeSessionLaunchOptions(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(3), true);
    }
}

public sealed record PracticeClientLaunchReport(
    ClientRuntimeRole Role,
    string RuntimeRoot,
    int ChaosLauncherProcessId,
    int? StarCraftProcessId,
    int PreviousCompletedStartCount,
    int CompletedStartCount);

public sealed record PracticeSessionLaunchReport(
    PracticeClientLaunchReport Player,
    PracticeClientLaunchReport Ai,
    int StoppedLocalProcesses);

public sealed class PracticeSessionLauncher
{
    private readonly ChaosLauncherClient _chaos;
    private readonly LocalRuntimeProcessCleaner _cleaner;

    public PracticeSessionLauncher()
        : this(new ChaosLauncherClient(), new LocalRuntimeProcessCleaner())
    {
    }

    public PracticeSessionLauncher(ChaosLauncherClient chaos, LocalRuntimeProcessCleaner cleaner)
    {
        _chaos = chaos;
        _cleaner = cleaner;
    }

    [SupportedOSPlatform("windows")]
    public PracticeSessionLaunchReport Launch(
        PracticeLaunchPlan plan,
        PracticeRuntimeOptions runtimeOptions,
        PracticeSessionLaunchOptions launchOptions)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Practice session launch is only supported on Windows.");
        }

        var prepared = RuntimeProvisioner.PrepareRuntimeAssets(plan);
        PracticeRuntimeConfigurator.Apply(prepared, runtimeOptions);

        var stopped = launchOptions.StopExistingLocalRuntime
            ? _cleaner.Stop(prepared.Player.RuntimeRoot, prepared.Ai.RuntimeRoot)
            : 0;

        var playerReport = LaunchClient(prepared.Player, launchOptions.StarCraftStartupTimeout);
        launchOptions.AfterClientStarted?.Invoke(prepared.Player);
        Thread.Sleep(launchOptions.AiLaunchDelay);
        var aiReport = LaunchClient(prepared.Ai, launchOptions.StarCraftStartupTimeout);
        launchOptions.AfterClientStarted?.Invoke(prepared.Ai);

        return new PracticeSessionLaunchReport(playerReport, aiReport, stopped);
    }

    [SupportedOSPlatform("windows")]
    private PracticeClientLaunchReport LaunchClient(ClientLaunchSettings settings, TimeSpan timeout)
    {
        var beforeStarCraftProcessIds = CurrentStarCraftProcessIds();
        var before = ChaosLauncherLog.CountCompletedStarts(settings.RuntimeRoot);
        var requestedAt = DateTime.UtcNow;
        using var tournamentEnvironment = TournamentModuleEnvironment.ApplyFor(settings.Role);
        var run = _chaos.Start(new ChaosLauncherRequest(
            settings.RuntimeRoot,
            RunStarCraftOnStartup: true,
            EnableWMode: settings.EnableWModePlugin,
            EnableBwapi: true,
            EnableApmAlert: settings.ApmAlertEnabled));

        try
        {
            ChaosLauncherLog.WaitForCompletedStart(settings.RuntimeRoot, before, timeout, requestedAt);
            CloseLauncherWindow(run.Process);
        }
        finally
        {
            run.RestorePoint.Restore();
        }
        var processDetectionTimeout = TimeSpan.FromSeconds(Math.Min(15, Math.Max(5, timeout.TotalSeconds / 2)));
        var starCraftProcessId = WaitForNewStarCraftProcess(beforeStarCraftProcessIds, processDetectionTimeout);

        return new PracticeClientLaunchReport(
            settings.Role,
            settings.RuntimeRoot,
            run.Process.Id,
            starCraftProcessId,
            before,
            ChaosLauncherLog.CountCompletedStarts(settings.RuntimeRoot));
    }

    private static HashSet<int> CurrentStarCraftProcessIds()
    {
        return Process.GetProcessesByName("StarCraft")
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .ToHashSet();
    }

    private static int? WaitForNewStarCraftProcess(IReadOnlySet<int> beforeProcessIds, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        int? latestProcessCandidate = null;
        while (DateTime.UtcNow < deadline)
        {
            var windowProcessId = FindNewBroodWarWindowProcessId(beforeProcessIds);
            if (windowProcessId is not null)
            {
                return windowProcessId;
            }

            var current = CurrentStarCraftProcessIds()
                .Where(id => !beforeProcessIds.Contains(id))
                .OrderBy(id => id)
                .ToList();
            if (current.Count > 0)
            {
                latestProcessCandidate = current[0];
            }

            Thread.Sleep(100);
        }

        return latestProcessCandidate;
    }

    private static int? FindNewBroodWarWindowProcessId(IReadOnlySet<int> beforeProcessIds)
    {
        int? result = null;
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle))
            {
                return true;
            }

            var title = new StringBuilder(256);
            GetWindowText(handle, title, title.Capacity);
            if (!IsBroodWarWindowTitle(title.ToString()))
            {
                return true;
            }

            GetWindowThreadProcessId(handle, out var processId);
            if (beforeProcessIds.Contains(processId))
            {
                return true;
            }

            result = processId;
            return false;
        }, IntPtr.Zero);

        return result;
    }

    internal static bool IsBroodWarWindowTitle(string title)
    {
        return title.Equals("Brood War", StringComparison.OrdinalIgnoreCase) ||
               title.StartsWith("Brood War Instance ", StringComparison.OrdinalIgnoreCase);
    }

    private static void CloseLauncherWindow(Process process)
    {
        try
        {
            process.Refresh();
            if (process.HasExited)
            {
                return;
            }

            if (process.CloseMainWindow() && process.WaitForExit(2000))
            {
                return;
            }

            process.Kill(entireProcessTree: false);
        }
        catch
        {
            // A stale ChaosLauncher UI should not block the already-started StarCraft process.
        }
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);
}

public sealed class LocalRuntimeProcessCleaner
{
    public int Stop(params string[] runtimeRoots)
    {
        var stopped = 0;
        var normalizedRoots = runtimeRoots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                var executablePath = TryGetExecutablePath(process);
                if (!IsLocalRuntimeProcess(process.ProcessName, executablePath, normalizedRoots))
                {
                    continue;
                }

                if (StopProcess(process))
                {
                    stopped++;
                }
            }
        }

        return stopped;
    }

    public int StopKnown(params int?[] processIds)
    {
        var stopped = 0;
        var knownProcessIds = processIds
            .Where(processId => processId is not null)
            .Select(processId => processId!.Value)
            .Distinct()
            .ToArray();

        foreach (var processId in knownProcessIds)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                if (!IsKnownLaunchedRuntimeProcess(process.ProcessName, process.Id, knownProcessIds))
                {
                    continue;
                }

                if (StopProcess(process))
                {
                    stopped++;
                }
            }
            catch
            {
                // The captured process may already have exited by the time cleanup runs.
            }
        }

        return stopped;
    }

    public static bool IsLocalRuntimeProcess(string processName, string? executablePath, IReadOnlyCollection<string> runtimeRoots)
    {
        if (!IsTargetProcessName(processName) || string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        return runtimeRoots.Any(root => RuntimeWritePolicy.IsSameOrUnder(executablePath, root));
    }

    public static bool IsKnownLaunchedRuntimeProcess(
        string processName,
        int processId,
        IReadOnlyCollection<int> knownProcessIds)
    {
        return IsTargetProcessName(processName) && knownProcessIds.Contains(processId);
    }

    private static bool IsTargetProcessName(string processName)
    {
        return string.Equals(processName, "StarCraft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(processName, "Chaoslauncher - MultiInstance", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static bool StopProcess(Process process)
    {
        try
        {
            process.Refresh();
        }
        catch
        {
            // Continue to a direct close/kill attempt; some 1.16.1 processes deny metadata access.
        }

        try
        {
            if (process.HasExited)
            {
                return false;
            }
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch
        {
            // If the process exists but denies HasExited, still try to close the captured local PID.
        }

        try
        {
            if (process.CloseMainWindow() && process.WaitForExit(1500))
            {
                return true;
            }
        }
        catch
        {
            // Fall through to Kill.
        }

        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(3000);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
