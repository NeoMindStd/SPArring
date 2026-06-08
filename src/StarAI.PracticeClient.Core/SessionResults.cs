using System.Text.Json;
using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public enum PracticeSessionMode
{
    Sparring,
    Ladder
}

public enum PracticeSessionOutcome
{
    InProgress,
    PlayerWin,
    PlayerLoss,
    Draw,
    Abandoned,
    Unknown
}

public static class PracticeSessionOutcomeText
{
    public static string ToDisplayText(PracticeSessionOutcome outcome)
    {
        return outcome switch
        {
            PracticeSessionOutcome.InProgress => "진행 중",
            PracticeSessionOutcome.PlayerWin => "승",
            PracticeSessionOutcome.PlayerLoss => "패",
            PracticeSessionOutcome.Draw => "무",
            PracticeSessionOutcome.Abandoned => "중단",
            _ => "미확인"
        };
    }
}

public readonly record struct EloRatingChange(
    int PlayerRatingBefore,
    int OpponentRating,
    int PlayerRatingAfter)
{
    public int Delta => PlayerRatingAfter - PlayerRatingBefore;
}

public static class EloRatingCalculator
{
    public const int DefaultRating = 1500;
    public const int DefaultKFactor = 32;

    public static EloRatingChange Calculate(
        int playerRating,
        int opponentRating,
        PracticeSessionOutcome outcome,
        int kFactor = DefaultKFactor)
    {
        var score = outcome switch
        {
            PracticeSessionOutcome.PlayerWin => 1.0,
            PracticeSessionOutcome.PlayerLoss => 0.0,
            PracticeSessionOutcome.Draw => 0.5,
            _ => throw new InvalidOperationException("Rating can be updated only for a completed win, loss, or draw.")
        };

        var expected = 1.0 / (1.0 + Math.Pow(10.0, (opponentRating - playerRating) / 400.0));
        var after = (int)Math.Round(playerRating + kFactor * (score - expected), MidpointRounding.AwayFromZero);
        if (outcome == PracticeSessionOutcome.PlayerWin && after <= playerRating)
        {
            after = playerRating + 1;
        }

        return new EloRatingChange(playerRating, opponentRating, Math.Max(0, after));
    }
}

public sealed record PracticeLadderRating(
    int PlayerRating,
    DateTime LastUpdatedAtUtc);

public sealed class PracticeLadderRatingStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public PracticeLadderRatingStore(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public PracticeLadderRating Load()
    {
        if (!File.Exists(Path))
        {
            return Default();
        }

        try
        {
            return JsonSerializer.Deserialize<PracticeLadderRating>(File.ReadAllText(Path), JsonOptions)
                ?? Default();
        }
        catch (JsonException)
        {
            return Default();
        }
    }

    public PracticeLadderRating Save(int playerRating)
    {
        var rating = new PracticeLadderRating(Math.Max(0, playerRating), DateTime.UtcNow);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(rating, JsonOptions));
        return rating;
    }

    public PracticeLadderRating Reset()
    {
        return Save(EloRatingCalculator.DefaultRating);
    }

    private static PracticeLadderRating Default()
    {
        return new PracticeLadderRating(EloRatingCalculator.DefaultRating, DateTime.MinValue);
    }
}

public sealed record BotResultLogObservation(
    PracticeSessionOutcome PlayerOutcome,
    string SourcePath,
    DateTime LastWriteTimeUtc);

public sealed record TournamentGameStateObservation(
    PracticeSessionOutcome PlayerOutcome,
    string SourcePath,
    DateTime LastWriteTimeUtc);

public static class PracticeSessionOutcomeResolver
{
    public static PracticeSessionOutcome Resolve(
        PracticeSessionMode mode,
        BotResultLogObservation? botResult,
        string reason)
    {
        return Resolve(mode, botResult, null, reason);
    }

    public static PracticeSessionOutcome Resolve(
        PracticeSessionMode mode,
        BotResultLogObservation? botResult,
        TournamentGameStateObservation? gameStateResult,
        string reason)
    {
        if (botResult is not null)
        {
            return botResult.PlayerOutcome;
        }

        if (gameStateResult is not null)
        {
            return gameStateResult.PlayerOutcome;
        }

        if (mode == PracticeSessionMode.Sparring && IsPlayerQuitReason(reason))
        {
            return PracticeSessionOutcome.Abandoned;
        }

        return PracticeSessionOutcome.Unknown;
    }

    private static bool IsPlayerQuitReason(string reason)
    {
        return reason.StartsWith("player-left-ingame:", StringComparison.OrdinalIgnoreCase) ||
               reason.StartsWith("player-process-exited", StringComparison.OrdinalIgnoreCase);
    }
}

public static class TournamentGameStateReader
{
    public const string RelativeGameStatePath = "bwapi-data/gameState.txt";

    private const int SelfNameLineIndex = 1;
    private const int DefeatedLineIndex = 8;
    private const int VictoriousLineIndex = 9;
    private const int GameOverLineIndex = 10;
    private const int MinimumLineCount = GameOverLineIndex + 1;

