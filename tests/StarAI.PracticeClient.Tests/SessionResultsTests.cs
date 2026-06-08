using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class SessionResultsTests
{
    [Fact]
    public void EloCalculatorAddsAndSubtractsPointsUsingExpectedScore()
    {
        var win = EloRatingCalculator.Calculate(1500, 1500, PracticeSessionOutcome.PlayerWin);
        var loss = EloRatingCalculator.Calculate(1500, 1500, PracticeSessionOutcome.PlayerLoss);

        Assert.Equal(1516, win.PlayerRatingAfter);
        Assert.Equal(16, win.Delta);
        Assert.Equal(1484, loss.PlayerRatingAfter);
        Assert.Equal(-16, loss.Delta);
    }

    [Fact]
    public void EloCalculatorGuaranteesAtLeastOnePointForAnyWin()
    {
        var win = EloRatingCalculator.Calculate(1453, 692, PracticeSessionOutcome.PlayerWin);

        Assert.Equal(1454, win.PlayerRatingAfter);
        Assert.Equal(1, win.Delta);
    }

    [Fact]
    public void RatingStoreCanSaveAndResetPlayerRating()
    {
        var path = Path.Combine(Path.GetTempPath(), "starai-rating", Guid.NewGuid().ToString("N"), "rating.json");
        var store = new PracticeLadderRatingStore(path);

        store.Save(1733);
        Assert.Equal(1733, store.Load().PlayerRating);

        store.Reset();
        Assert.Equal(EloRatingCalculator.DefaultRating, store.Load().PlayerRating);
    }

    [Fact]
    public void BotResultLogReaderInvertsAiWinLossIntoPlayerOutcome()
    {
        Assert.Equal(
            PracticeSessionOutcome.PlayerLoss,
            BotResultLogReader.ParsePlayerOutcomeFromAiText("Game Ended. Result: WIN"));
        Assert.Equal(
            PracticeSessionOutcome.PlayerWin,
            BotResultLogReader.ParsePlayerOutcomeFromAiText("{\"is_winner\":0}"));
        Assert.Equal(
            PracticeSessionOutcome.PlayerWin,
            BotResultLogReader.ParsePlayerOutcomeFromAiText("{\"result\":\"lost\"}"));
    }

    [Fact]
    public void BotResultLogReaderFindsLatestChangedResultFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-result-logs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var startedAt = DateTime.UtcNow;
        var path = Path.Combine(root, "bot.log");
        File.WriteAllText(path, "Game Ended. Result: LOSS");
        File.SetLastWriteTimeUtc(path, startedAt.AddSeconds(5));

        var observation = BotResultLogReader.FindLatestPlayerOutcome([root], startedAt);

        Assert.NotNull(observation);
        Assert.Equal(PracticeSessionOutcome.PlayerWin, observation.PlayerOutcome);
        Assert.Equal(path, observation.SourcePath);
    }

    [Fact]
    public void TournamentGameStateReaderParsesHumanVictory()
    {
        var outcome = TournamentGameStateReader.ParsePlayerOutcome(
            CreateGameStateLines(defeated: 0, victorious: 1, gameOver: 1));

        Assert.Equal(PracticeSessionOutcome.PlayerWin, outcome);
    }

    [Fact]
    public void TournamentGameStateReaderParsesHumanDefeat()
    {
        var outcome = TournamentGameStateReader.ParsePlayerOutcome(
            CreateGameStateLines(defeated: 1, victorious: 0, gameOver: 1));

        Assert.Equal(PracticeSessionOutcome.PlayerLoss, outcome);
    }

    [Fact]
    public void TournamentGameStateReaderIgnoresStaleGameStateFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-game-state", Guid.NewGuid().ToString("N"));
        var gameStatePath = Path.Combine(root, "bwapi-data", "gameState.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(gameStatePath)!);
        File.WriteAllLines(gameStatePath, CreateGameStateLines(defeated: 0, victorious: 1, gameOver: 1));

        var startedAt = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(gameStatePath, startedAt.AddMinutes(-5));

        var observation = TournamentGameStateReader.FindPlayerOutcome(root, startedAt);

        Assert.Null(observation);
    }

    [Fact]
    public void OutcomeResolverPrefersBotResultLog()
    {
        var observation = new BotResultLogObservation(
            PracticeSessionOutcome.PlayerWin,
            @"C:\runtime\bot.log",
            DateTime.UtcNow);

        var outcome = PracticeSessionOutcomeResolver.Resolve(
            PracticeSessionMode.Ladder,
            observation,
            "player-left-ingame:GameRoom");

        Assert.Equal(PracticeSessionOutcome.PlayerWin, outcome);
    }

    [Fact]
    public void OutcomeResolverUsesTournamentGameStateWhenBotResultIsMissing()
    {
        var observation = new TournamentGameStateObservation(
            PracticeSessionOutcome.PlayerWin,
            @"C:\runtime\bwapi-data\gameState.txt",
            DateTime.UtcNow);

        var outcome = PracticeSessionOutcomeResolver.Resolve(
            PracticeSessionMode.Ladder,
            null,
            observation,
            "player-left-ingame:GameRoom");

        Assert.Equal(PracticeSessionOutcome.PlayerWin, outcome);
    }

    [Theory]
    [InlineData("player-left-ingame:GameRoom")]
    [InlineData("player-process-exited")]
    public void OutcomeResolverDoesNotGuessLadderLossWithoutBotResult(string reason)
    {
        var outcome = PracticeSessionOutcomeResolver.Resolve(
            PracticeSessionMode.Ladder,
            null,
            reason);

        Assert.Equal(PracticeSessionOutcome.Unknown, outcome);
    }

    [Theory]
    [InlineData("player-left-ingame:GameRoom")]
    [InlineData("player-process-exited")]
    public void OutcomeResolverTreatsPlayerQuitAsSparringAbandonedWhenNoBotResultExists(string reason)
    {
        var outcome = PracticeSessionOutcomeResolver.Resolve(
            PracticeSessionMode.Sparring,
            null,
            reason);

        Assert.Equal(PracticeSessionOutcome.Abandoned, outcome);
    }

    private static string[] CreateGameStateLines(int defeated, int victorious, int gameOver)
    {
        return
        [
            "3.0.0",
            "StarAIHuman",
            "StarAIBot",
            "Fighting Spirit",
            "11286",
            "18908",
            "0",
            "474177",
            defeated.ToString(),
            victorious.ToString(),
            gameOver.ToString(),
            "0",
            "0",
            "0"
        ];
    }
}
