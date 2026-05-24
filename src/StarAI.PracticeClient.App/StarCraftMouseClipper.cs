using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarAI.PracticeClient.App;

internal sealed class StarCraftMouseClipper : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Func<bool> _enabled;
    private IntPtr _lastClippedWindow = IntPtr.Zero;

    public StarCraftMouseClipper(Func<bool> enabled)
    {
        _enabled = enabled;
        _timer = new System.Windows.Forms.Timer { Interval = 250 };
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
            _lastClippedWindow = IntPtr.Zero;
            ReleaseClip();
            return;
        }

        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            _lastClippedWindow = IntPtr.Zero;
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
                _lastClippedWindow = IntPtr.Zero;
                ReleaseClip();
                return;
            }
        }
        catch
        {
            _lastClippedWindow = IntPtr.Zero;
            ReleaseClip();
            return;
        }

        if (!GetCursorPos(out var cursor) || !GetClientRect(foreground, out var rect))
        {
            _lastClippedWindow = IntPtr.Zero;
            ReleaseClip();
            return;
        }

        var clientCursor = cursor;
        ScreenToClient(foreground, ref clientCursor);
        var cursorInsideClient =
            clientCursor.X >= rect.Left &&
            clientCursor.X < rect.Right &&
            clientCursor.Y >= rect.Top &&
            clientCursor.Y < rect.Bottom;

        if (!cursorInsideClient)
        {
            _lastClippedWindow = IntPtr.Zero;
            ReleaseClip();
            return;
        }

        if ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0 && _lastClippedWindow != foreground)
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
        _lastClippedWindow = foreground;
    }

    private const int VK_LBUTTON = 0x01;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr handle, ref Point point);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr handle, ref Point point);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

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
