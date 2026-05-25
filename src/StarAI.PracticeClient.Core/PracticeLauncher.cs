using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace StarAI.PracticeClient.Core;

public sealed class PracticeLauncher
{
    private static readonly object ChaosStartupLock = new();
    private readonly ChaosLauncherConfigurator _chaos = new();

    [SupportedOSPlatform("windows")]
    public Process LaunchChaos(string starCraftRoot, ChaosLaunchMode mode, bool clickStart = false, bool closeLauncherAfterStart = false)
    {
        var process = OpenChaos(starCraftRoot, mode, runStarCraftOnStartup: false);

        if (clickStart)
        {
            ClickStart(process, TimeSpan.FromSeconds(20));
        }

        if (clickStart && closeLauncherAfterStart)
        {
            CloseLauncherWindow(process);
        }

        return process;
    }

    [SupportedOSPlatform("windows")]
    public int StopExistingLocalRuntime(string starCraftRoot)
    {
        var stopped = 0;
        var root = Path.GetFullPath(starCraftRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var launcherPath = Path.GetFullPath(Path.Combine(root, "Chaoslauncher - MultiInstance.exe"));
        var starCraftPath = Path.GetFullPath(Path.Combine(root, "StarCraft.exe"));

        StopLocalRuntimeViaPowerShell(root);

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var matchedThisPass = 0;
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    if (IsMatchingProcess(process, "Chaoslauncher - MultiInstance", launcherPath) ||
                        IsMatchingProcess(process, "StarCraft", starCraftPath, allowUnknownPath: true))
                    {
                        matchedThisPass++;
                        if (StopProcess(process))
                        {
                            stopped++;
                        }
                    }
                }
            }

            if (matchedThisPass == 0)
            {
                return stopped;
            }

