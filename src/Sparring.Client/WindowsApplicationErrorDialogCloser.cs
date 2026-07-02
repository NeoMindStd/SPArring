using System.Runtime.InteropServices;
using System.Text;

namespace Sparring.Client;

internal static class WindowsApplicationErrorDialogCloser
{
    private const int WmCommand = 0x0111;
    private const int WmClose = 0x0010;
    private const int BmClick = 0x00F5;
    private static readonly IntPtr IdOk = new(1);

    public static IReadOnlySet<IntPtr> Capture()
    {
        var handles = new HashSet<IntPtr>();
        EnumWindows((handle, _) =>
        {
            if (IsWindowVisible(handle) && IsApplicationErrorTitle(GetTitle(handle)))
            {
                handles.Add(handle);
            }

            return true;
        }, IntPtr.Zero);
        return handles;
    }

    public static int CloseNewDialogs(IReadOnlySet<IntPtr> baseline)
    {
        var closed = 0;
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) ||
                baseline.Contains(handle) ||
                !IsApplicationErrorTitle(GetTitle(handle)))
            {
                return true;
            }

            if (!ClickOkButton(handle))
            {
                SendMessage(handle, WmCommand, IdOk, IntPtr.Zero);
            }

            Thread.Sleep(100);
            if (IsWindow(handle))
            {
                PostMessage(handle, WmClose, IntPtr.Zero, IntPtr.Zero);
            }

            closed++;
            return true;
        }, IntPtr.Zero);
        return closed;
    }

    public static int CloseAllDialogs()
    {
        return CloseNewDialogs(new HashSet<IntPtr>());
    }

    public static IDisposable CloseNewDialogsUntilDisposed(IReadOnlySet<IntPtr> baseline, TimeSpan interval)
    {
        var cancellation = new CancellationTokenSource();
        var task = Task.Run(async () =>
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    CloseNewDialogs(baseline);
                }
                catch
                {
                    // Dialog cleanup must never block smoke result reporting.
                }

                try
                {
                    await Task.Delay(interval, cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, cancellation.Token);

        return new BackgroundDialogCloser(cancellation, task);
    }

    internal static bool IsApplicationErrorTitle(string title)
    {
        return title.Equals("Windows - 응용 프로그램 오류", StringComparison.OrdinalIgnoreCase) ||
               title.Equals("Windows - Application Error", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTitle(IntPtr handle)
    {
        var title = new StringBuilder(256);
        GetWindowText(handle, title, title.Capacity);
        return title.ToString();
    }

    private static bool ClickOkButton(IntPtr dialogHandle)
    {
        var clicked = false;
        EnumChildWindows(dialogHandle, (handle, _) =>
        {
            if (!IsWindowVisible(handle) || !IsOkButton(handle))
            {
                return true;
            }

            SendMessage(handle, BmClick, IntPtr.Zero, IntPtr.Zero);
            clicked = true;
            return false;
        }, IntPtr.Zero);
        return clicked;
    }

    private static bool IsOkButton(IntPtr handle)
    {
        var className = new StringBuilder(64);
        GetClassName(handle, className, className.Capacity);
        if (!className.ToString().Equals("Button", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var text = GetTitle(handle);
        return text.Equals("확인", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("OK", StringComparison.OrdinalIgnoreCase);
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr parentHandle, EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr handle);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr handle, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private sealed class BackgroundDialogCloser(CancellationTokenSource cancellation, Task task) : IDisposable
    {
        public void Dispose()
        {
            cancellation.Cancel();
            try
            {
                task.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Best-effort cleanup only.
            }
            finally
            {
                cancellation.Dispose();
            }
        }
    }
}
