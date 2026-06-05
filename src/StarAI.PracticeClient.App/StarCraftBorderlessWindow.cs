using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarAI.PracticeClient.App;

internal static class StarCraftBorderlessWindow
{
    private const int GwlStyle = -16;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsMinimize = 0x20000000L;
    private const long WsMaximize = 0x01000000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsPopup = 0x80000000L;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;

    public static HashSet<int> CurrentStarCraftProcessIds()
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

    public static BorderlessApplyResult ApplyWhenReady(
        string starCraftRoot,
        Rectangle targetBounds,
        TimeSpan timeout,
        IReadOnlySet<int> excludedProcessIds)
    {
        var expectedExe = Path.GetFullPath(Path.Combine(starCraftRoot, "StarCraft.exe"));
        var deadline = DateTime.UtcNow + timeout;
        var stableMatches = 0;
        while (DateTime.UtcNow < deadline)
        {
            var processId = TryApply(expectedExe, targetBounds, excludedProcessIds);
            if (processId is not null)
            {
                stableMatches++;
                if (stableMatches >= 4)
                {
                    return new BorderlessApplyResult(true, processId);
                }
            }
            else
            {
                stableMatches = 0;
            }

            Thread.Sleep(200);
        }

        return new BorderlessApplyResult(false, null);
    }

    public static BorderlessApplyResult ApplyToProcessWhenReady(
        int processId,
        Rectangle targetBounds,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var stableMatches = 0;
        while (DateTime.UtcNow < deadline)
        {
            if (EnsureProcessBorderless(processId, targetBounds).Applied)
            {
                stableMatches++;
                if (stableMatches >= 4)
                {
                    return new BorderlessApplyResult(true, processId);
                }
            }
            else
            {
                stableMatches = 0;
            }

            Thread.Sleep(200);
        }

        return new BorderlessApplyResult(false, processId);
    }

    public static BorderlessApplyResult EnsureProcessBorderless(int processId, Rectangle targetBounds)
    {
        var foundWindow = false;
        var applied = false;
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || !IsBroodWarWindow(handle))
            {
                return true;
            }

            GetWindowThreadProcessId(handle, out var currentProcessId);
            if (currentProcessId != processId)
            {
                return true;
            }

            foundWindow = true;
            if (!WindowMatches(handle, targetBounds))
            {
                ApplyBorderless(handle, targetBounds);
            }

            applied = WindowMatches(handle, targetBounds);
            return false;
        }, IntPtr.Zero);

        return new BorderlessApplyResult(applied, foundWindow ? processId : null);
    }

    public static bool MinimizeProcessWindowWhenReady(int processId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var minimized = false;
            EnumWindows((handle, _) =>
            {
                if (!IsWindowVisible(handle) || !IsBroodWarWindow(handle))
                {
                    return true;
                }

                GetWindowThreadProcessId(handle, out var currentProcessId);
                if (currentProcessId != processId)
                {
                    return true;
                }

                ShowWindow(handle, ShowMinimize);
                minimized = true;
                return false;
            }, IntPtr.Zero);

            if (minimized)
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return false;
    }

    private static int? TryApply(string expectedExe, Rectangle targetBounds, IReadOnlySet<int> excludedProcessIds)
    {
        int? appliedProcessId = null;
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle))
            {
                return true;
            }

            GetWindowThreadProcessId(handle, out var processId);
            if (!IsExpectedStarCraftProcess(processId, expectedExe, excludedProcessIds))
            {
                return true;
            }

            ApplyBorderless(handle, targetBounds);
            if (WindowMatches(handle, targetBounds))
            {
                appliedProcessId = processId;
            }
            return false;
        }, IntPtr.Zero);

        return appliedProcessId;
    }

    private static bool WindowMatches(IntPtr handle, Rectangle bounds)
    {
        if (!GetWindowRect(handle, out var rect))
        {
            return false;
        }

        return Math.Abs(rect.Left - bounds.Left) <= 2 &&
               Math.Abs(rect.Top - bounds.Top) <= 2 &&
               Math.Abs((rect.Right - rect.Left) - bounds.Width) <= 2 &&
               Math.Abs((rect.Bottom - rect.Top) - bounds.Height) <= 2;
    }

    private static bool IsExpectedStarCraftProcess(int processId, string expectedExe, IReadOnlySet<int> excludedProcessIds)
    {
        if (excludedProcessIds.Contains(processId))
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.ProcessName.Equals("StarCraft", StringComparison.OrdinalIgnoreCase) &&
                !process.ProcessName.Equals("Brood War", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var actualPath = process.MainModule?.FileName;
            return string.IsNullOrWhiteSpace(actualPath) ||
                   string.Equals(Path.GetFullPath(actualPath), expectedExe, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    private static void ApplyBorderless(IntPtr handle, Rectangle bounds)
    {
        ShowWindow(handle, ShowNoActivate);
        var style = GetWindowStyle(handle);
        style &= ~(WsCaption | WsThickFrame | WsMinimize | WsMaximize | WsSysMenu);
        style |= WsPopup;
        SetWindowStyle(handle, style);

        SetWindowPos(
            handle,
            IntPtr.Zero,
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            SwpNoActivate | SwpNoOwnerZOrder | SwpFrameChanged | SwpShowWindow);
    }

    private static long GetWindowStyle(IntPtr handle)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr(handle, GwlStyle).ToInt64()
            : GetWindowLong(handle, GwlStyle);
    }

    private static void SetWindowStyle(IntPtr handle, long style)
    {
        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr(handle, GwlStyle, new IntPtr(style));
        }
        else
        {
            SetWindowLong(handle, GwlStyle, unchecked((int)style));
        }
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr lParam);

    private const int ShowNoActivate = 4;
    private const int ShowMinimize = 6;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    private static bool IsBroodWarWindow(IntPtr handle)
    {
        var title = new System.Text.StringBuilder(256);
        GetWindowText(handle, title, title.Capacity);
        return IsBroodWarWindowTitle(title.ToString());
    }

    internal static bool IsBroodWarWindowTitle(string title)
    {
        return title.Equals("Brood War", StringComparison.OrdinalIgnoreCase) ||
               title.StartsWith("Brood War Instance ", StringComparison.OrdinalIgnoreCase);
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr handle, int command);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr handle, System.Text.StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong(IntPtr handle, int index, int value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr handle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

internal sealed record BorderlessApplyResult(bool Applied, int? ProcessId);
