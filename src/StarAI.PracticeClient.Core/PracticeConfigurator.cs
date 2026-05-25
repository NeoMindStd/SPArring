using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace StarAI.PracticeClient.Core;

public sealed class PracticeConfigurator
{
    public const string BwapiIniRelativePath = "bwapi-data/bwapi.ini";
    public const string DefaultReplayRoot = @"D:\OneDrive\Documents\StarCraft\Maps\Replays\ai";

    private readonly string _replayRoot;

    public PracticeConfigurator(string? replayRoot = null)
    {
        _replayRoot = string.IsNullOrWhiteSpace(replayRoot) ? DefaultReplayRoot : replayRoot;
    }

    public IReadOnlyList<ValidationIssue> Validate(PracticeSettings settings)
    {
        var issues = new List<ValidationIssue>();
        if (!Directory.Exists(settings.StarCraftRoot))
        {
            issues.Add(new ValidationIssue($"StarCraft root does not exist: {settings.StarCraftRoot}", true));
            return issues;
        }

        var starCraftExe = Path.Combine(settings.StarCraftRoot, "StarCraft.exe");
        if (!File.Exists(starCraftExe))
        {
            issues.Add(new ValidationIssue($"StarCraft.exe not found: {starCraftExe}", true));
        }

        var bwapiIni = Path.Combine(settings.StarCraftRoot, BwapiIniRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(bwapiIni))
        {
            issues.Add(new ValidationIssue($"bwapi.ini not found: {bwapiIni}", true));
        }

        var botDll = settings.Bot.DllPath(settings.StarCraftRoot);
        if (!File.Exists(botDll))
        {
            issues.Add(new ValidationIssue($"Bot DLL not found: {botDll}", true));
        }

        if (PracticeCatalog.IsKnownUnstableBot(settings.Bot))
        {
            issues.Add(new ValidationIssue(
                $"{settings.Bot.Name} is blocked because it produced local StarCraft access-violation crashes.",
                true));
        }

        var mapPath = Path.Combine(settings.StarCraftRoot, settings.Map.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(mapPath))
        {
            issues.Add(new ValidationIssue($"Map not found: {mapPath}", true));
        }

        return issues;
    }

