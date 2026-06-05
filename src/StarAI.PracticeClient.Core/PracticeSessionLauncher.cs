using System.Diagnostics;
using System.Runtime.Versioning;

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
        var starCraftProcessId = WaitForNewStarCraftProcess(beforeStarCraftProcessIds, TimeSpan.FromSeconds(3));

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
        while (DateTime.UtcNow < deadline)
        {
            var current = CurrentStarCraftProcessIds()
                .Where(id => !beforeProcessIds.Contains(id))
                .OrderBy(id => id)
                .ToList();
            if (current.Count > 0)
            {
                return current[0];
            }

            Thread.Sleep(100);
        }

        return null;
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

    public static bool IsLocalRuntimeProcess(string processName, string? executablePath, IReadOnlyCollection<string> runtimeRoots)
    {
        if (!IsTargetProcessName(processName) || string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        return runtimeRoots.Any(root => RuntimeWritePolicy.IsSameOrUnder(executablePath, root));
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
            if (process.HasExited)
            {
                return false;
            }

            if (process.CloseMainWindow() && process.WaitForExit(1500))
            {
                return true;
            }

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
