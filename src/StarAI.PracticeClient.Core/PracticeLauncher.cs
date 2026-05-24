using System.Diagnostics;
using System.Runtime.Versioning;

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
        var process = OpenChaos(starCraftRoot, mode, runStarCraftOnStartup: true);
        WaitForCompletedStart(starCraftRoot, before, TimeSpan.FromSeconds(20));
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
        _chaos.SetRunStarCraftOnStartup(starCraftRoot, enabled: false);
        ClickStart(process, starCraftRoot, timeout);
        WaitForCompletedStart(starCraftRoot, before, timeout);
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
        Thread.Sleep(2000);
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

            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The StarCraft process is already created by this point; a stale launcher
            // window should not block the next launcher instance.
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

    private static void WaitForCompletedStart(string starCraftRoot, int previousCount, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (CountCompletedStarts(starCraftRoot) > previousCount)
            {
                return;
            }

            Thread.Sleep(200);
        }

        throw new InvalidOperationException("StarCraft launch was requested, but ChaosLauncher did not report a completed start.");
    }
}