            Thread.Sleep(250);
        }

        StopLocalRuntimeViaPowerShell(root);
        return stopped;
    }

    [SupportedOSPlatform("windows")]
    public Process OpenChaos(string starCraftRoot, ChaosLaunchMode mode, bool runStarCraftOnStartup = false)
    {
        lock (ChaosStartupLock)
        {
            _chaos.Apply(mode, starCraftRoot, runStarCraftOnStartup);

            var launcher = Path.Combine(starCraftRoot, "Chaoslauncher - MultiInstance.exe");
            if (!File.Exists(launcher))
            {
                throw new FileNotFoundException("ChaosLauncher MultiInstance executable not found.", launcher);
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = launcher,
                WorkingDirectory = starCraftRoot,
                UseShellExecute = true
            }) ?? throw new InvalidOperationException("Failed to start ChaosLauncher.");

            // ChaosLauncher reads the global StarCraft install path during early startup.
            // Keep the registry stable briefly before another launcher instance changes it.
            Thread.Sleep(runStarCraftOnStartup ? 700 : 250);
            return process;
        }
    }

    [SupportedOSPlatform("windows")]
    public Process OpenChaosAndStartStarCraft(string starCraftRoot, ChaosLaunchMode mode)
    {
        var before = CountCompletedStarts(starCraftRoot);
        var requestedAt = DateTime.UtcNow;
        var process = OpenChaos(starCraftRoot, mode, runStarCraftOnStartup: true);
        WaitForCompletedStart(starCraftRoot, before, TimeSpan.FromSeconds(20), requestedAt);
        return process;
    }

    [SupportedOSPlatform("windows")]
    public void DisableStartupLaunch(string starCraftRoot)
    {
        _chaos.SetRunStarCraftOnStartup(starCraftRoot, enabled: false);
    }

    [SupportedOSPlatform("windows")]
    public void StartAdditionalStarCraft(Process process, string starCraftRoot, TimeSpan timeout)
    {
        var before = CountCompletedStarts(starCraftRoot);
        var requestedAt = DateTime.UtcNow;
        _chaos.SetRunStarCraftOnStartup(starCraftRoot, enabled: false);
        ClickStart(process, starCraftRoot, timeout);
        WaitForCompletedStart(starCraftRoot, before, timeout, requestedAt);
    }

    [SupportedOSPlatform("windows")]
    public void ClickStart(Process process, TimeSpan timeout)
    {
        var starCraftRoot = process.StartInfo.WorkingDirectory;
        if (string.IsNullOrWhiteSpace(starCraftRoot))
        {
            starCraftRoot = Path.GetDirectoryName(process.StartInfo.FileName) ?? "";
        }

        ClickStart(process, starCraftRoot, timeout);
    }

    [SupportedOSPlatform("windows")]
    public void ClickStart(Process process, string starCraftRoot, TimeSpan timeout)
    {
        if (!ChaosLauncherWindowAutomation.ClickStart(process, starCraftRoot, timeout))
        {
            throw new InvalidOperationException("ChaosLauncher Start button could not be clicked automatically.");
        }
    }

    private static void CloseLauncherWindow(Process process)
    {
        Thread.Sleep(300);
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
            // The StarCraft process is already created by this point; a stale launcher
            // window should not block the next launcher instance.
        }
    }

    private static bool IsMatchingProcess(Process process, string processName, string expectedPath, bool allowUnknownPath = false)
    {
        if (!string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var actualPath = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(actualPath))
            {
                return string.Equals(Path.GetFullPath(actualPath), expectedPath, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            return allowUnknownPath;
        }

        return allowUnknownPath;
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

    private static void StopLocalRuntimeViaPowerShell(string starCraftRoot)
    {
        var escapedRoot = starCraftRoot.Replace("'", "''", StringComparison.Ordinal);
        var script = $$"""
            $root = '{{escapedRoot}}'
            $launcher = Join-Path $root 'Chaoslauncher - MultiInstance.exe'
            Get-CimInstance Win32_Process | Where-Object {
                ($_.Name -eq 'Chaoslauncher - MultiInstance.exe' -and $_.ExecutablePath -eq $launcher) -or
                ($_.Name -eq 'StarCraft.exe' -and ($_.ExecutablePath -like "$root*" -or [string]::IsNullOrWhiteSpace($_.ExecutablePath)))
            } | ForEach-Object {
                Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
            }
            """;
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList =
                {
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-EncodedCommand",
                    encoded
                }
            });
            process?.WaitForExit(5000);
        }
        catch
        {
            // Best-effort cleanup; the caller still validates the actual launch.
        }
    }

    private static int CountCompletedStarts(string starCraftRoot)
    {
        var log = Path.Combine(starCraftRoot, "Chaoslauncher - MultiInstance.log");
        if (!File.Exists(log))
        {
            return 0;
        }

        try
        {
            return File.ReadLines(log)
                .Count(line => line.Contains("Starting Starcraft completed", StringComparison.OrdinalIgnoreCase));
        }
        catch (IOException)
        {
            return 0;
        }
    }

    private static void WaitForCompletedStart(string starCraftRoot, int previousCount, TimeSpan timeout, DateTime requestedAtUtc)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var currentCount = CountCompletedStarts(starCraftRoot);
            if (currentCount > previousCount || LogWasRewrittenForThisRequest(starCraftRoot, currentCount, requestedAtUtc))
            {
                return;
            }

            Thread.Sleep(200);
        }

        throw new InvalidOperationException("StarCraft launch was requested, but ChaosLauncher did not report a completed start.");
    }

    private static bool LogWasRewrittenForThisRequest(string starCraftRoot, int currentCount, DateTime requestedAtUtc)
    {
        if (currentCount <= 0)
        {
            return false;
        }

        var log = Path.Combine(starCraftRoot, "Chaoslauncher - MultiInstance.log");
        try
        {
            return File.GetLastWriteTimeUtc(log) >= requestedAtUtc.AddSeconds(-1);
        }
        catch (IOException)
        {
            return false;
        }
    }
}
