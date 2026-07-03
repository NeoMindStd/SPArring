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

    public static (Rectangle Left, Rectangle Right) RowPair(Rectangle workingArea, int rowIndex, int rowCount)
    {
        const int margin = 8;
        const int gap = 8;
        const double starCraftAspectRatio = 4.0 / 3.0;

        var rows = Math.Max(1, rowCount);
        var row = Math.Clamp(rowIndex, 0, rows - 1);
        var availableWidth = Math.Max(1, workingArea.Width - (margin * 2) - gap);
        var availableHeight = Math.Max(1, workingArea.Height - (margin * 2) - (gap * (rows - 1)));
        var cellWidth = Math.Max(1, availableWidth / 2);
        var cellHeight = Math.Max(1, availableHeight / rows);
        var window = FitAspectRatio(cellWidth, cellHeight, starCraftAspectRatio);

        var top = workingArea.Top + margin + (row * (cellHeight + gap)) + ((cellHeight - window.Height) / 2);
        var leftCellX = workingArea.Left + margin;
        var rightCellX = leftCellX + cellWidth + gap;
        var leftX = leftCellX + ((cellWidth - window.Width) / 2);
        var rightX = rightCellX + ((cellWidth - window.Width) / 2);

        return (
            new Rectangle(leftX, top, window.Width, window.Height),
            new Rectangle(rightX, top, window.Width, window.Height));
    }

    private static Size FitAspectRatio(int maxWidth, int maxHeight, double aspectRatio)
    {
        var width = Math.Max(1, maxWidth);
        var height = Math.Max(1, (int)Math.Round(width / aspectRatio));
        if (height > maxHeight)
        {
            height = Math.Max(1, maxHeight);
            width = Math.Max(1, (int)Math.Round(height * aspectRatio));
        }

        return new Size(Math.Min(width, maxWidth), Math.Min(height, maxHeight));
    }
}
