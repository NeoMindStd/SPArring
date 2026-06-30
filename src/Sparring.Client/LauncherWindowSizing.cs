namespace Sparring.Client;

internal static class LauncherWindowSizing
{
    public static Size MinimumSize { get; } = new(760, 520);

    public static Size InitialSize(Rectangle workingArea)
    {
        var width = workingArea.Width <= 1500
            ? workingArea.Width - 48
            : Math.Min(1480, Math.Max(1180, (int)Math.Round(workingArea.Width * 0.72)));
        var height = workingArea.Height <= 1000
            ? workingArea.Height - 48
            : Math.Min(980, Math.Max(820, (int)Math.Round(workingArea.Height * 0.78)));

        var maxWidth = Math.Max(1, workingArea.Width);
        var maxHeight = Math.Max(1, workingArea.Height);
        var minWidth = Math.Min(MinimumSize.Width, maxWidth);
        var minHeight = Math.Min(MinimumSize.Height, maxHeight);

        width = Math.Clamp(width, minWidth, maxWidth);
        height = Math.Clamp(height, minHeight, maxHeight);
        return new Size(width, height);
    }
}

internal static class LauncherLayoutScale
{
    public const int BaseDpi = 96;

    public static int ToBaseDpi(int logicalPixels, int deviceDpi)
    {
        if (logicalPixels <= 0)
        {
            return 0;
        }

        var dpi = Math.Max(BaseDpi, deviceDpi);
        return Math.Max(1, (int)Math.Round(logicalPixels * (double)BaseDpi / dpi));
    }

    public static int FromBaseDpi(int basePixels, int deviceDpi)
    {
        if (basePixels <= 0)
        {
            return 0;
        }

        var dpi = Math.Max(BaseDpi, deviceDpi);
        return Math.Max(1, (int)Math.Round(basePixels * (double)dpi / BaseDpi));
    }
}
