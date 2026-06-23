namespace Sparring.Client;

public static class OverlaySizing
{
    public static Size MeasureOverlaySize(string text, Font font)
    {
        var measured = TextRenderer.MeasureText(
            text,
            font,
            new Size(900, 120),
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        var paddingX = Math.Max(28, (int)Math.Ceiling(font.SizeInPoints * 1.8F));
        var paddingY = Math.Max(10, (int)Math.Ceiling(font.SizeInPoints * 0.75F));
        var estimatedWidth = (int)Math.Ceiling(text.Length * font.SizeInPoints * 0.82F) + paddingX;
        var width = Math.Clamp(Math.Max(measured.Width + paddingX, estimatedWidth), 220, 460);
        var height = Math.Clamp(measured.Height + paddingY, 36, 72);
        return new Size(width, height);
    }

    public static Point ChooseLocation(Rectangle gameBounds, Size overlaySize)
    {
        var margin = Math.Clamp(gameBounds.Width / 80, 12, 32);
        var left = Math.Min(gameBounds.Left + margin, gameBounds.Right - overlaySize.Width - margin);
        var top = Math.Min(gameBounds.Top + margin, gameBounds.Bottom - overlaySize.Height - margin);
        return new Point(Math.Max(gameBounds.Left, left), Math.Max(gameBounds.Top, top));
    }
}
