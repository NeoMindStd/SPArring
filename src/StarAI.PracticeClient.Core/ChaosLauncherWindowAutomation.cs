using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace StarAI.PracticeClient.Core;

[SupportedOSPlatform("windows")]
internal static class ChaosLauncherWindowAutomation
{
    private const int BM_CLICK = 0x00F5;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int MK_LBUTTON = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private static readonly object PhysicalClickLock = new();

    public static bool ClickStart(Process process, string starCraftRoot, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            foreach (var window in CandidateWindows(process, starCraftRoot))
            {
                var button = FindChildByText(window, "Start");
                if (button != IntPtr.Zero)
                {
                    SetForegroundWindow(window);
                    if (ClickButtonByMessage(button))
                    {
                        return true;
                    }

                    SendMessage(button, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    Thread.Sleep(150);
                    if (ClickCenter(button))
                    {
                        return true;
                    }
                }

                if (ClickApproximateStart(window))
                {
                    return true;
                }
            }

            Thread.Sleep(200);
        }

        return false;
    }

    private static IEnumerable<IntPtr> CandidateWindows(Process process, string starCraftRoot)
    {
        process.Refresh();
        var processWindows = new List<IntPtr>();
        if (process.MainWindowHandle != IntPtr.Zero && IsWindowVisible(process.MainWindowHandle))
        {
            processWindows.Add(process.MainWindowHandle);
        }

        foreach (var window in TopLevelWindows())
        {
            GetWindowThreadProcessId(window, out var pid);
            if (pid == process.Id)
            {
                processWindows.Add(window);
            }
        }

        foreach (var window in processWindows.Distinct())
        {
            yield return window;
        }

        if (processWindows.Count > 0)
        {
            yield break;
        }

        var launcherPath = Path.GetFullPath(Path.Combine(starCraftRoot, "Chaoslauncher - MultiInstance.exe"));
        foreach (var launcherProcess in Process.GetProcessesByName("Chaoslauncher - MultiInstance"))
        {
            using (launcherProcess)
            {
                if (!IsProcessExecutable(launcherProcess, launcherPath))
                {
                    continue;
                }

                foreach (var window in TopLevelWindows())
                {
                    GetWindowThreadProcessId(window, out var pid);
                    if (pid == launcherProcess.Id)
                    {
                        yield return window;
                    }
                }
            }
        }

        if (processWindows.Count > 0)
        {
            yield break;
        }

        var chaosWindows = TopLevelWindows()
            .Where(window =>
            {
                var title = GetWindowText(window);
                return title.Contains("Chaoslauncher", StringComparison.OrdinalIgnoreCase) ||
                       title.Contains("ChaosLauncher", StringComparison.OrdinalIgnoreCase);
            })
            .Distinct()
            .ToArray();

        if (chaosWindows.Length == 1)
        {
            yield return chaosWindows[0];
        }
    }

    private static bool IsProcessExecutable(Process process, string expectedPath)
    {
        try
        {
            return string.Equals(Path.GetFullPath(process.MainModule?.FileName ?? ""), expectedPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static IntPtr FindChildByText(IntPtr parent, string text)
    {
        var found = IntPtr.Zero;
        EnumChildWindows(parent, (handle, _) =>
        {
            var windowText = GetWindowText(handle).Replace("&", "", StringComparison.Ordinal);
            if (windowText.Contains(text, StringComparison.OrdinalIgnoreCase))
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

    private static bool ClickApproximateStart(IntPtr window)
    {
        if (!GetWindowRect(window, out var rect))
        {
            return false;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width < 200 || height < 120)
        {
            return false;
        }

        SetForegroundWindow(window);
        lock (PhysicalClickLock)
        {
            Thread.Sleep(80);
            SetCursorPos(rect.Left + 45, rect.Bottom - 24);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        return true;
    }

    private static bool ClickButtonByMessage(IntPtr button)
    {
        if (!GetClientRect(button, out var rect))
        {
            return false;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var lParam = MakeLParam(width / 2, height / 2);
        SendMessage(button, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
        SendMessage(button, WM_LBUTTONUP, IntPtr.Zero, lParam);
        Thread.Sleep(150);
        return true;
    }

    private static bool ClickCenter(IntPtr window)
    {
        if (!GetWindowRect(window, out var rect))
        {
            return false;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        lock (PhysicalClickLock)
        {
            SetCursorPos(rect.Left + width / 2, rect.Top + height / 2);
            Thread.Sleep(80);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        return true;
    }

    private static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

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

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
