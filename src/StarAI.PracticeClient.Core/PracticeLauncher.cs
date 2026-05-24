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
        return OpenChaos(starCraftRoot, mode, runStarCraftOnStartup: true);
    }

    [SupportedOSPlatform("windows")]
    public void DisableStartupLaunch(string starCraftRoot)
    {
        _chaos.Apply(ChaosLaunchMode.Human, starCraftRoot, runStarCraftOnStartup: false);
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
}
