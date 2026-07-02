using System.Drawing;

namespace Sparring.Client;

internal sealed record SmokeScreenActivitySummary(
    int SampleCount,
    int ChangedSamples,
    double ChangedRatio,
    double AverageDelta)
{
    public static SmokeScreenActivitySummary Empty { get; } = new(0, 0, 0, 0);

    public bool HasMeaningfulActivity =>
        SampleCount >= SmokeScreenActivityAnalyzer.MinimumSamples &&
        ChangedSamples >= SmokeScreenActivityAnalyzer.MinimumChangedSamples &&
        ChangedRatio >= SmokeScreenActivityAnalyzer.MinimumChangedRatio &&
        AverageDelta >= SmokeScreenActivityAnalyzer.MinimumAverageDelta;
}

internal static class SmokeScreenActivityAnalyzer
{
    internal const int MinimumSamples = 1000;
    internal const int MinimumChangedSamples = 20;
    internal const double MinimumChangedRatio = 0.005;
    internal const double MinimumAverageDelta = 1.0;

    private const int SampleStep = 4;
    private const int ChangedPixelDelta = 45;

    public static SmokeScreenActivitySummary Compare(Bitmap? before, Bitmap? after)
    {
        if (before is null || after is null)
        {
            return SmokeScreenActivitySummary.Empty;
        }

        var width = Math.Min(before.Width, after.Width);
        var height = Math.Min(before.Height, after.Height);
        if (width < 64 || height < 64)
        {
            return SmokeScreenActivitySummary.Empty;
        }

        var startX = Math.Max(0, (int)(width * 0.04));
        var endX = Math.Min(width, (int)(width * 0.96));
        var startY = Math.Max(0, (int)(height * 0.08));
        var endY = Math.Min(height, (int)(height * 0.72));

        var samples = 0;
        var changed = 0;
        long totalDelta = 0;
        for (var y = startY; y < endY; y += SampleStep)
        {
            for (var x = startX; x < endX; x += SampleStep)
            {
                var beforePixel = before.GetPixel(x, y);
                var afterPixel = after.GetPixel(x, y);
                var delta =
                    Math.Abs(beforePixel.R - afterPixel.R) +
                    Math.Abs(beforePixel.G - afterPixel.G) +
                    Math.Abs(beforePixel.B - afterPixel.B);
                totalDelta += delta;
                samples++;
                if (delta >= ChangedPixelDelta)
                {
                    changed++;
                }
            }
        }

        if (samples == 0)
        {
            return SmokeScreenActivitySummary.Empty;
        }

        return new SmokeScreenActivitySummary(
            samples,
            changed,
            changed / (double)samples,
            totalDelta / (double)samples);
    }
}
