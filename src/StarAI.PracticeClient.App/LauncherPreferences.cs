using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.App;

internal sealed record LauncherPreferences
{
    public string? StarCraftRoot { get; init; }
    public Race PlayerRace { get; init; } = Race.Protoss;
    public Race? EnemyRace { get; init; } = Race.Terran;
    public DifficultyTier? Tier { get; init; }
    public string? BuildFilter { get; init; }
    public string Sort { get; init; } = "recommended";
    public string? Search { get; init; }
    public string? BotId { get; init; }
    public string? MapRelativePath { get; init; }
    public string? BotBuildId { get; init; }
    public string GameName { get; init; } = "AIPractice";
    public int? SpeedOverrideMs { get; init; } = 42;
    public bool PlayerFullscreen { get; init; } = true;
    public bool WindowedMode { get; init; } = true;
    public bool ConfineMouse { get; init; } = false;
    public bool ShowApmAlert { get; init; } = false;

    public static LauncherPreferences Load()
    {
        try
        {
            var path = Path();
            if (!File.Exists(path))
            {
                return new LauncherPreferences();
            }

            return JsonSerializer.Deserialize<LauncherPreferences>(File.ReadAllText(path), JsonOptions())
                   ?? new LauncherPreferences();
        }
        catch
        {
            return new LauncherPreferences();
        }
    }

    public void Save()
    {
        try
        {
            var path = Path();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions()));
        }
        catch
        {
            // Preference persistence should never block launching StarCraft.
        }
    }

    private static string Path() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AIStarClient",
        "preferences.json");

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
}
