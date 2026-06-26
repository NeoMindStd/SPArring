using Sparring.Client;
using Sparring.Core;

namespace Sparring.Tests;

public sealed class SparringSettingsTests
{
    [Fact]
    public void SettingsStorePersistsLastGameSelections()
    {
        using var workspace = new TempWorkspace();
        var store = new SparringSettingsStore(Path.Combine(workspace.Root, "settings.json"));
        var settings = new SparringSettings(
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
            LastMapName: "(4)Fighting Spirit",
            LastBotBuildId: "23_nexus",
            GameSpeedOverrideMs: 42,
            MouseScrollSpeed: 5,
            KeyboardScrollSpeed: 2,
            MouseSensitivity: 67);

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal("래더", loaded.LastMode);
        Assert.Equal("테란", loaded.LastEnemyRace);
        Assert.Equal("이름순", loaded.LastSort);
        Assert.Equal(StarCraftRace.Protoss, loaded.LastPlayerRace);
        Assert.Equal("Dragon", loaded.LastBotName);
        Assert.Equal("(4)Fighting Spirit", loaded.LastMapName);
        Assert.Equal("23_nexus", loaded.LastBotBuildId);
        Assert.Equal(42, loaded.GameSpeedOverrideMs);
        Assert.Equal(5, loaded.MouseScrollSpeed);
        Assert.Equal(2, loaded.KeyboardScrollSpeed);
        Assert.Equal(67, loaded.MouseSensitivity);
    }

    private sealed class TempWorkspace : IDisposable
    {
        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "Sparring.Tests", Guid.NewGuid().ToString("N"));
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
