using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public sealed class HotkeyEntry
{
    public int StringId { get; set; }
    public string CommandId { get; set; } = string.Empty;
    public string Hotkey { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultText { get; set; } = string.Empty;
    public string CurrentText { get; set; } = string.Empty;
}

public sealed class HotkeyApplyResult
{
    public bool AppliedMpq { get; init; }
    public string WorkingCsvPath { get; init; } = string.Empty;
    public string? PatchedTblPath { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed partial class HotkeyCsvStore
{
    public const string RelativeWorkingCsvPath = @"bwapi-data\read\sc_hotkeys.csv";

    public IReadOnlyList<HotkeyEntry> Load(string csvPath, string? messagesPath = null)
    {
        if (!File.Exists(csvPath))
        {
            return [];
        }

        var descriptions = LoadDescriptions(messagesPath);
        return File.ReadAllLines(csvPath, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => ParseLine(line, descriptions))
            .Where(entry => entry is not null)
            .Cast<HotkeyEntry>()
            .OrderBy(entry => entry.CommandId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string SaveWorkingCopy(string runtimeRoot, IReadOnlyList<HotkeyEntry> entries)
    {
        var path = Path.Combine(runtimeRoot, RelativeWorkingCsvPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, entries.Select(Serialize), Encoding.UTF8);
        return path;
    }

    public string ImportFromDefaultAssets(PracticePaths paths, string runtimeRoot)
    {
        var source = PracticeAssetPaths.DefaultHotkeyCsv(paths);
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("StarAI bundled hotkey CSV was not found.", source);
        }

        var target = Path.Combine(runtimeRoot, RelativeWorkingCsvPath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
        return target;
    }

    public static HotkeyEntry? ParseLine(string line, IReadOnlyDictionary<string, string>? descriptions = null)
    {
        var parts = line.Split(',');
        if (parts.Length < 3 || !int.TryParse(parts[0].Trim(), out var stringId))
        {
            return null;
        }

        var commandId = parts[2].Trim();
        var currentText = parts.Length >= 4 ? parts[3].Trim() : parts[1].Trim();
        var hotkey = ExtractHotkey(currentText);
        string? description = null;
        descriptions?.TryGetValue($"hotkey_desc_{commandId}", out description);

        return new HotkeyEntry
        {
            StringId = stringId,
            CommandId = commandId,
            Hotkey = hotkey,
            Description = string.IsNullOrWhiteSpace(description)
                ? commandId.Replace('_', ' ')
                : description,
            DefaultText = parts[1].Trim(),
            CurrentText = currentText
        };
    }

    public static string Serialize(HotkeyEntry entry)
    {
        var rendered = RenderGameText(entry);
        return $"{entry.StringId},{entry.DefaultText},{entry.CommandId},{rendered}";
    }

    public static string RenderGameText(HotkeyEntry entry)
    {
        var key = NormalizeHotkey(entry.Hotkey);
        var upper = key.ToUpperInvariant();
        var description = SanitizeDescription(entry.Description);
        return $"{key}<1>{description}(<3>{upper}<1>)<0>";
    }

    private static string ExtractHotkey(string text)
    {
        var match = LeadingHotkeyRegex().Match(text);
        return match.Success ? match.Groups["key"].Value.ToLowerInvariant() : string.Empty;
    }

    private static string NormalizeHotkey(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrEmpty(trimmed) ? " " : trimmed[..1].ToLowerInvariant();
    }

    private static string SanitizeDescription(string value)
    {
        return value.Replace(",", "", StringComparison.Ordinal).Trim();
    }

    private static Dictionary<string, string> LoadDescriptions(string? messagesPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(messagesPath) || !File.Exists(messagesPath))
        {
            return result;
        }

        foreach (var line in File.ReadAllLines(messagesPath, Encoding.UTF8))
        {
            var index = line.IndexOf('=');
            if (index <= 0)
            {
                continue;
            }

            result[line[..index].Trim()] = line[(index + 1)..].Trim();
        }

        return result;
    }

    [GeneratedRegex("^(?<key>.)<")]
    private static partial Regex LeadingHotkeyRegex();
}

public sealed record RemasteredHotkeyImportResult(
    string SourcePath,
    int ParsedCount,
    int UpdatedCount);

public static class RemasteredHotkeyImporter
{
    private static readonly IReadOnlyDictionary<string, string[]> CommandIdsByRemasteredKey =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["STR_ATTACK"] = ["general_cmd_attack"],
            ["STR_RETURN"] = ["general_cmd_cargo"],
            ["STR_GATHER"] = ["general_cmd_gather"],
            ["STR_HOLD"] = ["general_cmd_holdpos"],
            ["STR_LAND"] = ["general_cmd_land"],
            ["STR_LIFTOFF"] = ["general_cmd_liftoff"],
            ["STR_PICKUP"] = ["general_cmd_load"],
            ["STR_MOVE"] = ["general_cmd_move"],
            ["STR_PATROL"] = ["general_cmd_patrol"],
            ["STR_RALLYPOINT"] = ["general_cmd_rally"],
            ["STR_STOP"] = ["general_cmd_stop"],
            ["STR_UNLOAD"] = ["general_cmd_unload"],

            ["STR_USESTIM"] = ["terran_ab_usestim"],
            ["STR_USEMAGNA"] = ["terran_ab_lockdown"],
            ["STR_USEMINES"] = ["terran_ab_spidermines"],
            ["STR_SCANNERSWEEP"] = ["terran_ab_scanner"],
            ["STR_SIEGE_MODE"] = ["terran_ab_siege"],
            ["STR_TANK_MODE"] = ["terran_ab_unsiege"],
            ["STR_DEFMTX"] = ["terran_ab_defmatrix"],
            ["STR_USEEMP"] = ["terran_ab_emp"],
            ["STR_IRRADIATE"] = ["terran_ab_irradiate"],
            ["STR_YAMATO"] = ["terran_ab_yamato"],
            ["STR_CLOAK"] = ["terran_ab_cloak"],
            ["STR_DECLOAK"] = ["terran_ab_decloak"],
            ["STR_REPAIR"] = ["terran_cmd_repair"],
            ["STR_NUKESTRIKE"] = ["terran_cmd_nuke"],
            ["STR_HEAL"] = ["terran_cmd_heal"],
            ["STR_CURE"] = ["terran_cmd_restoration"],
            ["STR_MYOPIA"] = ["terran_cmd_optflare"],
            ["STR_BUILD"] = ["terran_cmd_buildstruc", "zerg_cmd_buildstruc"],
            ["STR_BLD_ADVANCED"] = ["terran_cmd_buildadvstruc"],
            ["STR_MUTATE"] = ["zerg_cmd_buildstruc"],
            ["STR_MUTATE_ADV"] = ["zerg_cmd_buildadvstruc"],
            ["STR_MORPH_ADV"] = ["zerg_cmd_buildadvstruc"],

            ["STR_RSRCH_STIM"] = ["terran_res_stimpack"],
            ["STR_RSRCH_MAGNA"] = ["terran_res_lockdown"],
            ["STR_RSRCH_EMP"] = ["terran_res_emp"],
            ["STR_RSRCH_MINES"] = ["terran_res_spidermines"],
            ["STR_RSRCH_SIEGE"] = ["terran_res_siegetech"],
            ["STR_RSRCH_DEFMTX"] = ["terran_res_defmatrix"],
            ["STR_RSRCH_IRRADIATE"] = ["terran_res_irradiate"],
            ["STR_RSRCH_YAMATO"] = ["terran_res_yamato"],
            ["STR_RSRCH_SHIP_CLOAK"] = ["terran_res_wraithcloak"],
            ["STR_RSRCH_MAN_CLOAK"] = ["terran_res_ghostcloak"],
            ["STR_RSRCH_CURE"] = ["terran_res_restoration"],
            ["STR_RSRCH_MYOPIA"] = ["terran_res_opticalflare"],
            ["STR_UP_MARINE_GUN_RANGE"] = ["terran_res_u238"],
            ["STR_UP_VULTURE_SPEED"] = ["terran_res_vulturespeed"],
            ["STR_UP_VESSEL_ENERGY"] = ["terran_res_vesselenergy"],
            ["STR_UP_GHOST_SIGHT"] = ["terran_res_ghostsight"],
            ["STR_UP_GHOST_ENERGY"] = ["terran_res_ghostenergy"],
            ["STR_UP_WRAITH_ENERGY"] = ["terran_res_wraithenergy"],
            ["STR_UP_CRUISER_ENERGY"] = ["terran_res_bcenergy"],
            ["STR_UP_MEDIC_ENERGY"] = ["terran_res_medicenergy"],
            ["STR_UP_T_MISSILE_BOOST"] = ["terran_res_charon"],
            ["STR_UP_T_ARMOR"] = ["terran_upg_infarmor"],
            ["STR_UP_T_VEHICLE_PLATING"] = ["terran_upg_vehicleplating"],
            ["STR_UP_T_SHIP_PLATING"] = ["terran_upg_shipplating"],
            ["STR_UP_T_MAN_GUNS"] = ["terran_upg_infweapons"],
            ["STR_UP_T_VEHICLE_GUNS"] = ["terran_upg_vehicleweapons"],
            ["STR_UP_T_SHIP_GUNS"] = ["terran_upg_shipweapons"],

            ["STR_MAKE_T_MARINE"] = ["terran_train_marine"],
            ["STR_MAKE_T_GHOST"] = ["terran_train_ghost"],
            ["STR_MAKE_T_FIREBAT"] = ["terran_train_firebat"],
            ["STR_MAKE_T_VULTURE"] = ["terran_train_vulture"],
            ["STR_MAKE_T_GOLIATH"] = ["terran_train_goliath"],
            ["STR_MAKE_T_TANK"] = ["terran_train_tank"],
            ["STR_MAKE_T_SCV"] = ["terran_train_scv"],
            ["STR_MAKE_T_WRAITH"] = ["terran_train_wraith"],
            ["STR_MAKE_T_VESSEL"] = ["terran_train_vessel"],
            ["STR_MAKE_T_DROPSHIP"] = ["terran_train_dropship"],
            ["STR_MAKE_T_BCRUISER"] = ["terran_train_battlecruiser"],
            ["STR_MAKE_T_MEDIC"] = ["terran_train_medic"],
            ["STR_MAKE_T_FRIGATE"] = ["terran_train_valkyrie"],
            ["STR_BLD_TCOMMANDCTR"] = ["terran_build_commandcenter"],
            ["STR_BLD_DEPOT"] = ["terran_build_supplydepot"],
            ["STR_BLD_REFINERY"] = ["terran_build_refinery"],
            ["STR_BLD_BARRACKS"] = ["terran_build_barracks"],
            ["STR_BLD_ENGINEERING"] = ["terran_build_engineeringbay"],
            ["STR_BLD_TURRET"] = ["terran_build_missileturret"],
            ["STR_BLD_ACADEMY"] = ["terran_build_academy"],
            ["STR_BLD_PILLBOX"] = ["terran_build_bunker"],
            ["STR_BLD_FACTORY"] = ["terran_build_factory"],
            ["STR_BLD_TSTARPORT"] = ["terran_build_starport"],
            ["STR_BLD_SCIENCE_FAC"] = ["terran_build_sciencefacility"],
            ["STR_BLD_ARMORY"] = ["terran_build_armory"],
            ["STR_BLD_COMSAT"] = ["terran_build_comsat"],
            ["STR_BLD_SILO"] = ["terran_build_nukesilo"],
            ["STR_BLD_DOCKS"] = ["terran_build_controltower"],
            ["STR_BLD_COVERT_OPS"] = ["terran_build_covertops"],
            ["STR_BLD_PHYSICS"] = ["terran_build_physicslab"],
            ["STR_BLD_MACHINE"] = ["terran_build_machineshop"],

            ["STR_BURROW"] = ["zerg_cmd_burrow"],
            ["STR_DEBURROW"] = ["zerg_cmd_unburrow"],
            ["STR_INFEST"] = ["zerg_cmd_infest_cc"],
            ["STR_INFBROOD"] = ["zerg_cmd_broodling"],
            ["STR_PLAGUE"] = ["zerg_cmode_plague"],
            ["STR_PARASITE"] = ["zerg_cmd_parasite"],
            ["STR_BLOODBOIL"] = ["zerg_cmd_darkswarm"],
            ["STR_CONSUME"] = ["zerg_cmd_consume"],
            ["STR_KERRIGAN_CONSUME"] = ["zerg_cmd_consume"],
            ["STR_ENSNARE"] = ["zerg_cmd_ensnare"],
            ["STR_SELECT_LARVA"] = ["zerg_cmd_selectlarva"],
            ["STR_NYDUS_EXIT"] = ["zerg_cmd_placenydusexit"],
            ["STR_GUARDIAN_ASPECT"] = ["zerg_train_guardian"],
            ["STR_DEVOURER_ASPECT"] = ["zerg_cmd_devourer"],
            ["STR_MAKE_Z_LURKER"] = ["zerg_cmd_lurker"],
            ["STR_RSRCH_BURROW"] = ["zerg_res_burrow"],
            ["STR_RSRCH_INFBROOD"] = ["zerg_res_broodling"],
            ["STR_RSRCH_PLAGUE"] = ["zerg_res_plague"],
            ["STR_RSRCH_PARASITE"] = ["zerg_res_parasite"],
            ["STR_RSRCH_BLOODBOIL"] = ["zerg_res_darkswarm"],
            ["STR_RSRCH_ENSNARE"] = ["zerg_res_ensnare"],
            ["STR_RSRCH_CONSUME"] = ["zerg_res_consume"],
            ["STR_RSRCH_LURKERASPECT"] = ["zerg_res_lurkeraspect"],
            ["STR_UP_Z_CARAPACE"] = ["zerg_upg_carapace"],
            ["STR_UP_Z_PLATING"] = ["zerg_upg_flyercarapace"],
            ["STR_UP_Z_MELEE_ATTACKS"] = ["zerg_upg_meleeattack"],
            ["STR_UP_Z_MISSILE_ATTACKS"] = ["zerg_upg_missileattack"],
            ["STR_UP_Z_FLYER_ATTACKS"] = ["zerg_upg_flyerattack"],
            ["STR_UP_OVERLORD_TRANSPORT"] = ["zerg_res_ventralsacs"],
            ["STR_UP_OVERLORD_SIGHT"] = ["zerg_res_antennae"],
            ["STR_UP_OVERLORD_SPEED"] = ["zerg_res_pneumcarapace"],
            ["STR_UP_ZERGLING_SPEED"] = ["zerg_res_metaboost"],
            ["STR_UP_ZERGLING_ATTACK_SPEED"] = ["zerg_res_adrenal"],
            ["STR_UP_HYDRALISK_SPEED"] = ["zerg_res_muscularaug"],
            ["STR_UP_HYDRALISK_ATTACK_RANGE"] = ["zerg_res_hydrarange"],
            ["STR_UP_QUEEN_ENERGY"] = ["zerg_res_queenenergy"],
            ["STR_UP_DEFILER_ENERGY"] = ["zerg_res_defilerenergy"],
            ["STR_UP_Z_ULTRA_SPEED"] = ["zerg_res_ultraspeed"],
            ["STR_UP_Z_ULTRA_ARMOR"] = ["zerg_res_ultraarmor"],
            ["STR_MAKE_Z_ZERGLING"] = ["zerg_train_zergling"],
            ["STR_MAKE_Z_HYDRALISK"] = ["zerg_train_hydra"],
            ["STR_MAKE_Z_ULTRALISK"] = ["zerg_train_ultra"],
            ["STR_MAKE_Z_DRONE"] = ["zerg_train_drone"],
            ["STR_MAKE_Z_OVERLORD"] = ["zerg_train_overlord"],
            ["STR_MAKE_Z_MUTALID"] = ["zerg_train_mutalisk"],
            ["STR_MAKE_Z_QUEEN"] = ["zerg_train_queen"],
            ["STR_MAKE_Z_DEFILER"] = ["zerg_train_defiler"],
            ["STR_MAKE_Z_AVENGER"] = ["zerg_train_scourge"],
            ["STR_MAKE_Z_INFESTED"] = ["zerg_train_infestedterran"],
            ["STR_BLD_HATCHERY"] = ["zerg_build_hatchery"],
            ["STR_BLD_CREEP_COLONY"] = ["zerg_build_creepcolony"],
            ["STR_BLD_ZEXTRACTOR"] = ["zerg_build_extractor"],
            ["STR_BLD_SPAWNING"] = ["zerg_build_spawningpool"],
            ["STR_BLD_EVO_CHAMBER"] = ["zerg_build_evolutionchamber"],
            ["STR_BLD_HYDRA_DEN"] = ["zerg_build_hydraden"],
            ["STR_BLD_NYDUS"] = ["zerg_build_nydus"],
            ["STR_BLD_SPIRE"] = ["zerg_build_spire"],
            ["STR_BLD_NEST"] = ["zerg_buiild_queensnest"],
            ["STR_BLD_ULTRA_CAVERN"] = ["zerg_build_ultracavern"],
            ["STR_BLD_DEFILER_MOUND"] = ["zerg_build_defilermound"],
            ["STR_BLD_LAIR"] = ["zerg_cmd_lair"],
            ["STR_BLD_HIVE"] = ["zerg_cmd_hive"],
            ["STR_BLD_GREATERSPIRE"] = ["zerg_cmd_greaterspire"],
            ["STR_BLD_SPORE_COLONY"] = ["zerg_cmd_sporecolony"],
            ["STR_BLD_SUNKEN_COLONY"] = ["zerg_cmd_sunkencolony"],

            ["STR_PSISTORM"] = ["protoss_cmd_psistorm"],
            ["STR_HALLUCINATION"] = ["protoss_cmd_hallucination"],
            ["STR_RECALL"] = ["protoss_cmd_recall"],
            ["STR_STASIS"] = ["protoss_cmd_stasis"],
            ["STR_MAKE_P_ARCHON"] = ["protoss_cmd_archonwarp"],
            ["STR_MAKE_P_DARCHON"] = ["protoss_cmd_darkarchonmeld"],
            ["STR_RECHARGE"] = ["protoss_cmd_recharge"],
            ["STR_DISRUPTOR"] = ["protoss_cmd_distweb"],
            ["STR_MINDCONTROL"] = ["protoss_cmd_mindcontrol"],
            ["STR_PSYFEEDBACK"] = ["protoss_cmd_feedback"],
            ["STR_USEPARALIZE"] = ["protoss_cmd_malestrom"],
            ["STR_RSRCH_PSISTORM"] = ["protoss_res_psistorm"],
            ["STR_RSRCH_HALLUCINATION"] = ["protoss_res_hallucination"],
            ["STR_RSRCH_RECALL"] = ["protoss_res_recall"],
            ["STR_RSRCH_STASIS"] = ["protoss_res_stasis"],
            ["STR_RSRCH_DISRUPTOR"] = ["protoss_res_distweb"],
            ["STR_RSRCH_MINDCONTROL"] = ["protoss_res_mindcontrol"],
            ["STR_RSRCH_PSYFEEDBACK"] = ["protoss_res_feedback"],
            ["STR_RSRCH_PARALIZE"] = ["protoss_res_maelstrom"],
            ["STR_UP_DRAGOON_ATTACK_RANGE"] = ["protoss_res_dragoonrange"],
            ["STR_UP_ZEALOT_SPEED"] = ["protoss_res_legspeed"],
            ["STR_UP_SCARAB_DAMAGE"] = ["protoss_res_scarabdmg"],
            ["STR_UP_REAVER_CAPACITY"] = ["protoss_res_reavercap"],
            ["STR_UP_SHUTTLE_SPEED"] = ["protoss_res_shuttlespeed"],
            ["STR_UP_OBSERVER_SIGHT"] = ["protoss_res_observersight"],
            ["STR_UP_OBSERVER_SPEED"] = ["protoss_res_observermove"],
            ["STR_UP_TEMPLAR_ENERGY"] = ["protoss_res_templarenergy"],
            ["STR_UP_SCOUT_SIGHT"] = ["protoss_res_scoutsight"],
            ["STR_UP_SCOUT_SPEED"] = ["protoss_res_scoutspeed"],
            ["STR_UP_CARRIER_CAPACITY"] = ["protoss_res_carriercap"],
            ["STR_UP_ARBITER_ENERGY"] = ["protoss_res_arbiterenergy"],
            ["STR_UP_CORSAIR_ENERGY"] = ["protoss_res_corsairenergy"],
            ["STR_UP_DARCHON_ENERGY"] = ["protoss_res_darkarchonenergy"],
            ["STR_UP_P_ARMOR"] = ["protoss_upg_groundarmor"],
            ["STR_UP_P_PLATING"] = ["protoss_upg_airarmor"],
            ["STR_UP_P_GND_WEAPONS"] = ["protoss_upg_groundweapons"],
            ["STR_UP_P_AIR_WEAPONS"] = ["protoss_upg_airweapons"],
            ["STR_UP_P_SHIELDS"] = ["protoss_upg_shiends"],
            ["STR_MAKE_P_OBSERVER"] = ["protoss_train_observer"],
            ["STR_MAKE_P_PROBE"] = ["protoss_train_probe"],
            ["STR_MAKE_P_ZEALOT"] = ["protoss_train_zealot"],
            ["STR_MAKE_P_DRAGOON"] = ["protoss_train_dragoon"],
            ["STR_MAKE_P_TEMPLAR"] = ["protoss_train_hightemplar"],
            ["STR_MAKE_P_SHUTTLE"] = ["protoss_train_shuttle"],
            ["STR_MAKE_P_SCOUT"] = ["protoss_train_scout"],
            ["STR_MAKE_P_ARBITER"] = ["protoss_train_arbiter"],
            ["STR_MAKE_P_CARRIER"] = ["protos_train_carrier"],
            ["STR_MAKE_P_INTERCEPTOR"] = ["protoss_cmd_inteceptor"],
            ["STR_MAKE_P_REAVER"] = ["protoss_train_reaver"],
            ["STR_MAKE_P_SCARAB"] = ["protoss_cmd_scarab"],
            ["STR_MAKE_P_CORSAIR"] = ["protoss_train_corsair"],
            ["STR_MAKE_P_DTEMPLAR"] = ["protoss_train_darktemplar"],
            ["STR_BLD_NEXUS"] = ["protoss_build_nexus"],
            ["STR_BLD_PYLON"] = ["protoss_build_pylon"],
            ["STR_BLD_ASSIMILATOR"] = ["protoss_build_assimilator"],
            ["STR_BLD_GATEWAY"] = ["protoss_build_gateway"],
            ["STR_BLD_FORGE"] = ["protoss_build_forge"],
            ["STR_BLD_PHOTON"] = ["protoss_build_cannon"],
            ["STR_BLD_CYBER_CORE"] = ["protoss_build_cybercore"],
            ["STR_BLD_SHIELDBATT"] = ["protoss_build_shieldbattery"],
            ["STR_BLD_ROBOTICS"] = ["protoss_build_roboticsfacility"],
            ["STR_BLD_OBSERVATORY"] = ["protoss_build_observatory"],
            ["STR_BLD_CITADEL"] = ["protoss_build_citadel"],
            ["STR_BLD_ARCHIVES"] = ["protoss_build_templararchives"],
            ["STR_BLD_STARGATE"] = ["protoss_build_stargate"],
            ["STR_BLD_FLEET_BEACON"] = ["protoss_build_fleetbeacon"],
            ["STR_BLD_TRIBUNAL"] = ["protoss_build_arbitertribunal"],
            ["STR_BLD_ROBOTICS_BAY"] = ["protoss_build_roboticssupport"]
        };

    public static RemasteredHotkeyImportResult ApplyFromFile(string sourcePath, IReadOnlyList<HotkeyEntry> entries)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Remastered hotkey file was not found.", sourcePath);
        }

        var values = ParseKeyValues(sourcePath);
        var entriesByCommand = entries.ToDictionary(
            entry => entry.CommandId,
            StringComparer.OrdinalIgnoreCase);
        var updated = 0;

        foreach (var (remasteredKey, hotkey) in values)
        {
            if (!CommandIdsByRemasteredKey.TryGetValue(remasteredKey, out var commandIds))
            {
                continue;
            }

            foreach (var commandId in commandIds)
            {
                if (!entriesByCommand.TryGetValue(commandId, out var entry))
                {
                    continue;
                }

                entry.Hotkey = hotkey;
                updated++;
            }
        }

        return new RemasteredHotkeyImportResult(sourcePath, values.Count, updated);
    }

    public static string? FindFirstCandidateFile(IEnumerable<string> roots)
    {
        foreach (var root in roots.Where(root => !string.IsNullOrWhiteSpace(root)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(root) && LooksLikeRemasteredHotkeyFile(root))
            {
                return root;
            }

            if (!Directory.Exists(root))
            {
                continue;
            }

            var files = Directory.EnumerateFiles(root, "*.*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.System
                })
                .Where(IsPotentialHotkeyFile)
                .OrderBy(file => Path.GetFileName(file).Contains("hotkey", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(file => file, StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                if (LooksLikeRemasteredHotkeyFile(file))
                {
                    return file;
                }
            }
        }

        return null;
    }

    public static string? FindDefaultCandidateFile(IEnumerable<string>? extraRoots = null)
    {
        return FindFirstCandidateFile(DefaultCandidateRoots(extraRoots));
    }

    public static IReadOnlyList<string> DefaultCandidateRoots(IEnumerable<string>? extraRoots = null)
    {
        var roots = new List<string>();
        if (extraRoots is not null)
        {
            roots.AddRange(extraRoots);
        }

        AddStarCraftDocumentRoots(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            AddStarCraftDocumentRoots(roots, Path.Combine(userProfile, "OneDrive", "Documents"));
        }

        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady))
        {
            AddStarCraftDocumentRoots(roots, Path.Combine(drive.RootDirectory.FullName, "OneDrive", "Documents"));
        }

        return roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddStarCraftDocumentRoots(List<string> roots, string documentsRoot)
    {
        if (string.IsNullOrWhiteSpace(documentsRoot))
        {
            return;
        }

        roots.Add(Path.Combine(documentsRoot, "StarCraft", "Hotkeys"));
        roots.Add(Path.Combine(documentsRoot, "StarCraft"));
    }

    private static Dictionary<string, string> ParseKeyValues(string sourcePath)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(sourcePath, Encoding.UTF8))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim();
            if (!key.StartsWith("STR_", StringComparison.OrdinalIgnoreCase) || value.Length == 0)
            {
                continue;
            }

            values[key] = value[..1].ToLowerInvariant();
        }

        return values;
    }

    private static bool LooksLikeRemasteredHotkeyFile(string path)
    {
        if (!IsPotentialHotkeyFile(path))
        {
            return false;
        }

        try
        {
            return File.ReadLines(path, Encoding.UTF8)
                .Take(64)
                .Any(line => line.TrimStart().StartsWith("STR_", StringComparison.OrdinalIgnoreCase));
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsPotentialHotkeyFile(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".hotkeys", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".ini", StringComparison.OrdinalIgnoreCase);
    }
}

