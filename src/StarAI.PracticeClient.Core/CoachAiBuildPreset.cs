namespace StarAI.PracticeClient.Core;

public sealed record CoachAiBuildPreset(
    string Id,
    string Name,
    Race Race,
    IReadOnlyList<string> Steps,
    string Tips,
    string? OverlayTitle = null)
{
    public override string ToString() => Name;

    public string TitleForOverlay => string.IsNullOrWhiteSpace(OverlayTitle) ? Name : OverlayTitle;
}

public static class CoachAiBuildPresets
{
    public static readonly CoachAiBuildPreset KeepExisting = new(
        "keep",
        "현재 CoachAI 설정 유지",
        Race.Random,
        Array.Empty<string>(),
        string.Empty);

    public static IReadOnlyList<CoachAiBuildPreset> All { get; } =
    [
        new(
            "protoss-recovery-core",
            "프로토스 기본 복구: 10/12 코어",
            Race.Protoss,
            [
                "00:00 Worker split, probe production",
                "00:09 8 Pylon",
                "01:05 10 Gateway",
                "01:35 12 Assimilator",
                "02:05 14 Cybernetics Core",
                "02:30 15 Pylon",
                "03:00 Start Dragoon",
                "03:15 Start range",
                "04:00 Scout; decide expand or pressure"
            ],
            "Early-order recovery preset. Replace with your exact matchup build later.",
            "Protoss recovery: 10/12 Core"),

        new(
            "protoss-pvt-fe",
            "프로토스 PvT: 옵저버 이후 앞마당",
            Race.Protoss,
            [
                "00:00 Worker split, probe production",
                "00:09 8 Pylon",
                "01:05 10 Gateway",
                "01:35 12 Gas",
                "02:05 14 Core",
                "02:50 Dragoon, range",
                "03:25 Robotics Facility",
                "04:15 Observatory",
                "05:00 Prepare natural",
                "06:00 Add gates; judge shuttle/third"
            ],
            "PvT recovery preset. Delay natural if early Terran pressure is heavy.",
            "Protoss PvT: Observer into FE"),

        new(
            "terran-recovery-factory",
            "테란 기본 복구: 팩더블 감각",
            Race.Terran,
            [
                "00:00 Worker split, SCV production",
                "00:45 8 Supply Depot",
                "01:15 10 Barracks",
                "01:50 11 Refinery",
                "02:20 13 Supply Depot",
                "02:45 Factory",
                "03:25 Machine Shop or natural plan",
                "04:20 Vulture/Tank choice",
                "05:00 Natural, keep scouting"
            ],
            "Worker, supply, and factory timing recovery preset.",
            "Terran recovery: Factory expand"),

        new(
            "zerg-recovery-hatch",
            "저그 기본 복구: 12해처리 감각",
            Race.Zerg,
            [
                "00:00 Worker split, drone production",
                "00:25 9 Overlord",
                "01:20 12 Hatchery or Pool",
                "02:10 Extractor if needed",
                "02:35 Overlord",
                "03:00 Spend larvae",
                "04:00 Decide hydra/muta/expand by matchup"
            ],
            "Larva and overlord rhythm recovery preset.",
            "Zerg recovery: 12 Hatch rhythm")
    ];

    public static CoachAiBuildPreset DefaultForRace(Race race) =>
        All.FirstOrDefault(preset => preset.Race == race) ?? All[0];
}
