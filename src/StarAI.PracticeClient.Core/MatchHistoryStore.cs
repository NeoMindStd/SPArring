using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace StarAI.PracticeClient.Core;

public sealed record MatchRecord(
    DateTime StartedAt,
    string BotName,
    Race BotRace,
    int? BotElo,
    string MapName,
    string BuildName,
    string ReplayRoot,
    string Result = "미확인");

public sealed record ReplayRecord(DateTime LastWriteTime, string FileName, string FullPath, long Length);

public sealed class MatchHistoryStore
{
    private const string HistoryRelativePath = "bwapi-data/read/starai-match-history.json";

    public string HistoryPath(string starCraftRoot) =>
        Path.Combine(starCraftRoot, HistoryRelativePath.Replace('/', Path.DirectorySeparatorChar));

    public IReadOnlyList<MatchRecord> Load(string starCraftRoot)
    {
        var path = HistoryPath(starCraftRoot);
        if (!File.Exists(path))
        {
            return Array.Empty<MatchRecord>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<MatchRecord>>(File.ReadAllText(path), CreateJsonOptions()) ?? new List<MatchRecord>();
        }
        catch
        {
            return Array.Empty<MatchRecord>();
        }
    }

    public void Add(string starCraftRoot, MatchRecord record)
    {
        var path = HistoryPath(starCraftRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var records = Load(starCraftRoot).ToList();
        records.Insert(0, record);
        File.WriteAllText(path, JsonSerializer.Serialize(records.Take(500), CreateJsonOptions()));
    }

    public IReadOnlyList<ReplayRecord> GetReplays(string replayRoot = PracticeConfigurator.DefaultReplayRoot)
    {
        if (!Directory.Exists(replayRoot))
        {
            return Array.Empty<ReplayRecord>();
        }

        return Directory.EnumerateFiles(replayRoot, "*.rep", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTime)
            .Take(300)
            .Select(file => new ReplayRecord(file.LastWriteTime, file.Name, file.FullName, file.Length))
            .ToArray();
    }

    private static JsonSerializerOptions CreateJsonOptions() => new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
}