public static class HotkeyStatTextPatcher
{
    public static string Patch(string statText, IReadOnlyList<HotkeyEntry> entries)
    {
        var lines = statText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        foreach (var entry in entries)
        {
            var index = entry.StringId - 1;
            if (index < 0 || index >= lines.Count)
            {
                continue;
            }

            lines[index] = HotkeyCsvStore.RenderGameText(entry);
        }

        return string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine;
    }
}

public sealed class HotkeyPatchApplier
{
    private const string MpqTargetName = @"rez\stat_txt.tbl";
    private readonly HotkeyCsvStore _store = new();

    public HotkeyApplyResult SaveAndApply(
        PracticePaths paths,
        string runtimeRoot,
        IReadOnlyList<HotkeyEntry> entries,
        bool applyMpq)
    {
        var safety = RuntimeWritePolicy.CheckMutableRuntimeTarget(paths, runtimeRoot);
        if (!safety.Allowed)
        {
            throw new InvalidOperationException(safety.Message);
        }

        var csvPath = _store.SaveWorkingCopy(runtimeRoot, entries);
        if (!applyMpq)
        {
            return new HotkeyApplyResult
            {
                AppliedMpq = false,
                WorkingCsvPath = csvPath,
                Message = "작업용 핫키 CSV를 저장했습니다."
            };
        }

        var patchedTbl = BuildPatchedTbl(paths, entries);
        var patchRtMpq = Path.Combine(runtimeRoot, "patch_rt.mpq");
        InsertTblIntoRuntimeMpq(paths, patchRtMpq, patchedTbl);

        return new HotkeyApplyResult
        {
            AppliedMpq = true,
            WorkingCsvPath = csvPath,
            PatchedTblPath = patchedTbl,
            Message = "작업용 CSV 저장과 런타임 patch_rt.mpq 핫키 반영을 완료했습니다."
        };
    }

