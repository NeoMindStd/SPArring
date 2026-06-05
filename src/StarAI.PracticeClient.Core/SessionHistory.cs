using System.Text.Json;

namespace StarAI.PracticeClient.Core;

public sealed record PracticeSessionRecord(
    Guid Id,
    DateTime StartedAtUtc,
    DateTime LastUpdatedAtUtc,
    string BotName,
    StarCraftRace BotRace,
    string MapName,
    string MapFileName,
    StarCraftRace PlayerRace,
    string ReplayRoot,
    int ActionCount,
    int ActionsPerMinute,
    double DurationSeconds);

public sealed class PracticeSessionHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public PracticeSessionHistoryStore(string historyPath)
    {
        HistoryPath = historyPath;
    }

    public string HistoryPath { get; }

    public IReadOnlyList<PracticeSessionRecord> Load()
    {
        if (!File.Exists(HistoryPath))
        {
            return [];
        }

        var records = JsonSerializer.Deserialize<List<PracticeSessionRecord>>(File.ReadAllText(HistoryPath), JsonOptions);
        return records?
            .OrderByDescending(record => record.StartedAtUtc)
            .ToList() ?? [];
    }

    public void Upsert(PracticeSessionRecord record)
    {
        var records = Load().ToList();
        var index = records.FindIndex(existing => existing.Id == record.Id);
        if (index >= 0)
        {
            records[index] = record;
        }
        else
        {
            records.Add(record);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
        File.WriteAllText(
            HistoryPath,
            JsonSerializer.Serialize(records.OrderByDescending(item => item.StartedAtUtc), JsonOptions));
    }
}