    public static TournamentGameStateObservation? FindPlayerOutcome(
        string runtimeRoot,
        DateTime sessionStartedAtUtc,
        string expectedSelfName = "StarAIHuman")
    {
        if (string.IsNullOrWhiteSpace(runtimeRoot))
        {
            return null;
        }

        var path = Path.Combine(runtimeRoot, RelativeGameStatePath);
        if (!File.Exists(path))
        {
            return null;
        }

        var file = new FileInfo(path);
        if (file.LastWriteTimeUtc < sessionStartedAtUtc.AddSeconds(-2))
        {
            return null;
        }

        try
        {
            var lines = ReadAllLinesShared(path);
            var outcome = ParsePlayerOutcome(lines, expectedSelfName);
            return outcome is null
                ? null
                : new TournamentGameStateObservation(outcome.Value, path, file.LastWriteTimeUtc);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static PracticeSessionOutcome? ParsePlayerOutcome(
        IReadOnlyList<string> lines,
        string expectedSelfName = "StarAIHuman")
    {
        if (lines.Count < MinimumLineCount)
        {
            return null;
        }

        var selfName = lines[SelfNameLineIndex].Trim();
        if (!string.IsNullOrWhiteSpace(expectedSelfName) &&
            !string.Equals(selfName, expectedSelfName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!TryReadFlag(lines[DefeatedLineIndex], out var defeated) ||
            !TryReadFlag(lines[VictoriousLineIndex], out var victorious) ||
            !TryReadFlag(lines[GameOverLineIndex], out var gameOver) ||
            !gameOver)
        {
            return null;
        }

        return (defeated, victorious) switch
        {
            (false, true) => PracticeSessionOutcome.PlayerWin,
            (true, false) => PracticeSessionOutcome.PlayerLoss,
            _ => null
        };
    }

    private static bool TryReadFlag(string text, out bool value)
    {
        value = false;
        if (!int.TryParse(text.Trim(), out var number))
        {
            return false;
        }

        value = number != 0;
        return true;
    }

    private static string[] ReadAllLinesShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split(["\r\n", "\n"], StringSplitOptions.None);
    }
}

public static partial class BotResultLogReader
{
    public static BotResultLogObservation? FindLatestPlayerOutcome(
        IEnumerable<string> searchRoots,
        DateTime sessionStartedAtUtc)
    {
        return searchRoots
            .Where(root => !string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
            .SelectMany(root => EnumerateFiles(root))
            .Where(file => file.LastWriteTimeUtc >= sessionStartedAtUtc.AddSeconds(-2))
            .Select(file => TryReadOutcome(file, sessionStartedAtUtc))
            .Where(observation => observation is not null)
            .Cast<BotResultLogObservation>()
            .OrderByDescending(observation => observation.LastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static IEnumerable<FileInfo> EnumerateFiles(string root)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System
        };

        foreach (var path in Directory.EnumerateFiles(root, "*.*", options))
        {
            var extension = System.IO.Path.GetExtension(path);
            if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".log", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                yield return new FileInfo(path);
            }
        }
    }

    private static BotResultLogObservation? TryReadOutcome(FileInfo file, DateTime sessionStartedAtUtc)
    {
        PracticeSessionOutcome? outcomeFromName = null;
        if (file.Name.Contains("DefeatResult", StringComparison.OrdinalIgnoreCase))
        {
            outcomeFromName = PracticeSessionOutcome.PlayerWin;
        }
        else if (file.Name.Contains("VictoryResult", StringComparison.OrdinalIgnoreCase))
        {
            outcomeFromName = PracticeSessionOutcome.PlayerLoss;
        }

        try
        {
            var text = File.ReadAllText(file.FullName);
            var outcome = ParsePlayerOutcomeFromAiText(text) ?? outcomeFromName;
            return outcome is null
                ? null
                : new BotResultLogObservation(outcome.Value, file.FullName, file.LastWriteTimeUtc);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static PracticeSessionOutcome? ParsePlayerOutcomeFromAiText(string text)
    {
        var gameEnded = GameEndedResultRegex().Matches(text);
        if (gameEnded.Count > 0)
        {
            return AiResultToPlayerOutcome(gameEnded[^1].Groups["result"].Value);
        }

        var isWinner = IsWinnerRegex().Matches(text);
        if (isWinner.Count > 0)
        {
            return isWinner[^1].Groups["winner"].Value == "1"
                ? PracticeSessionOutcome.PlayerLoss
                : PracticeSessionOutcome.PlayerWin;
        }

        var result = JsonLikeResultRegex().Matches(text);
        if (result.Count > 0)
        {
            return AiResultToPlayerOutcome(result[^1].Groups["result"].Value);
        }

        return null;
    }

    private static PracticeSessionOutcome? AiResultToPlayerOutcome(string aiResult)
    {
        return aiResult.Trim().ToUpperInvariant() switch
        {
            "WIN" or "WON" => PracticeSessionOutcome.PlayerLoss,
            "LOSS" or "LOST" or "LOSE" => PracticeSessionOutcome.PlayerWin,
            "DRAW" or "TIE" => PracticeSessionOutcome.Draw,
            _ => null
        };
    }

    [GeneratedRegex(@"Game\s+Ended\.\s+Result:\s*(?<result>WIN|LOSS|DRAW)", RegexOptions.IgnoreCase)]
    private static partial Regex GameEndedResultRegex();

    [GeneratedRegex(@"""is_winner""\s*:\s*(?<winner>[01])", RegexOptions.IgnoreCase)]
    private static partial Regex IsWinnerRegex();

    [GeneratedRegex(@"""result""\s*:\s*""(?<result>won|lost|win|loss|draw|tie)""", RegexOptions.IgnoreCase)]
    private static partial Regex JsonLikeResultRegex();
}
