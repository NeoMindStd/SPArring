using System.Text.Json;
using System.Text.Json.Serialization;
using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

internal sealed record PracticeClientSettings(
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
    string? LastMapName = null)
{
    [JsonIgnore]
    public bool EffectiveHideAiName => UseBotNameAsAiCharacter is { } showBotName
        ? !showBotName
        : HideAiName;

    public static PracticeClientSettings Defaults()
    {
        return new PracticeClientSettings(
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

internal sealed class PracticeClientSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public PracticeClientSettingsStore(string settingsPath)
    {
        SettingsPath = settingsPath;
    }

    public string SettingsPath { get; }

    public static PracticeClientSettingsStore Default()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StarAI.PracticeClient");
        return new PracticeClientSettingsStore(Path.Combine(root, "settings.json"));
    }

    public PracticeClientSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return PracticeClientSettings.Defaults();
        }

        try
        {
            return JsonSerializer.Deserialize<PracticeClientSettings>(File.ReadAllText(SettingsPath), JsonOptions)
                ?? PracticeClientSettings.Defaults();
        }
        catch (JsonException)
        {
            return PracticeClientSettings.Defaults();
        }
    }

    public void Save(PracticeClientSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
