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
    double DurationSeconds,
    PracticeSessionMode Mode = PracticeSessionMode.Sparring,
    PracticeSessionOutcome Outcome = PracticeSessionOutcome.InProgress,
    int? PlayerRatingBefore = null,
    int? OpponentRating = null,
    int? PlayerRatingAfter = null,
    int? RatingDelta = null,
    string? ResultSource = null)
{
    public string StartedLocalText => StartedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string ModeText => Mode == PracticeSessionMode.Ladder ? "래더" : "스파링";

    public string OutcomeText => PracticeSessionOutcomeText.ToDisplayText(Outcome);

    public string MatchupText => $"{PlayerRace} vs {BotRace}";

    public string DurationText
    {
        get
        {
            var duration = TimeSpan.FromSeconds(Math.Max(0, DurationSeconds));
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss")
                : duration.ToString(@"m\:ss");
        }
    }

    public string RatingText => PlayerRatingAfter is null
        ? string.Empty
        : RatingDelta is null
            ? PlayerRatingAfter.Value.ToString()
            : $"{PlayerRatingAfter.Value} ({RatingDelta.Value:+#;-#;0})";

    public string ResultSourceText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ResultSource))
            {
                return string.Empty;
            }

            if (ResultSource.StartsWith("player-left-ingame:", StringComparison.OrdinalIgnoreCase))
            {
                return "플레이어 나가기";
            }

            if (ResultSource.StartsWith("player-process-exited", StringComparison.OrdinalIgnoreCase))
            {
                return "플레이어 종료";
            }

            if (ResultSource.StartsWith("ai-process-exited", StringComparison.OrdinalIgnoreCase))
            {
                return "AI 종료";
            }

            return Path.IsPathFullyQualified(ResultSource)
                ? Path.GetFileName(ResultSource)
                : ResultSource;
        }
    }
}

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
