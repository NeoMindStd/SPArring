using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarAI.PracticeClient.App;

internal enum StarCraftExitKey
{
    F10,
    Q
}

internal sealed record StarCraftAiShutdownResult(
    bool ProcessWasRunning,
    bool LeaveSequenceSent,
    bool Exited);

internal static class StarCraftGameExitController
{
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
            return new StarCraftAiShutdownResult(false, false, true);
        }

        using (process)
        {
            var leaveSent = TrySendLeaveGameSequence(processId, leaveWait);
            var exited = CloseProcess(process, closeWait);
            return new StarCraftAiShutdownResult(
                ProcessWasRunning: true,
                LeaveSequenceSent: leaveSent,
                Exited: exited);
        }
    }

    private static bool TrySendLeaveGameSequence(int processId, TimeSpan leaveWait)
    {
        if (!StarCraftBorderlessWindow.TryFindBroodWarWindow(processId, out var windowHandle))
        {
            return false;
        }

        var state = StarCraftScreenDetector.Detect(processId);
        if (!ShouldSendLeaveSequence(state))
        {
            return false;
        }

        foreach (var key in LeaveGameSequence)
        {
            PostKey(windowHandle, key);
            Thread.Sleep(250);
        }

        var deadline = DateTime.UtcNow + leaveWait;
        while (DateTime.UtcNow < deadline && IsProcessRunning(processId))
        {
            var currentState = StarCraftScreenDetector.Detect(processId);
            if (currentState != StarCraftScreenState.InGame &&
                currentState != StarCraftScreenState.PreGameWait)
            {
                return true;
            }

            Thread.Sleep(250);
        }

        return true;
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

    private static bool CloseProcess(Process process, TimeSpan closeWait)
    {
        try
        {
            if (process.CloseMainWindow() && process.WaitForExit((int)closeWait.TotalMilliseconds))
            {
                return true;
            }
        }
        catch
        {
            // Fall through to a final kill only after the in-game leave attempt.
        }

        if (TryKill(process, entireProcessTree: true, closeWait))
        {
            return true;
        }

        if (TryKill(process, entireProcessTree: false, closeWait))
        {
            return true;
        }

        return !ProcessExists(process.Id);
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
}
