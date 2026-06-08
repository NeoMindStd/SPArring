using System.Text.Json;
using System.Text.Json.Serialization;
using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

internal sealed record PracticeClientSettings(
    string ReplayRoot,
    string UserMapRoot,
    string LadderMapRoot = "",
    bool HideAiName = true,
    bool? UseBotNameAsAiCharacter = null)
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
            HideAiName: true);
    }
}

internal sealed class PracticeClientSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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

        return JsonSerializer.Deserialize<PracticeClientSettings>(File.ReadAllText(SettingsPath), JsonOptions)
            ?? PracticeClientSettings.Defaults();
    }

    public void Save(PracticeClientSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
