using Sparring.Core;

namespace Sparring.Client;

internal sealed record HotkeyCommandObjectInfo(
    string Key,
    string Race,
    string Category,
    string DisplayName,
    int CategoryRank,
    bool IncludeCommonCommands);

internal static class HotkeyCommandLayout
{
    public const string PageGeneral = "일반";
    public const string PageBasicStructures = "기본 구조물";
    public const string PageAdvancedStructures = "고급 구조물";
    public const string PageAll = "전체";

    private static readonly IReadOnlyDictionary<string, int> GeneralSlots =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["general_cmd_move"] = 0,
            ["general_cmd_stop"] = 1,
            ["general_cmd_attack"] = 2,
            ["terran_cmd_repair"] = 3,
            ["general_cmd_gather"] = 4,
            ["general_cmd_holdpos"] = 5,
            ["general_cmd_patrol"] = 6,
            ["terran_cmd_buildstruc"] = 7,
            ["protoss_cmd_buildstruc"] = 7,
            ["zerg_cmd_buildstruc"] = 7,
            ["terran_cmd_buildadvstruc"] = 8,
            ["protoss_cmd_buildadvstruc"] = 8,
            ["zerg_cmd_buildadvstruc"] = 8
        };

    private static readonly IReadOnlyDictionary<string, int> StructureSlots =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["terran_build_commandcenter"] = 0,
            ["terran_build_supplydepot"] = 1,
            ["terran_build_refinery"] = 2,
            ["terran_build_barracks"] = 3,
            ["terran_build_engineeringbay"] = 4,
            ["terran_build_missileturret"] = 5,
            ["terran_build_bunker"] = 6,
            ["terran_build_academy"] = 7,
            ["terran_build_factory"] = 0,
            ["terran_build_starport"] = 1,
            ["terran_build_sciencefacility"] = 2,
            ["terran_build_armory"] = 3,
            ["terran_build_comsat"] = 4,
            ["terran_build_nukesilo"] = 5,
            ["terran_build_machineshop"] = 6,
            ["terran_build_controltower"] = 7,
            ["terran_build_covertops"] = 8,

            ["protoss_build_nexus"] = 0,
            ["protoss_build_pylon"] = 1,
            ["protoss_build_assimilator"] = 2,
            ["protoss_build_gateway"] = 3,
            ["protoss_build_forge"] = 4,
            ["protoss_build_cannon"] = 5,
            ["protoss_build_cybercore"] = 6,
            ["protoss_build_shieldbattery"] = 7,
            ["protoss_build_roboticsfacility"] = 0,
            ["protoss_build_stargate"] = 1,
            ["protoss_build_citadel"] = 2,
            ["protoss_build_roboticssupport"] = 3,
            ["protoss_build_fleetbeacon"] = 4,
            ["protoss_build_templararchives"] = 5,
            ["protoss_build_observatory"] = 6,
            ["protoss_build_arbitertribunal"] = 7,

            ["zerg_build_hatchery"] = 0,
            ["zerg_build_extractor"] = 1,
            ["zerg_build_spawningpool"] = 2,
            ["zerg_build_evolutionchamber"] = 3,
            ["zerg_build_hydraden"] = 4,
            ["zerg_build_creepcolony"] = 5,
            ["zerg_build_spire"] = 6,
            ["zerg_build_nydus"] = 7,
            ["zerg_buiild_queensnest"] = 0,
            ["zerg_build_ultracavern"] = 1,
            ["zerg_build_defilermound"] = 2
        };

    private static readonly HashSet<string> AdvancedStructures =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "terran_build_factory",
            "terran_build_starport",
            "terran_build_sciencefacility",
            "terran_build_armory",
            "terran_build_comsat",
            "terran_build_nukesilo",
            "terran_build_machineshop",
            "terran_build_controltower",
            "terran_build_covertops",
            "terran_build_physicslab",
            "protoss_build_roboticsfacility",
            "protoss_build_stargate",
            "protoss_build_citadel",
            "protoss_build_roboticssupport",
            "protoss_build_fleetbeacon",
            "protoss_build_templararchives",
            "protoss_build_observatory",
            "protoss_build_arbitertribunal",
            "zerg_buiild_queensnest",
            "zerg_build_ultracavern",
            "zerg_build_defilermound"
        };

    public static HotkeyCommandObjectInfo ObjectFor(HotkeyEntry entry)
    {
        var race = RaceName(entry);
        if (TryWorkerName(entry, out var workerName))
        {
            var workerKey = $"{race}|worker|{workerName}".ToLowerInvariant();
            return new HotkeyCommandObjectInfo(workerKey, race, "유닛", workerName, 1, IncludeCommonCommands: true);
        }

        var category = DepthCategoryName(entry);
        var display = ObjectDisplayName(entry);
        var rank = category switch
        {
            "일반" => 0,
            "유닛" => 1,
            "건물" => 2,
            "기술" => 3,
            "연구" => 4,
            "업그레이드" => 5,
            "변태" => 6,
            _ => 9
        };
        var key = $"{race}|{category}|{display}".ToLowerInvariant();
        return new HotkeyCommandObjectInfo(key, race, category, display, rank, IncludeCommonCommands: category is "유닛" or "건물");
    }

    public static string PageName(HotkeyEntry entry)
    {
        var id = entry.CommandId;
        if (GeneralSlots.ContainsKey(id) || IsCommonCommand(entry))
        {
            return PageGeneral;
        }

        if (id.Contains("_build_", StringComparison.OrdinalIgnoreCase) ||
            id.StartsWith("zerg_buiild_", StringComparison.OrdinalIgnoreCase))
        {
            return AdvancedStructures.Contains(id) ? PageAdvancedStructures : PageBasicStructures;
        }

        return PageGeneral;
    }

    public static int Slot(HotkeyEntry entry)
    {
        var id = entry.CommandId;
        if (PageName(entry) == PageGeneral && GeneralSlots.TryGetValue(id, out var generalSlot))
        {
            return generalSlot;
        }

        return StructureSlots.TryGetValue(id, out var structureSlot) ? structureSlot : -1;
    }

    public static bool IsCommonCommand(HotkeyEntry entry)
    {
        return entry.CommandId.StartsWith("general_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryWorkerName(HotkeyEntry entry, out string workerName)
    {
        var id = entry.CommandId;
        if (id.Equals("terran_train_scv", StringComparison.OrdinalIgnoreCase) ||
            id.StartsWith("terran_build_", StringComparison.OrdinalIgnoreCase) ||
            id is "terran_cmd_buildstruc" or "terran_cmd_buildadvstruc" or "terran_cmd_repair")
        {
            workerName = "SCV";
            return true;
        }

        if (id.Equals("protoss_train_probe", StringComparison.OrdinalIgnoreCase) ||
            id.StartsWith("protoss_build_", StringComparison.OrdinalIgnoreCase))
        {
            workerName = "Probe";
            return true;
        }

        if (id.Equals("zerg_train_drone", StringComparison.OrdinalIgnoreCase) ||
            id.StartsWith("zerg_build_", StringComparison.OrdinalIgnoreCase) ||
            id.StartsWith("zerg_buiild_", StringComparison.OrdinalIgnoreCase) ||
            id is "zerg_cmd_buildstruc" or "zerg_cmd_buildadvstruc")
        {
            workerName = "Drone";
            return true;
        }

        workerName = string.Empty;
        return false;
    }

    public static string RaceName(HotkeyEntry entry)
    {
        var id = entry.CommandId.ToLowerInvariant();
        return id switch
        {
            var value when value.StartsWith("terran_", StringComparison.Ordinal) => "Terran",
            var value when value.StartsWith("protoss_", StringComparison.Ordinal) => "Protoss",
            var value when value.StartsWith("zerg_", StringComparison.Ordinal) => "Zerg",
            _ => "Common"
        };
    }

    private static string CategoryName(HotkeyEntry entry)
    {
        var id = entry.CommandId.ToLowerInvariant();
        return id switch
        {
            var value when value.StartsWith("general_", StringComparison.Ordinal) => "일반",
            var value when value.Contains("_train_", StringComparison.Ordinal) => "생산",
            var value when value.Contains("_build_", StringComparison.Ordinal) => "건설",
            var value when value.Contains("_res_", StringComparison.Ordinal) => "연구",
            var value when value.Contains("_upg_", StringComparison.Ordinal) => "업그레이드",
            var value when value.Contains("_spell_", StringComparison.Ordinal) => "기술",
            var value when value.Contains("_morph_", StringComparison.Ordinal) => "변태",
            _ => "기타"
        };
    }

    private static string DepthCategoryName(HotkeyEntry entry)
    {
        return CategoryName(entry) switch
        {
            "생산" => "유닛",
            "건설" => "건물",
            var category => category
        };
    }

    private static string ObjectDisplayName(HotkeyEntry entry)
    {
        var description = entry.Description.Trim();
        var suffixes = new[]
        {
            " 생산",
            " 소환",
            " 건설",
            " 개발",
            " 업그레이드",
            " 사용",
            " 연구"
        };

        foreach (var suffix in suffixes)
        {
            if (description.EndsWith(suffix, StringComparison.Ordinal))
            {
                return description[..^suffix.Length].Trim();
            }
        }

        return description;
    }
}
