using System.Text.Json;
using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

internal sealed record PracticeClientSettings(
    string ReplayRoot,
    string UserMapRoot)
{
    public static PracticeClientSettings Defaults()
    {
        return new PracticeClientSettings(
            PracticeRuntimeOptions.Defaults().ReplayRoot,
            string.Empty);
    }
}

internal sealed class PracticeClientSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
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
