using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace StarAI.PracticeClient.Core;

[SupportedOSPlatform("windows")]
internal static class ChaosLauncherWindowAutomation
{
    private const int BM_CLICK = 0x00F5;

    public static bool ClickStart(Process process, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            foreach (var window in CandidateWindows(process))
            {
                var button = FindChildByText(window, "Start");
                if (button != IntPtr.Zero)
                {
                    SendMessage(button, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }
            }

            Thread.Sleep(200);
        }

        return false;
    }

    private static IEnumerable<IntPtr> CandidateWindows(Process process)
    {
        process.Refresh();
        if (process.MainWindowHandle != IntPtr.Zero)
        {
            yield return process.MainWindowHandle;
        }

        foreach (var window in TopLevelWindows())
        {
            GetWindowThreadProcessId(window, out var pid);
            if (pid == process.Id)
            {
                yield return window;
            }
        }
    }

    private static IntPtr FindChildByText(IntPtr parent, string text)
    {
        var found = IntPtr.Zero;
        EnumChildWindows(parent, (handle, _) =>
        {
            if (string.Equals(GetWindowText(handle), text, StringComparison.OrdinalIgnoreCase))
            {
                found = handle;
                return false;
            }

            var nested = FindChildByText(handle, text);
            if (nested != IntPtr.Zero)
            {
                found = nested;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return found;
    }

    private static IReadOnlyList<IntPtr> TopLevelWindows()
    {
        var windows = new List<IntPtr>();
        EnumWindows((handle, _) =>
        {
            if (IsWindowVisible(handle))
            {
                windows.Add(handle);
            }

            return true;
        }, IntPtr.Zero);
        return windows;
    }

    private static string GetWindowText(IntPtr handle)
    {
        var length = GetWindowTextLength(handle);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr parentHandle, EnumWindowsProc enumProc, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr handle);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
}
