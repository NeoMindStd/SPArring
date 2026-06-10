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
    private const int VirtualKeyF4 = 0x73;
    private const int VirtualKeyMenu = 0x12;
    private const int KeyboardFlagAltDown = 0x20;

    private readonly ActionRateCounter _counter;
    private readonly IReadOnlySet<int> _excludedStarCraftProcessIds;
    private readonly int? _playerStarCraftProcessId;
    private readonly Action? _playerExitShortcutRequested;
    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;

    public GlobalInputActionHook(ActionRateCounter counter, IReadOnlySet<int> excludedStarCraftProcessIds)
        : this(counter, excludedStarCraftProcessIds, null, null)
    {
    }

    public GlobalInputActionHook(
        ActionRateCounter counter,
        IReadOnlySet<int> excludedStarCraftProcessIds,
        int? playerStarCraftProcessId,
        Action? playerExitShortcutRequested)
    {
        _counter = counter;
        _excludedStarCraftProcessIds = excludedStarCraftProcessIds;
        _playerStarCraftProcessId = playerStarCraftProcessId;
        _playerExitShortcutRequested = playerExitShortcutRequested;
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
        var message = wParam.ToInt32();
        if (code >= 0 && (message == WmKeyDown || message == WmSysKeyDown))
        {
            var foregroundProcessId = ForegroundPracticeStarCraftProcessId();
            var key = Marshal.PtrToStructure<KeyboardLowLevelHookData>(lParam);
            var altDown = message == WmSysKeyDown ||
                          (key.Flags & KeyboardFlagAltDown) != 0 ||
                          (GetKeyState(VirtualKeyMenu) & 0x8000) != 0;

            if (ShouldInterceptPlayerExitShortcut(
                    message,
                    key.VirtualKeyCode,
                    altDown,
                    foregroundProcessId,
                    _playerStarCraftProcessId))
            {
                _playerExitShortcutRequested?.Invoke();
                return new IntPtr(1);
            }

            if (foregroundProcessId is not null)
            {
                _counter.RecordAction();
            }
        }

        return CallNextHookEx(_keyboardHook, code, wParam, lParam);
    }

    private IntPtr MouseCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        var message = wParam.ToInt32();
        if (code >= 0 &&
            (message == WmLButtonDown || message == WmRButtonDown || message == WmMButtonDown || message == WmXButtonDown) &&
            ForegroundPracticeStarCraftProcessId() is not null)
        {
            _counter.RecordAction();
        }

        return CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    internal static bool ShouldInterceptPlayerExitShortcut(
        int message,
        int virtualKeyCode,
        bool altDown,
        int? foregroundProcessId,
        int? playerStarCraftProcessId)
    {
        return playerStarCraftProcessId is not null &&
               foregroundProcessId == playerStarCraftProcessId &&
               altDown &&
               virtualKeyCode == VirtualKeyF4 &&
               (message == WmKeyDown || message == WmSysKeyDown);
    }

    private int? ForegroundPracticeStarCraftProcessId()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return null;
        }

        GetWindowThreadProcessId(foreground, out var processId);
        if (_excludedStarCraftProcessIds.Contains(processId))
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.ProcessName.Equals("StarCraft", StringComparison.OrdinalIgnoreCase) ||
                process.ProcessName.Equals("Brood War", StringComparison.OrdinalIgnoreCase))
            {
                return processId;
            }
        }
        catch
        {
        }

        return null;
    }

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

#pragma warning disable CS0649
    private struct KeyboardLowLevelHookData
    {
        public int VirtualKeyCode;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }
#pragma warning restore CS0649

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

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);
}
