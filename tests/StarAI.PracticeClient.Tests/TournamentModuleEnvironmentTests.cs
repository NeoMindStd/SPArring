using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class TournamentModuleEnvironmentTests
{
    [Fact]
    public void ForPlayerRuntimeDisablesTournamentDrawOverlays()
    {
        var variables = TournamentModuleEnvironment.ForPlayerRuntime();

        Assert.Equal("false", variables["TM_DISABLE_USER_INPUT"]);
        Assert.Equal("true", variables["TM_DISABLE_DRAW_GAME_TIMER"]);
        Assert.Equal("false", variables["TM_DRAW_UNIT_INFO"]);
        Assert.Equal("false", variables["TM_DRAW_BOT_NAMES"]);
        Assert.Equal("false", variables["TM_DRAW_TOURNAMENT_INFO"]);
        Assert.Equal("true", variables["TM_DISABLE_USER_INPUT_LOCAL_SPEED"]);
        Assert.Equal("42", variables["TM_LOCAL_SPEED"]);
        Assert.Equal("false", variables["TM_AUTO_OBS"]);
        Assert.Equal(@"bwapi-data\gameState.txt", variables["TM_STATE_FILE"]);
    }
}
