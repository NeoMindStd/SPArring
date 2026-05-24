using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public class PracticeSelectionTests
{
    [Fact]
    public void FindMapIndex_UsesSavedMapOnInitialLoad()
    {
        var maps = new[]
        {
            new MapProfile("Benzene", "maps/(2)Benzene.scx", 2),
            new MapProfile("Fighting Spirit", "maps/(4)Fighting Spirit.scx", 4),
            new MapProfile("Circuit Breaker", "maps/(4)Circuit Breaker.scx", 4)
        };

        var index = PracticeSelection.FindMapIndex(maps, "maps/(4)Circuit Breaker.scx");

        Assert.Equal(2, index);
    }

    [Fact]
    public void FindMapIndex_FallsBackToFightingSpiritWhenSavedMapIsMissing()
    {
        var maps = new[]
        {
            new MapProfile("Benzene", "maps/(2)Benzene.scx", 2),
            new MapProfile("Fighting Spirit", "maps/(4)Fighting Spirit.scx", 4)
        };

        var index = PracticeSelection.FindMapIndex(maps, "maps/missing.scx");

        Assert.Equal(1, index);
    }
}
