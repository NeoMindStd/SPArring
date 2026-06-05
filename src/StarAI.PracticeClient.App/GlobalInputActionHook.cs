using StarAI.PracticeClient.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarAI.PracticeClient.App;

internal sealed class GlobalInputActionHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmXButtonDown = 0x020B;

    private readonly ActionRateCounter _counter;
    private readonly IReadOnlySet<int> _excludedStarCraftProcessIds;
    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;

    public GlobalInputActionHook(ActionRateCounter counter, IReadOnlySet<int> excludedStarCraftProcessIds)
    {
        _counter = counter;
        _excludedStarCraftProcessIds = excludedStarCraftProcessIds;
        _keyboardProc = KeyboardCallback;
        _mouseProc = MouseCallback;
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, IntPtr.Zero, 0);
        _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProc, IntPtr.Zero, 0);
    }

    public void Dispose()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr KeyboardCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && (wParam.ToInt32() == WmKeyDown || wParam.ToInt32() == WmSysKeyDown) && IsForegroundPracticeStarCraft())
        {
            _counter.RecordAction();
        }

        return CallNextHookEx(_keyboardHook, code, wParam, lParam);
    }

    private IntPtr MouseCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        var message = wParam.ToInt32();
        if (code >= 0 &&
            (message == WmLButtonDown || message == WmRButtonDown || message == WmMButtonDown || message == WmXButtonDown) &&
            IsForegroundPracticeStarCraft())
        {
            _counter.RecordAction();
        }

        return CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    private bool IsForegroundPracticeStarCraft()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(foreground, out var processId);
        if (_excludedStarCraftProcessIds.Contains(processId))
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName.Equals("StarCraft", StringComparison.OrdinalIgnoreCase) ||
                   process.ProcessName.Equals("Brood War", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int hookId, HookProc callback, IntPtr instance, uint threadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);
}
