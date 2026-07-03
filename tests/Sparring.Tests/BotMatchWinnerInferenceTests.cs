using Sparring.Client;

namespace Sparring.Tests;

public sealed class BotMatchWinnerInferenceTests : IDisposable
{
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), "Sparring.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void PrefersClientThatStayedInGameAfterOpponentLeft()
    {
        var inference = SmokeChecks.InferBotMatchWinner(
            StarCraftScreenState.GameRoom,
            StarCraftScreenState.InGame,
            [],
            "NeoZergE",
            "McRave");

        Assert.Equal("right", inference.WinnerSide);
        Assert.Equal("right-still-in-game", inference.Reason);
    }

    [Fact]
    public void UsesLatestReplayFileWhenBothClientsLeftGame()
    {
        Directory.CreateDirectory(_workspace);
        var leftReplay = Path.Combine(_workspace, "Sparring_NeoZer_Fighting_120000.rep");
        var rightReplay = Path.Combine(_workspace, "Sparring_McRave_Fighting_120004.rep");
        File.WriteAllText(leftReplay, "left");
        File.WriteAllText(rightReplay, "right");
        File.SetLastWriteTimeUtc(leftReplay, new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(rightReplay, new DateTime(2026, 7, 3, 12, 0, 4, DateTimeKind.Utc));

        var inference = SmokeChecks.InferBotMatchWinner(
            StarCraftScreenState.GameRoom,
            StarCraftScreenState.GameRoom,
            [leftReplay, rightReplay],
            "NeoZergE",
            "McRave");

        Assert.Equal("right", inference.WinnerSide);
        Assert.Equal("latest-replay-file", inference.Reason);
    }

    [Fact]
    public void LeavesWinnerUnknownWhenSignalsAreAmbiguous()
    {
        var inference = SmokeChecks.InferBotMatchWinner(
            StarCraftScreenState.GameRoom,
            StarCraftScreenState.GameRoom,
            [],
            "NeoProtossE",
            "NeoProtossF");

        Assert.Equal("unknown", inference.WinnerSide);
        Assert.Equal("insufficient-signal", inference.Reason);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspace))
        {
            Directory.Delete(_workspace, recursive: true);
        }
    }
}
