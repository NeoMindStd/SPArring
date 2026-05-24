using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace StarAI.PracticeClient.Core;

public sealed class PracticeConfigurator
{
    public const string BwapiIniRelativePath = "bwapi-data/bwapi.ini";
    public const string CoachAiConfigRelativePath = "bwapi-data/AnyRace_CoachAI.json";
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
                $"{settings.Bot.Name}는 이 PC의 StarCraft 오류 로그에서 액세스 위반 크래시가 확인되어 현재 차단되어 있습니다.",
                true));
        }

        var mapPath = Path.Combine(settings.StarCraftRoot, settings.Map.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(mapPath))
        {
            issues.Add(new ValidationIssue($"Map not found: {mapPath}", true));
        }

        if (settings.EnableCoachAi && CoachAiLocator.FindCoachAiDll(settings.StarCraftRoot) is null)
        {
            issues.Add(new ValidationIssue("CoachAI is not installed in this StarCraft folder yet.", false));
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

        var iniPath = Path.Combine(settings.StarCraftRoot, BwapiIniRelativePath.Replace('/', Path.DirectorySeparatorChar));
        EnsureBackup(iniPath);

        var ini = BwapiIni.Load(iniPath);
        ini.Set("ai", "ai", settings.Bot.RelativeDllPath);
        ApplyCommonSettings(ini, settings, settings.Map.RelativePath, settings.Bot.Race, settings.PlayerRace, soundOn: false, characterName: "StarAIBot");
        ini.Save(iniPath);
        ApplyBuildPatch(settings);
        EnsureBotRuntimeConfig(settings);

        return iniPath;
    }

    public string ApplyCoachClient(PracticeSettings settings, string coachAiDllPath)
    {
        var errors = Validate(settings)
            .Where(issue => issue.IsError && !issue.Message.StartsWith("Bot DLL not found:", StringComparison.Ordinal))
            .ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(issue => issue.Message)));
        }

        if (!File.Exists(coachAiDllPath))
        {
            throw new FileNotFoundException("CoachAI DLL not found.", coachAiDllPath);
        }

        var iniPath = Path.Combine(settings.StarCraftRoot, BwapiIniRelativePath.Replace('/', Path.DirectorySeparatorChar));
        EnsureBackup(iniPath);

        var relativeCoachPath = Path.GetRelativePath(settings.StarCraftRoot, coachAiDllPath).Replace('\\', '/');
        var ini = BwapiIni.Load(iniPath);
        ini.Set("ai", "ai", relativeCoachPath);
        ApplyCommonSettings(ini, settings, "", settings.PlayerRace, settings.Bot.Race, soundOn: true, characterName: "StarAIHuman");
        ini.Save(iniPath);
        EnsureCoachAiConfig(settings.StarCraftRoot);

        return iniPath;
    }

    public string ApplyPlayerHost(PracticeSettings settings, string? coachAiDllPath)
    {
        var errors = Validate(settings)
            .Where(issue => issue.IsError && !issue.Message.StartsWith("Bot DLL not found:", StringComparison.Ordinal))
            .ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(issue => issue.Message)));
        }

        var iniPath = Path.Combine(settings.StarCraftRoot, BwapiIniRelativePath.Replace('/', Path.DirectorySeparatorChar));
        EnsureBackup(iniPath);

        var ini = BwapiIni.Load(iniPath);
        if (!string.IsNullOrWhiteSpace(coachAiDllPath))
        {
            if (!File.Exists(coachAiDllPath))
            {
                throw new FileNotFoundException("CoachAI DLL not found.", coachAiDllPath);
            }

            var relativeCoachPath = Path.GetRelativePath(settings.StarCraftRoot, coachAiDllPath).Replace('\\', '/');
            ini.Set("ai", "ai", relativeCoachPath);
            EnsureCoachAiConfig(settings.StarCraftRoot);
        }
        else
        {
            ini.Set("ai", "ai", "");
        }

        ApplyCommonSettings(ini, settings, settings.Map.RelativePath, settings.PlayerRace, settings.Bot.Race, soundOn: true, characterName: "StarAIHuman");
        ini.Save(iniPath);

        return iniPath;
    }

    public string ApplyBotJoin(PracticeSettings settings, string? firstInstanceAiDllPath = null)
    {
        var errors = Validate(settings).Where(issue => issue.IsError).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(issue => issue.Message)));
        }

        var iniPath = Path.Combine(settings.StarCraftRoot, BwapiIniRelativePath.Replace('/', Path.DirectorySeparatorChar));
        EnsureBackup(iniPath);

        var ini = BwapiIni.Load(iniPath);
        ini.Set("ai", "ai", settings.Bot.RelativeDllPath);

        ApplyCommonSettings(ini, settings, "", settings.Bot.Race, settings.PlayerRace, soundOn: false, characterName: "StarAIBot");

        ini.Save(iniPath);
        ApplyBuildPatch(settings);
        EnsureBotRuntimeConfig(settings);

        return iniPath;
    }

    public static string EnsureCoachAiConfig(string starCraftRoot)
    {
        var path = Path.Combine(starCraftRoot, CoachAiConfigRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            File.WriteAllText(path, DefaultCoachAiConfigJson, new System.Text.UTF8Encoding(false));
            ForceCoachAiManualWorkerSettings(path);
        }
        else
        {
            ForceCoachAiManualWorkerSettings(path);
        }

        return path;
    }

    public static void ApplyCoachAiBuildPreset(string starCraftRoot, CoachAiBuildPreset preset)
    {
        if (preset.Id == CoachAiBuildPresets.KeepExisting.Id)
        {
            return;
        }

        var path = EnsureCoachAiConfig(starCraftRoot);
        var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? new JsonObject();
        var presetPlan = root["Preset Plan"] as JsonObject;
        if (presetPlan is null)
        {
            presetPlan = new JsonObject();
            root["Preset Plan"] = presetPlan;
        }

        var steps = new JsonArray();
        foreach (var step in preset.Steps)
        {
            steps.Add(step);
        }

        presetPlan["TimedBo1 Title"] = preset.TitleForOverlay;
        presetPlan["TimedBo1"] = steps;
        presetPlan["TimedBo1 Tips"] = preset.Tips;

        var options = CreateIndentedJsonOptions();
        File.WriteAllText(path, root.ToJsonString(options), new System.Text.UTF8Encoding(false));
    }

    private static void EnsureBackup(string path)
    {
        var backup = path + ".starai-original";
        if (!File.Exists(backup))
        {
            File.Copy(path, backup);
        }
    }

    private void ApplyCommonSettings(BwapiIni ini, PracticeSettings settings, string mapRelativePath, Race race, Race enemyRace, bool soundOn, string characterName)
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

    private static void ForceCoachAiManualWorkerSettings(string path)
    {
        var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? new JsonObject();
        var controlPanel = root["Control Panel"] as JsonObject;
        if (controlPanel is null)
        {
            controlPanel = new JsonObject();
            root["Control Panel"] = controlPanel;
        }

        controlPanel["autoTrainWorkers"] = false;
        controlPanel["maxWorkers"] = 200;
        controlPanel["autoMine"] = false;
        controlPanel["autoBuildSuppliesBeforeBlocked"] = -200;
        controlPanel["workerCutWarningEvery"] = 8;
        controlPanel["idleWorkerWarningEvery"] = 8;
        controlPanel["idleProductionBuildingWarningEvery"] = 30;
        controlPanel["idleFightingUnitWarningEvery"] = 60;
        controlPanel["totalTimeOnScreenOrSelectionAbove"] = 9999;
        controlPanel["sameScreenWarningEvery"] = 9999;
        controlPanel["sameSelectionWarningEvery"] = 9999;
        controlPanel["logSupplyProduction"] = false;
        controlPanel["logUnitsProduction"] = false;
        controlPanel["showMultitaskStats"] = false;
        controlPanel["workersCutCalculationPeriod"] = 999999;
        controlPanel["dontDrift"] = -1;
        controlPanel["workerCutLimit"] = 999999;
        controlPanel["workerCutLimitForOnce"] = -1;
        controlPanel["spend_more_minerals_WarningFor"] = 0;
        controlPanel["stickyScreen"] = 1;
        controlPanel["autoGameSpeed"] = false;
        controlPanel["replayAutoMoveToScanOrStorm"] = false;
        controlPanel["debug"] = false;
        controlPanel["TTSname"] = "";
        controlPanel["TTSspeed"] = 0;

        var options = CreateIndentedJsonOptions();
        File.WriteAllText(path, root.ToJsonString(options), new System.Text.UTF8Encoding(false));
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
        File.WriteAllText(configPath, root.ToJsonString(options), new System.Text.UTF8Encoding(false));
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

    private const string DefaultCoachAiConfigJson = """
        {
          "Control Panel": {
            "autoTrainWorkers": false,
            "maxWorkers": 200,
            "autoMine": false,
            "autoBuildSuppliesBeforeBlocked": -200,
            "maxProductionBuildingQueue": 2,
            "workerCutWarningEvery": 8,
            "idleWorkerWarningEvery": 8,
            "idleProductionBuildingWarningEvery": 30,
            "idleFightingUnitWarningEvery": 60,
            "totalTimeOnScreenOrSelectionAbove": 9999,
            "sameScreenWarningEvery": 9999,
            "sameSelectionWarningEvery": 9999,
            "logSupplyProduction": false,
            "logUnitsProduction": false,
            "showMultitaskStats": false,
            "workersCutCalculationPeriod": 999999,
            "replayLogUnitsFor": 420,
            "replayLogSupplyFor": 40,
            "dontDrift": -1,
            "workerCutLimit": 999999,
            "workerCutLimitForOnce": -1,
            "spend_more_minerals_WarningFor": 0,
            "mineralsAboveLog": 750,
            "stickyScreen": 1,
            "autoGameSpeed": false,
            "replayAutoMoveToScanOrStorm": false,
            "TTSname": "",
            "TTSspeed": 0,
            "debug": false
          },
          "Preset Plan": {
            "TimedBo Color1": 7,
            "TimedBo Color2": 2,
            "ReplayBo Color1": 7,
            "ReplayBo Color2": 2,
            "TimedBo1 Title": "StarAI Protoss recovery",
            "TimedBo1": [
              "00:00 Worker split and probe production",
              "00:09 8 Pylon",
              "01:05 10 Gateway",
              "01:35 12 Assimilator",
              "02:05 14 Cybernetics Core",
              "02:30 15 Pylon",
              "03:00 Start Dragoon",
              "03:15 Start range",
              "04:00 Scout and decide expand or pressure"
            ],
            "TimedBo1 Tips": "Replace this with your exact PvT/PvP/PvZ build when ready.",
            "TimedBo2 Title": "StarAI Terran recovery",
            "TimedBo2": [
              "00:00 Worker split and SCV production",
              "00:45 8 Supply Depot",
              "01:15 10 Barracks",
              "01:50 11 Refinery",
              "02:20 13 Supply Depot",
              "02:45 Factory",
              "03:30 Machine Shop or expand plan"
            ],
            "TimedBo2 Tips": "Keep workers and production running. Edit this file for your real Terran build.",
            "TimedBo3 Title": "StarAI Zerg recovery",
            "TimedBo3": [
              "00:00 Worker split and drone production",
              "00:25 9 Overlord",
              "01:20 12 Hatchery or Pool by plan",
              "02:10 Extractor if needed",
              "02:35 Overlord",
              "03:00 Larva inject rhythm: spend larva before floating"
            ],
            "TimedBo3 Tips": "Use this as a placeholder until you enter a real Zerg build.",
            "Tips1": [
              "CoachAI focus: worker cut, idle production, minerals above 750.",
              "Use F12 to hide or show overlay.",
              "Use F11 to toggle sound.",
              "Use Ctrl+F1/F2/F3 to switch TimedBo."
            ],
            "Tips2": [
              "Keep enemy info features off for honest practice.",
              "Avoid autoTrainWorkers and autoMine for ladder-skill recovery."
            ],
            "Tips3": [
              "After the game, check where worker cuts and mineral floats happened."
            ]
          }
        }
        """;
}
