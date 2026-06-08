using System.Runtime.InteropServices;

namespace StarAI.PracticeClient.App;

internal enum StarCraftScreenState
{
    Unknown,
    MenuLike,
    GameRoom,
    PreGameWait,
    BlockedDialog,
    InGame
}

internal static class StarCraftScreenAnalyzer
{
    public static StarCraftScreenState Analyze(Bitmap bitmap)
    {
        if (bitmap.Width < 64 || bitmap.Height < 64)
        {
            return StarCraftScreenState.Unknown;
        }

        var step = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / 240);
        var bottomStart = (int)(bitmap.Height * 0.72);
        var bottomSamples = 0;
        var hudPixels = 0;
        var hudPanelPixels = 0;
        var hudDarkPanelPixels = 0;
        var fullSamples = 0;
        var menuGreenPixels = 0;
        var roomFramePixels = 0;
        var lobbyTextPixels = 0;
        var centralSamples = 0;
        var preGamePanelPixels = 0;
        var dialogLightPixels = 0;

        for (var y = 0; y < bitmap.Height; y += step)
        {
            for (var x = 0; x < bitmap.Width; x += step)
            {
                var color = bitmap.GetPixel(x, y);
                fullSamples++;
                if (IsMenuGreen(color))
                {
                    menuGreenPixels++;
                }

                if (IsRoomFrameRed(color))
                {
                    roomFramePixels++;
                }

                if (IsLobbyTextColor(color))
                {
                    lobbyTextPixels++;
                }

                if (IsCentralOverlayRegion(bitmap, x, y))
                {
                    centralSamples++;
                    if (IsPreGamePanelBlue(color))
                    {
                        preGamePanelPixels++;
                    }

                    if (IsDialogLight(color))
                    {
                        dialogLightPixels++;
                    }
                }

                if (y < bottomStart)
                {
                    continue;
                }

                bottomSamples++;
                if (IsGameHudColor(color))
                {
                    hudPixels++;
                }

                if (IsGameHudPanelColor(color))
                {
                    hudPanelPixels++;
                }

                if (IsHudDarkPanelColor(color))
                {
                    hudDarkPanelPixels++;
                }
            }
        }

        var dialogLightRatio = centralSamples == 0 ? 0 : dialogLightPixels / (double)centralSamples;
        if (dialogLightRatio >= 0.025)
        {
            return StarCraftScreenState.BlockedDialog;
        }

        var hudRatio = bottomSamples == 0 ? 0 : hudPixels / (double)bottomSamples;
        var hudPanelRatio = bottomSamples == 0 ? 0 : hudPanelPixels / (double)bottomSamples;
        var hudDarkPanelRatio = bottomSamples == 0 ? 0 : hudDarkPanelPixels / (double)bottomSamples;
        var hudDetected = hudRatio >= 0.02 || hudPanelRatio >= 0.08;
        if (hudDetected && hudDarkPanelRatio >= 0.04)
        {
            return StarCraftScreenState.InGame;
        }

        var roomFrameRatio = fullSamples == 0 ? 0 : roomFramePixels / (double)fullSamples;
        if (roomFrameRatio >= 0.0022)
        {
            return StarCraftScreenState.GameRoom;
        }

        if (hudDetected)
        {
            return StarCraftScreenState.InGame;
        }

        var preGamePanelRatio = centralSamples == 0 ? 0 : preGamePanelPixels / (double)centralSamples;
        if (preGamePanelRatio >= 0.12)
        {
            return StarCraftScreenState.PreGameWait;
        }

        var menuGreenRatio = fullSamples == 0 ? 0 : menuGreenPixels / (double)fullSamples;
        var lobbyTextRatio = fullSamples == 0 ? 0 : lobbyTextPixels / (double)fullSamples;
        if (menuGreenRatio >= 0.0015 || lobbyTextRatio >= 0.0015)
        {
            return StarCraftScreenState.MenuLike;
        }

