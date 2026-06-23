using StarAI.PracticeClient.App;
using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class PracticeClientSettingsTests
{
    [Fact]
    public void SettingsStorePersistsLastGameSelections()
    {
        using var workspace = new TempWorkspace();
        var store = new PracticeClientSettingsStore(Path.Combine(workspace.Root, "settings.json"));
        var settings = new PracticeClientSettings(
            ReplayRoot: @"C:\replays",
            UserMapRoot: @"C:\maps",
            LadderMapRoot: @"C:\ladder",
            HideAiName: true,
            UseBotNameAsAiCharacter: null,
            LastMode: "래더",
            LastEnemyRace: "테란",
            LastSort: "이름순",
            LastPlayerRace: StarCraftRace.Protoss,
            LastBotName: "Dragon",
            LastMapName: "(4)Fighting Spirit");

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal("래더", loaded.LastMode);
        Assert.Equal("테란", loaded.LastEnemyRace);
        Assert.Equal("이름순", loaded.LastSort);
        Assert.Equal(StarCraftRace.Protoss, loaded.LastPlayerRace);
        Assert.Equal("Dragon", loaded.LastBotName);
        Assert.Equal("(4)Fighting Spirit", loaded.LastMapName);
    }

    private sealed class TempWorkspace : IDisposable
    {
        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "StarAI.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
                // Test cleanup is best-effort.
            }
        }
    }
}