    public static string BuildPatchedTbl(PracticePaths paths, IReadOnlyList<HotkeyEntry> entries)
    {
        var sourceStatText = PracticeAssetPaths.StatText(paths);
        var compiler = PracticeAssetPaths.TblCompiler(paths);
        if (!File.Exists(sourceStatText))
        {
            throw new FileNotFoundException("StarAI bundled stat_txt.txt was not found.", sourceStatText);
        }

        if (!File.Exists(compiler))
        {
            throw new FileNotFoundException("StarAI bundled TBL compiler was not found.", compiler);
        }

        var workDir = Path.Combine(Path.GetTempPath(), "StarAI.PracticeClient", "hotkeys", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var patchedTxt = Path.Combine(workDir, "stat_txt.txt");
        var patchedTbl = Path.Combine(workDir, "stat_txt.tbl");
        var patchedText = HotkeyStatTextPatcher.Patch(File.ReadAllText(sourceStatText, Encoding.UTF8), entries);
        File.WriteAllText(patchedTxt, patchedText, Encoding.UTF8);

        RunProcess(compiler, $"/i \"{patchedTxt}\" \"{patchedTbl}\"", workDir);
        if (!File.Exists(patchedTbl))
        {
            throw new InvalidOperationException("TBL compiler did not create stat_txt.tbl.");
        }

        return patchedTbl;
    }

    public static void InsertTblIntoRuntimeMpq(PracticePaths paths, string patchRtMpqPath, string statTxtTblPath)
    {
        var safety = RuntimeWritePolicy.CheckMutableRuntimeTarget(paths, patchRtMpqPath);
        if (!safety.Allowed)
        {
            throw new InvalidOperationException(safety.Message);
        }

        if (!File.Exists(patchRtMpqPath))
        {
            throw new FileNotFoundException("Runtime patch_rt.mpq was not found.", patchRtMpqPath);
        }

        if (!File.Exists(statTxtTblPath))
        {
            throw new FileNotFoundException("Patched stat_txt.tbl was not found.", statTxtTblPath);
        }

        var backup = patchRtMpqPath + ".starai-hotkey-original";
        if (!File.Exists(backup))
        {
            File.Copy(patchRtMpqPath, backup, overwrite: false);
        }

        var javaExe = ResolveJavaExe();
        var helperPath = WriteJavaHelper();
        var classPath = PracticeAssetPaths.MpqWriterClasspath(paths);
        if (!File.Exists(classPath))
        {
            throw new FileNotFoundException("StarAI bundled MPQ writer classpath was not found.", classPath);
        }

        var args = $"-cp \"{classPath}\" \"{helperPath}\" \"{patchRtMpqPath}\" \"{statTxtTblPath}\" \"{MpqTargetName}\"";
        RunProcess(javaExe, args, Path.GetDirectoryName(helperPath)!);
    }

    private static string ResolveJavaExe()
    {
        var candidates = new[]
        {
            "java.exe",
            @"C:\Java\jdk-25.0.1\bin\java.exe"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                process?.WaitForExit(3000);
                if (process is { ExitCode: 0 })
                {
                    return candidate;
                }
            }
            catch
            {
                // Try the next candidate.
            }
        }

        throw new InvalidOperationException("Java 11+ runtime was not found. Hotkey MPQ patching requires Java source-file mode.");
    }

