using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class PracticeSessionHistoryStoreTests
{
    [Fact]
    public void UpsertPersistsAndUpdatesSessionById()
    {
        var path = Path.Combine(Path.GetTempPath(), "starai-history", Guid.NewGuid().ToString("N"), "history.json");
        var store = new PracticeSessionHistoryStore(path);
        var id = Guid.NewGuid();
        var first = CreateRecord(id, apm: 12);
        var updated = first with { ActionsPerMinute = 90, ActionCount = 30 };

        store.Upsert(first);
        store.Upsert(updated);

        var records = store.Load();
        Assert.Single(records);
        Assert.Equal(90, records[0].ActionsPerMinute);
        Assert.Equal(30, records[0].ActionCount);
    }

    [Fact]
    public void LoadReturnsNewestSessionFirst()
    {
        var path = Path.Combine(Path.GetTempPath(), "starai-history", Guid.NewGuid().ToString("N"), "history.json");
        var store = new PracticeSessionHistoryStore(path);
        var older = CreateRecord(Guid.NewGuid(), apm: 10) with { StartedAtUtc = DateTime.UtcNow.AddMinutes(-10) };
        var newer = CreateRecord(Guid.NewGuid(), apm: 20) with { StartedAtUtc = DateTime.UtcNow };

        store.Upsert(older);
        store.Upsert(newer);

        Assert.Equal(newer.Id, store.Load()[0].Id);
    }

    private static PracticeSessionRecord CreateRecord(Guid id, int apm)
    {
        return new PracticeSessionRecord(
            Id: id,
            StartedAtUtc: DateTime.UtcNow,
            LastUpdatedAtUtc: DateTime.UtcNow,
            BotName: "Bot",
            BotRace: StarCraftRace.Terran,
            MapName: "Map",
            MapFileName: "Map.scx",
            PlayerRace: StarCraftRace.Protoss,
            ReplayRoot: @"D:\Replays",
            ActionCount: apm,
            ActionsPerMinute: apm,
            DurationSeconds: 60);
    }
}
