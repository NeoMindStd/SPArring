using System.Text;

namespace Sparring.Client;

internal sealed record SmokeAiActivityLogSnapshot(IReadOnlyDictionary<string, SmokeAiActivityLogEntry> Entries)
{
    private const int MaxReadBytesPerFile = 512 * 1024;

    public static SmokeAiActivityLogSnapshot Capture(string writeDirectory)
    {
        if (!Directory.Exists(writeDirectory))
        {
            return new SmokeAiActivityLogSnapshot(new Dictionary<string, SmokeAiActivityLogEntry>(StringComparer.OrdinalIgnoreCase));
        }

        var entries = Directory.EnumerateFiles(writeDirectory)
            .Where(IsReadableLogFile)
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new KeyValuePair<string, SmokeAiActivityLogEntry>(
                    info.FullName,
                    new SmokeAiActivityLogEntry(info.Length, info.LastWriteTimeUtc));
            })
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        return new SmokeAiActivityLogSnapshot(entries);
    }

    public SmokeAiActivityLogSummary FindActivitySince(SmokeAiActivityLogSnapshot baseline)
    {
        var changedFiles = new List<string>();
        var meaningfulFiles = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var newLineCount = 0;
        var meaningfulLineCount = 0;

        foreach (var (path, current) in Entries)
        {
            baseline.Entries.TryGetValue(path, out var previous);
            if (previous is not null &&
                previous.Length == current.Length &&
                previous.LastWriteTimeUtc == current.LastWriteTimeUtc)
            {
                continue;
            }

            var startOffset = previous is not null && current.Length >= previous.Length
                ? previous.Length
                : 0;
            var text = ReadText(path, startOffset, current.Length);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            changedFiles.Add(Path.GetFileName(path));
            foreach (var line in SplitLines(text))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                newLineCount++;
                if (IsMeaningfulActivityLine(line))
                {
                    meaningfulLineCount++;
                    meaningfulFiles.Add(Path.GetFileName(path));
                }
            }
        }

        return new SmokeAiActivityLogSummary(
            Entries.Count,
            changedFiles.Count,
            newLineCount,
            meaningfulLineCount,
            meaningfulFiles.ToArray());
    }

    private static bool IsReadableLogFile(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() is ".txt" or ".log";
    }

    private static string ReadText(string path, long startOffset, long currentLength)
    {
        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            if (startOffset > 0)
            {
                stream.Seek(startOffset, SeekOrigin.Begin);
            }

            var bytesToRead = currentLength - startOffset;
            if (bytesToRead > MaxReadBytesPerFile)
            {
                stream.Seek(Math.Max(0, currentLength - MaxReadBytesPerFile), SeekOrigin.Begin);
            }

            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }

    internal static bool IsMeaningfulActivityLine(string line)
    {
        return line.Contains("Started morping", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Started morphing", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Started building", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Started training", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Started researching", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("Started upgrading", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record SmokeAiActivityLogEntry(long Length, DateTime LastWriteTimeUtc);

internal sealed record SmokeAiActivityLogSummary(
    int FileCount,
    int ChangedFileCount,
    int NewLineCount,
    int MeaningfulLineCount,
    IReadOnlyList<string> MeaningfulFiles)
{
    public static SmokeAiActivityLogSummary Empty { get; } = new(0, 0, 0, 0, []);

    public bool HasMeaningfulActivity => MeaningfulLineCount > 0;

    public string FormatFiles()
    {
        return MeaningfulFiles.Count == 0 ? "none" : string.Join("|", MeaningfulFiles);
    }
}