    private static string WriteJavaHelper()
    {
        var helperDir = Path.Combine(Path.GetTempPath(), "StarAI.PracticeClient", "mpq-helper");
        Directory.CreateDirectory(helperDir);
        var helperPath = Path.Combine(helperDir, "StarAiMpqInsert.java");
        File.WriteAllText(helperPath, JavaHelperSource, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return helperPath;
    }

    private static void RunProcess(string fileName, string arguments, string workingDirectory)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
        }
    }

    private const string JavaHelperSource = """
        import java.nio.file.Files;
        import java.nio.file.Path;
        import systems.crigges.jmpq3.JMpqEditor;
        import systems.crigges.jmpq3.MPQOpenOption;
        import org.jasperge.mpq.MPQEditor;

        public class StarAiMpqInsert {
            public static void main(String[] args) throws Exception {
                if (args.length != 3) {
                    throw new IllegalArgumentException("Usage: StarAiMpqInsert <mpq> <source-file> <target-name>");
                }

                String mpqPath = args[0];
                String sourcePath = args[1];
                String targetName = args[2];

                try (MPQEditor editor = new MPQEditor(Path.of(mpqPath))) {
                    editor.addFile(sourcePath, targetName);
                    if (!editor.hasFile(targetName)) {
                        throw new IllegalStateException("Inserted target is not visible in MPQ: " + targetName);
                    }
                    if (!editor.hasFile("rez\\minimappreview.bin")) {
                        throw new IllegalStateException("Required StarCraft data file missing after insert: rez\\minimappreview.bin");
                    }
                }

                byte[] expected = Files.readAllBytes(Path.of(sourcePath));
                try (JMpqEditor verifier = new JMpqEditor(Path.of(mpqPath), MPQOpenOption.READ_ONLY)) {
                    if (!verifier.hasFile(targetName)) {
                        throw new IllegalStateException("Inserted target missing after MPQ close: " + targetName);
                    }
                    if (!verifier.hasFile("rez\\minimappreview.bin")) {
                        throw new IllegalStateException("Required StarCraft data file missing after MPQ close: rez\\minimappreview.bin");
                    }
                    byte[] actual = verifier.extractFileAsBytes(targetName);
                    if (actual.length != expected.length) {
                        throw new IllegalStateException("Inserted TBL size mismatch. expected=" + expected.length + " actual=" + actual.length);
                    }
                }
            }
        }
        """;
}
