namespace Sparring.Client;

internal static class LauncherWindowSizing
{
    public static Size MinimumSize { get; } = new(900, 620);

    public static Size InitialSize(Rectangle workingArea)
    {
        var width = workingArea.Width <= 1500
            ? workingArea.Width - 48
            : Math.Min(1480, Math.Max(1180, (int)Math.Round(workingArea.Width * 0.72)));
        var height = workingArea.Height <= 1000
            ? workingArea.Height - 48
            : Math.Min(980, Math.Max(820, (int)Math.Round(workingArea.Height * 0.78)));

        width = Math.Clamp(width, MinimumSize.Width, workingArea.Width);
        height = Math.Clamp(height, MinimumSize.Height, workingArea.Height);
        return new Size(width, height);
    }
}