    public string Apply(PracticeSettings settings)
    {
        var errors = Validate(settings).Where(issue => issue.IsError).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(issue => issue.Message)));
        }

        var iniPath = IniPath(settings.StarCraftRoot);
        EnsureBackup(iniPath);

        var ini = BwapiIni.Load(iniPath);
        ini.Set("ai", "ai", settings.Bot.RelativeDllPath);
        ApplyCommonSettings(
            ini,
            settings,
            settings.Map.RelativePath,
            settings.Bot.Race,
            settings.PlayerRace,
            soundOn: false,
            characterName: "StarAIBot");
        ini.Save(iniPath);
        ApplyBuildPatch(settings);
        EnsureBotRuntimeConfig(settings);

        return iniPath;
    }

    public string ApplyPlayerHost(PracticeSettings settings)
    {
        var errors = Validate(settings)
            .Where(issue => issue.IsError && !issue.Message.StartsWith("Bot DLL not found:", StringComparison.Ordinal))
            .ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(issue => issue.Message)));
        }

        var iniPath = IniPath(settings.StarCraftRoot);
        EnsureBackup(iniPath);

        var ini = BwapiIni.Load(iniPath);
        ini.Set("ai", "ai", "");
        ApplyCommonSettings(
            ini,
            settings,
            settings.Map.RelativePath,
            settings.PlayerRace,
            settings.Bot.Race,
            soundOn: true,
            characterName: "StarAIHuman");
        ini.Save(iniPath);

        return iniPath;
    }

    public string ApplyBotJoin(PracticeSettings settings)
    {
        var errors = Validate(settings).Where(issue => issue.IsError).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(issue => issue.Message)));
        }

        var iniPath = IniPath(settings.StarCraftRoot);
        EnsureBackup(iniPath);

        var ini = BwapiIni.Load(iniPath);
        ini.Set("ai", "ai", settings.Bot.RelativeDllPath);
        ApplyCommonSettings(
            ini,
            settings,
            mapRelativePath: "",
            race: settings.Bot.Race,
            enemyRace: settings.PlayerRace,
            soundOn: false,
            characterName: "StarAIBot");
        ini.Save(iniPath);
        ApplyBuildPatch(settings);
        EnsureBotRuntimeConfig(settings);

        return iniPath;
    }

    private static string IniPath(string starCraftRoot)
    {
        return Path.Combine(starCraftRoot, BwapiIniRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void EnsureBackup(string path)
    {
        var backup = path + ".starai-original";
        if (!File.Exists(backup))
        {
            File.Copy(path, backup);
        }
    }

    private void ApplyCommonSettings(
        BwapiIni ini,
        PracticeSettings settings,
        string mapRelativePath,
        Race race,
        Race enemyRace,
        bool soundOn,
        string characterName)
    {
        Directory.CreateDirectory(_replayRoot);

        ini.Set("auto_menu", "auto_menu", "LAN");
        ini.Set("auto_menu", "character_name", characterName);
        ini.Set("auto_menu", "lan_mode", "Local PC");
        ini.Set("auto_menu", "map", mapRelativePath);
        ini.Set("auto_menu", "game", settings.GameName);
        ini.Set("auto_menu", "game_type", "MELEE");
        ini.Set("auto_menu", "race", race.ToString());
        ini.Set("auto_menu", "enemy_race", enemyRace.ToString());
        ini.Set("auto_menu", "enemy_race_1", "Default");
        ini.Set("auto_menu", "enemy_race_2", "Default");
        ini.Set("auto_menu", "enemy_race_3", "Default");
        ini.Set("auto_menu", "enemy_race_4", "Default");
        ini.Set("auto_menu", "enemy_race_5", "Default");
        ini.Set("auto_menu", "enemy_race_6", "Default");
        ini.Set("auto_menu", "enemy_race_7", "Default");
        ini.Set("auto_menu", "save_replay", ReplaySavePattern());
        ini.Set("auto_menu", "wait_for_min_players", "2");
        ini.Set("auto_menu", "wait_for_max_players", "2");
        ini.Set("auto_menu", "wait_for_time", "5000");
        ini.Set("window", "windowed", settings.WindowedMode ? "ON" : "OFF");
        ini.Set("starcraft", "sound", soundOn ? "ON" : "OFF");
        ini.Set("starcraft", "speed_override", settings.SpeedOverrideMs?.ToString() ?? "-1");
    }

    private string ReplaySavePattern()
    {
        return Path.Combine(
            _replayRoot,
            "$Y $b $d",
            "StarAI_%BOTNAME6%_%MAP%_$H$M$S.rep");
    }

    private static void ApplyBuildPatch(PracticeSettings settings)
    {
        var patch = settings.BuildOption?.Patch;
        if (patch is null || patch.Kind == BuildPatchKind.None)
        {
            return;
        }

        var configPath = Path.Combine(settings.StarCraftRoot, patch.ConfigRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("Selected build config file was not found.", configPath);
        }

        EnsureBackup(configPath);

        var root = JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject
            ?? throw new InvalidOperationException($"Invalid JSON object: {configPath}");

        switch (patch.Kind)
        {
            case BuildPatchKind.UAlbertaRaceStrategy:
                ApplyUAlbertaPatch(root, settings.Bot.Race, patch.StrategyId);
                break;
            case BuildPatchKind.MatchupWeightedStrategy:
                ApplyWeightedPatch(root, settings.Bot.Race, settings.PlayerRace, patch.StrategyId);
                break;
            default:
                return;
        }

        var options = CreateIndentedJsonOptions();
        File.WriteAllText(configPath, root.ToJsonString(options), new UTF8Encoding(false));
    }

    private static void EnsureBotRuntimeConfig(PracticeSettings settings)
    {
        if (!settings.Bot.RelativeDllPath.Contains("Steamhammer", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relativeDllPath = settings.Bot.RelativeDllPath.Replace('/', Path.DirectorySeparatorChar);
        var dllDirectory = Path.GetDirectoryName(relativeDllPath);
        if (string.IsNullOrWhiteSpace(dllDirectory))
        {
            return;
        }

        var source = Path.Combine(settings.StarCraftRoot, dllDirectory, "Steamhammer_5.2.3.json");
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("Steamhammer config file was not found.", source);
        }

        var target = Path.Combine(settings.StarCraftRoot, "bwapi-data", "AI", "Steamhammer_5.2.3.json");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
    }

    private static void ApplyUAlbertaPatch(JsonObject root, Race botRace, string strategyId)
    {
        var strategy = RequireObject(root, "Strategy");
        strategy[botRace.ToString()] = strategyId;
        strategy["UseEnemySpecificStrategy"] = false;
    }

    private static void ApplyWeightedPatch(JsonObject root, Race botRace, Race playerRace, string strategyId)
    {
        var strategy = RequireObject(root, "Strategy");
        var matchups = GetMatchups(botRace, playerRace);
        var botRaceKey = botRace.ToString();
        var patched = false;

        foreach (var matchup in matchups)
        {
            if (strategy[matchup] is not JsonObject matchupObject)
            {
                continue;
            }

            if (matchupObject[botRaceKey] is not JsonArray)
            {
                continue;
            }

            matchupObject[botRaceKey] = new JsonArray(new JsonObject
            {
                ["Weight"] = 100,
                ["Strategy"] = strategyId
            });
            patched = true;
        }

        if (!patched)
        {
            throw new InvalidOperationException($"Strategy '{strategyId}' could not be applied for {botRace} vs {playerRace}.");
        }
    }

    private static JsonObject RequireObject(JsonObject parent, string propertyName)
    {
        return parent[propertyName] as JsonObject
            ?? throw new InvalidOperationException($"Missing JSON object: {propertyName}");
    }

    private static JsonSerializerOptions CreateIndentedJsonOptions() => new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private static IReadOnlyList<string> GetMatchups(Race botRace, Race playerRace)
    {
        var bot = RaceLetter(botRace);
        if (bot is null)
        {
            return Array.Empty<string>();
        }

        if (playerRace == Race.Random)
        {
            return new[] { $"{bot}vT", $"{bot}vP", $"{bot}vZ", $"{bot}vU" };
        }

        var player = RaceLetter(playerRace);
        return player is null ? Array.Empty<string>() : new[] { $"{bot}v{player}" };
    }

    private static string? RaceLetter(Race race)
    {
        return race switch
        {
            Race.Terran => "T",
            Race.Protoss => "P",
            Race.Zerg => "Z",
            _ => null
        };
    }
}
