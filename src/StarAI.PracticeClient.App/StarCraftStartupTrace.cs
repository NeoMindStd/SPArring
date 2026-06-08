using System.Diagnostics;
using System.Drawing.Imaging;
using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

internal sealed record StarCraftStartupTraceSummary(
    int SampleCount,
    long? FirstCaptureMs,
    long? FirstInGameMs,
    int MaxRedErrorPixels,
    string? MaxRedErrorFrame);

internal sealed class StarCraftStartupTrace : IDisposable
{
    private readonly int _processId;
    private readonly Stopwatch _timing;
    private readonly TimeSpan _interval;
    private readonly int _maxSamples;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _worker;
    private readonly object _summaryLock = new();
    private int _sampleCount;
    private long? _firstCaptureMs;
    private long? _firstInGameMs;
    private int _maxRedErrorPixels;
    private string? _maxRedErrorFrame;
    private bool _disposed;

    private StarCraftStartupTrace(
        int processId,
        string traceDirectory,
        Stopwatch timing,
        TimeSpan interval,
        int maxSamples)
    {
        _processId = processId;
        TraceDirectory = traceDirectory;
        _timing = timing;
        _interval = interval;
        _maxSamples = maxSamples;
        _worker = Task.Run(CaptureLoop);
    }

    public string TraceDirectory { get; }

    public static StarCraftStartupTrace Start(
        PracticePaths paths,
        int processId,
        Stopwatch timing,
        TimeSpan? interval = null,
        int maxSamples = 240)
    {
        var traceDirectory = Path.Combine(paths.RepositoryRoot, "artifacts", "screenshots", "startup-trace");
        ResetTraceDirectory(traceDirectory);
        return new StarCraftStartupTrace(
            processId,
            traceDirectory,
            timing,
            interval ?? TimeSpan.FromMilliseconds(150),
            maxSamples);
    }

    public StarCraftStartupTraceSummary StopAndWait(TimeSpan timeout)
    {
        _cancellation.Cancel();
        try
        {
            _worker.Wait(timeout);
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(inner => inner is TaskCanceledException or OperationCanceledException))
        {
        }
        catch (OperationCanceledException)
        {
        }

        return Summary();
    }

    public StarCraftStartupTraceSummary Summary()
    {
        lock (_summaryLock)
        {
            return new StarCraftStartupTraceSummary(
                _sampleCount,
                _firstCaptureMs,
                _firstInGameMs,
                _maxRedErrorPixels,
                _maxRedErrorFrame);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAndWait(TimeSpan.FromSeconds(2));
        _cancellation.Dispose();
    }

    internal static Rectangle BuildChatCropRectangle(Size sourceSize)
    {
        var width = Math.Min(sourceSize.Width, 1600);
        var height = Math.Min(sourceSize.Height, 760);
        return new Rectangle(0, 0, Math.Max(1, width), Math.Max(1, height));
    }

    internal static int CountRedErrorPixels(Bitmap bitmap)
    {
        var crop = BuildChatCropRectangle(bitmap.Size);
        var redPixels = 0;
        for (var y = crop.Top; y < crop.Bottom; y += 2)
        {
            for (var x = crop.Left; x < crop.Right; x += 2)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.R >= 150 &&
                    color.G <= 90 &&
                    color.B <= 90 &&
                    color.R >= color.G + 55 &&
                    color.R >= color.B + 55)
                {
                    redPixels++;
                }
            }
        }

        return redPixels;
    }

    private async Task CaptureLoop()
    {
        var csvPath = Path.Combine(TraceDirectory, "player-startup-trace.csv");
        await using var writer = new StreamWriter(csvPath, append: false);
        await writer.WriteLineAsync("index,elapsedMs,state,redErrorPixels,cropFile,fullFile").ConfigureAwait(false);

        var lastState = StarCraftScreenState.Unknown;
        var hasLastState = false;
        for (var index = 0; index < _maxSamples && !_cancellation.IsCancellationRequested; index++)
        {
            try
            {
                CaptureSample(index, writer, ref lastState, ref hasLastState);
                await writer.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{index},{_timing.ElapsedMilliseconds},CaptureError,0,,\"{EscapeCsv(ex.GetType().Name)}\"")
                    .ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(_interval, _cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void CaptureSample(
        int index,
        TextWriter writer,
        ref StarCraftScreenState lastState,
        ref bool hasLastState)
    {
        if (!StarCraftScreenDetector.TryCaptureWindowBitmap(_processId, out var bitmap) || bitmap is null)
        {
            writer.WriteLine($"{index},{_timing.ElapsedMilliseconds},NoCapture,0,,");
            return;
        }

        using (bitmap)
        {
            var elapsedMs = _timing.ElapsedMilliseconds;
            var state = StarCraftScreenAnalyzer.Analyze(bitmap);
            var redErrorPixels = CountRedErrorPixels(bitmap);
            var cropFile = $"player-{index:D4}-{elapsedMs:D6}ms-{state}-red{redErrorPixels}.png";
            var cropPath = Path.Combine(TraceDirectory, cropFile);
            SaveChatCrop(bitmap, cropPath);

            string? fullFile = null;
            if (!hasLastState || state != lastState || state == StarCraftScreenState.InGame)
            {
                fullFile = $"player-full-{index:D4}-{elapsedMs:D6}ms-{state}.png";
                bitmap.Save(Path.Combine(TraceDirectory, fullFile), ImageFormat.Png);
            }

            hasLastState = true;
            lastState = state;
            UpdateSummary(elapsedMs, state, redErrorPixels, cropFile);
            writer.WriteLine($"{index},{elapsedMs},{state},{redErrorPixels},{EscapeCsv(cropFile)},{EscapeCsv(fullFile)}");
        }
    }

    private void UpdateSummary(long elapsedMs, StarCraftScreenState state, int redErrorPixels, string cropFile)
    {
        lock (_summaryLock)
        {
            _sampleCount++;
            _firstCaptureMs ??= elapsedMs;
            if (state == StarCraftScreenState.InGame)
            {
                _firstInGameMs ??= elapsedMs;
            }

            if (redErrorPixels > _maxRedErrorPixels)
            {
                _maxRedErrorPixels = redErrorPixels;
                _maxRedErrorFrame = cropFile;
            }
        }
    }

    private static void SaveChatCrop(Bitmap bitmap, string path)
    {
        var crop = BuildChatCropRectangle(bitmap.Size);
        using var clone = bitmap.Clone(crop, PixelFormat.Format24bppRgb);
        clone.Save(path, ImageFormat.Png);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
    }

    private static void ResetTraceDirectory(string traceDirectory)
    {
        Directory.CreateDirectory(traceDirectory);
        foreach (var file in Directory.EnumerateFiles(traceDirectory))
        {
            File.Delete(file);
        }
    }
}
