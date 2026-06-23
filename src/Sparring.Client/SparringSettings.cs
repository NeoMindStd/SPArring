using System.Text.Json;
using System.Text.Json.Serialization;
using Sparring.Core;

namespace Sparring.Client;

internal sealed record SparringSettings(
    string ReplayRoot,
    string UserMapRoot,
    string LadderMapRoot = "",
    bool HideAiName = true,
    bool? UseBotNameAsAiCharacter = null,
    string? LastMode = null,
    string? LastEnemyRace = null,
    string? LastSort = null,
    StarCraftRace? LastPlayerRace = null,
    string? LastBotName = null,
    string? LastMapName = null,
    int GameSpeedOverrideMs = -1,
    int MouseScrollSpeed = 3,
    int KeyboardScrollSpeed = 3,
    string? SkippedUpdateVersion = null)
{
    [JsonIgnore]
    public bool EffectiveHideAiName => UseBotNameAsAiCharacter is { } showBotName
        ? !showBotName
        : HideAiName;

    public static SparringSettings Defaults()
    {
        return new SparringSettings(
            PracticeRuntimeOptions.Defaults().ReplayRoot,
            string.Empty,
            RemasteredLadderMapCatalogReader.DefaultDirectory(),
            HideAiName: true,
            LastMode: "스파링",
            LastEnemyRace: "모두",
            LastSort: "ELO 높은순",
            LastPlayerRace: StarCraftRace.Protoss);
    }
}

internal sealed class SparringSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public SparringSettingsStore(string settingsPath)
    {
        SettingsPath = settingsPath;
    }

    public string SettingsPath { get; }

    public static SparringSettingsStore Default()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Sparring");
        return new SparringSettingsStore(Path.Combine(root, "settings.json"));
    }

    public SparringSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return SparringSettings.Defaults();
        }

        try
        {
            return JsonSerializer.Deserialize<SparringSettings>(File.ReadAllText(SettingsPath), JsonOptions)
                ?? SparringSettings.Defaults();
        }
        catch (JsonException)
        {
            return SparringSettings.Defaults();
        }
    }

    public void Save(SparringSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
