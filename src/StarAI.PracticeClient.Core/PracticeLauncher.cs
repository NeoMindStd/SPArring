using System.Diagnostics;
using System.Runtime.Versioning;

namespace StarAI.PracticeClient.Core;

public sealed class PracticeLauncher
{
    private readonly ChaosLauncherConfigurator _chaos = new();

    [SupportedOSPlatform("windows")]
    public Process LaunchChaos(string starCraftRoot, ChaosLaunchMode mode, bool clickStart = false, bool closeLauncherAfterStart = false)
    {
        var process = OpenChaos(starCraftRoot, mode);

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
    public Process OpenChaos(string starCraftRoot, ChaosLaunchMode mode)
    {
        _chaos.Apply(mode);

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

        return process;
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
