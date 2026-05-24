using System.Diagnostics;
using System.Runtime.Versioning;

namespace StarAI.PracticeClient.Core;

public sealed class PracticeLauncher
{
    private readonly ChaosLauncherConfigurator _chaos = new();

    [SupportedOSPlatform("windows")]
    public Process LaunchChaos(string starCraftRoot, ChaosLaunchMode mode, bool clickStart = false, bool closeLauncherAfterStart = false)
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

        if (clickStart && !ChaosLauncherWindowAutomation.ClickStart(process, TimeSpan.FromSeconds(10)))
        {
            throw new InvalidOperationException("ChaosLauncher Start button could not be clicked automatically.");
        }

        if (clickStart && closeLauncherAfterStart)
        {
            CloseLauncherWindow(process);
        }

        return process;
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
