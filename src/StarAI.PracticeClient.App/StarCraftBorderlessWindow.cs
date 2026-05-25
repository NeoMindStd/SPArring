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
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;

    public static bool ApplyWhenReady(string starCraftRoot, Rectangle targetBounds, TimeSpan timeout)
    {
        var expectedExe = Path.GetFullPath(Path.Combine(starCraftRoot, "StarCraft.exe"));
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (TryApply(expectedExe, targetBounds))
            {
                return true;
            }

            Thread.Sleep(200);
        }

        return false;
    }

    private static bool TryApply(string expectedExe, Rectangle targetBounds)
    {
        var applied = false;
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle))
            {
                return true;
            }

            GetWindowThreadProcessId(handle, out var processId);
            if (!IsExpectedStarCraftProcess(processId, expectedExe))
            {
                return true;
            }

            ApplyBorderless(handle, targetBounds);
            applied = true;
            return false;
        }, IntPtr.Zero);

        return applied;
    }

    private static bool IsExpectedStarCraftProcess(int processId, string expectedExe)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.ProcessName.Equals("StarCraft", StringComparison.OrdinalIgnoreCase) &&
                !process.ProcessName.Equals("Brood War", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var actualPath = process.MainModule?.FileName;
            return !string.IsNullOrWhiteSpace(actualPath) &&
                   string.Equals(Path.GetFullPath(actualPath), expectedExe, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyBorderless(IntPtr handle, Rectangle bounds)
    {
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
            SwpNoZOrder | SwpNoOwnerZOrder | SwpFrameChanged | SwpShowWindow);
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

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

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
}