        return StarCraftScreenState.Unknown;
    }

    private static bool IsGameHudColor(Color color)
    {
        return (color.B >= 85 && color.G >= 70 && color.R <= 95) ||
               (color.R >= 145 && color.G >= 115 && color.B <= 90) ||
               (color.G >= 115 && color.B >= 80 && color.R <= 80);
    }

    private static bool IsGameHudPanelColor(Color color)
    {
        return color.R is >= 28 and <= 105 &&
               color.G is >= 34 and <= 120 &&
               color.B is >= 42 and <= 145 &&
               color.B >= color.R + 3 &&
               color.B >= color.G - 18;
    }

    private static bool IsHudDarkPanelColor(Color color)
    {
        return color.R <= 22 && color.G <= 24 && color.B <= 28;
    }

    private static bool IsMenuGreen(Color color)
    {
        return color.G >= 120 && color.R <= 80 && color.B <= 80;
    }

    private static bool IsRoomFrameRed(Color color)
    {
        return color.R >= 115 &&
               color.G <= 35 &&
               color.B <= 35 &&
               color.R >= color.G * 3 &&
               color.R >= color.B * 3;
    }

    private static bool IsLobbyTextColor(Color color)
    {
        return color.G >= 115 &&
               color.R >= 95 &&
               color.B <= 70 &&
               color.R >= color.B + 35 &&
               color.G >= color.B + 45;
    }

    private static bool IsCentralOverlayRegion(Bitmap bitmap, int x, int y)
    {
        return x >= bitmap.Width * 0.25 &&
               x <= bitmap.Width * 0.75 &&
               y >= bitmap.Height * 0.05 &&
               y <= bitmap.Height * 0.62;
    }

    private static bool IsPreGamePanelBlue(Color color)
    {
        return color.B >= 50 &&
               color.R <= 115 &&
               color.G <= 125 &&
               color.B >= color.R + 12 &&
               color.B >= color.G + 2;
    }

    private static bool IsDialogLight(Color color)
    {
        return color.R >= 210 && color.G >= 210 && color.B >= 210;
    }
}

internal static class StarCraftScreenDetector
{
    public static async Task<bool> WaitForInGameAsync(int processId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return await WaitForStableStateAsync(
                processId,
                StarCraftScreenState.InGame,
                timeout,
                stableSampleCount: 2,
                pollInterval: TimeSpan.FromMilliseconds(100),
                cancellationToken)
            .ConfigureAwait(true);
    }

    private static async Task<bool> WaitForStableStateAsync(
        int processId,
        StarCraftScreenState expectedState,
        TimeSpan timeout,
        int stableSampleCount,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        var stableSamples = 0;
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            if (Detect(processId) == expectedState)
            {
                stableSamples++;
                if (stableSamples >= stableSampleCount)
                {
                    return true;
                }
            }
            else
            {
                stableSamples = 0;
            }

            await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(true);
        }

        return false;
    }

    public static StarCraftScreenState Detect(int processId)
    {
        return TryCaptureWindowBitmap(processId, out var bitmap)
            ? AnalyzeAndDispose(bitmap)
            : StarCraftScreenState.Unknown;
    }

    public static bool TryCaptureWindowBitmap(int processId, out Bitmap? bitmap)
    {
        var handle = FindBroodWarWindow(processId);
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var rect))
        {
            bitmap = null;
            return false;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0 || IsIconic(handle))
        {
            bitmap = null;
            return false;
        }

        try
        {
            bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }

            return true;
        }
        catch
        {
            bitmap = null;
            return false;
        }
    }

    private static StarCraftScreenState AnalyzeAndDispose(Bitmap? bitmap)
    {
        if (bitmap is null)
        {
            return StarCraftScreenState.Unknown;
        }

        using (bitmap)
        {
            return StarCraftScreenAnalyzer.Analyze(bitmap);
        }
    }

    private static IntPtr FindBroodWarWindow(int processId)
    {
        var result = IntPtr.Zero;
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle))
            {
                return true;
            }

            GetWindowThreadProcessId(handle, out var currentProcessId);
            if (currentProcessId != processId)
            {
                return true;
            }

            var title = new System.Text.StringBuilder(256);
            GetWindowText(handle, title, title.Capacity);
            if (!StarCraftBorderlessWindow.IsBroodWarWindowTitle(title.ToString()))
            {
                return true;
            }

            result = handle;
            return false;
        }, IntPtr.Zero);

        return result;
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr handle, System.Text.StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr handle, out Rect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
