namespace Sparring.Client;

internal static class BotMatchWindowLayout
{
    public static (Rectangle Left, Rectangle Right) SideBySide(Rectangle workingArea)
    {
        const int margin = 8;
        const int gap = 16;
        var availableWidth = Math.Max(1, workingArea.Width - (margin * 2) - gap);
        var width = Math.Min(720, Math.Max(320, availableWidth / 2));
        var height = Math.Min(560, Math.Max(240, workingArea.Height - (margin * 2)));
        var top = workingArea.Top + margin;
        var leftX = workingArea.Left + margin;
        var rightX = leftX + width + gap;
        if (rightX + width > workingArea.Right - margin)
        {
            rightX = workingArea.Right - margin - width;
            leftX = Math.Max(workingArea.Left + margin, rightX - gap - width);
        }

        return (
            new Rectangle(leftX, top, width, height),
            new Rectangle(rightX, top, width, height));
    }
}
