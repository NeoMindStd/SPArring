using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarAI.PracticeClient.App;

internal sealed class StarCraftMouseClipper : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Func<bool> _enabled;

    public StarCraftMouseClipper(Func<bool> enabled)
    {
        _enabled = enabled;
        _timer = new System.Windows.Forms.Timer { Interval = 50 };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        ReleaseClip();
    }

    public static void ReleaseClip()
    {
        ClipCursor(IntPtr.Zero);
    }

    private void Tick()
    {
        if (!_enabled())
        {
            ReleaseClip();
            return;
        }

        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            ReleaseClip();
            return;
        }

        GetWindowThreadProcessId(foreground, out var processId);
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.ProcessName.Equals("StarCraft", StringComparison.OrdinalIgnoreCase) &&
                !process.ProcessName.Equals("Brood War", StringComparison.OrdinalIgnoreCase))
            {
                ReleaseClip();
                return;
            }
        }
        catch
        {
            ReleaseClip();
            return;
        }

        if (!GetClientRect(foreground, out var rect))
        {
            ReleaseClip();
            return;
        }

        var topLeft = new Point { X = rect.Left, Y = rect.Top };
        var bottomRight = new Point { X = rect.Right, Y = rect.Bottom };
        ClientToScreen(foreground, ref topLeft);
        ClientToScreen(foreground, ref bottomRight);
        var screenRect = new Rect
        {
            Left = topLeft.X,
            Top = topLeft.Y,
            Right = bottomRight.X,
            Bottom = bottomRight.Y
        };
        ClipCursor(ref screenRect);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr handle, ref Point point);

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(ref Rect rect);

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(IntPtr rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
