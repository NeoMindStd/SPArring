using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public static partial class PracticeCatalog
{
    public static IReadOnlyList<BotProfile> GetDefaultBots()
    {
        return new[]
        {
            new BotProfile(
                "nitekatt",
                "NiteKatT",
                Race.Terran,
                DifficultyTier.Recovery,
                "bwapi-data/AI/practice-bots/NiteKatT/ExampleAIModule.dll",
                "Low-strength Terran warm-up bot.",
                "BGHBot family. Use it to recover early build order, worker production, scouting, and first expansion timing.",
                "Low to medium. Not a top micro stress bot.",
                Basic("Default", "No forced opening. Good for low-pressure PvT warm-up.")),
            new BotProfile(
                "letabot",
                "LetaBot",
                Race.Terran,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/LetaBot/LetaBot.dll",
                "Terran with wall-in and mined build-order flavor.",
                "Useful for basic bio/mech contact, wall handling, and midgame macro habits.",
                "Medium. More useful as a fundamentals opponent than as a ladder simulator.",
                Basic("Default", "Wall-in/text-mined Terran style.")),
            new BotProfile(
                "insanitybot",
                "insanitybot",
                Race.Terran,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/insanitybot/insanitybot.dll",
                "Defensive two-base Terran tendency.",
                "Good when you want the game to reach your first real PvT midgame without instant top-bot pressure.",
                "Medium.",
                Basic("Default", "Defensive Terran practice.")),
            new BotProfile(
                "icelab",
                "ICELab",
                Race.Terran,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/ICELab/binary_aimodule.dll",
                "Heuristic/strategy-prediction Terran.",
                "Try after NiteKatT/LetaBot feel too soft. Keep or drop based on replay feel.",
                "Unknown. Watch the first two replays.",
                Basic("Default", "Mid Terran candidate.")),
            new BotProfile(
                "iron",
                "Iron bot",
                Race.Terran,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots/Iron_bot/Iron.dll",
                "Strong combat-oriented Terran.",
                "Good hard PvT check. Vultures/raids can feel bot-like, so do not use it as your only daily partner.",
                "High. Strong tactical control.",
                Basic("Default", "Hard Terran check.")),
            new BotProfile(
                "stone",
                "Stone",
                Race.Terran,
                DifficultyTier.Experimental,
                "bwapi-data/AI/practice-bots/Stone/Stone.dll",
                "Older Iron-family Terran.",
                "Kept for retesting. If it crashes or feels strange, leave it out.",
                "Unknown.",
                Basic("Default", "Experimental Terran.")),

            new BotProfile(
                "skyforknet",
                "skyFORKnet",
                Race.Protoss,
                DifficultyTier.Recovery,
                "bwapi-data/AI/practice-bots/skyFORKnet/Skynet.dll",
                "Mostly Skynet Protoss.",
                "Use as a Protoss warm-up, but move it out if its openings feel too repetitive.",
                "Medium.",
                Basic("Default", "Protoss warm-up.")),
            new BotProfile(
                "tommybot",
                "TommyBot",
                Race.Protoss,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/TommyBot/TommyBot.dll",
                "Replay-clustering strategy selection.",
                "Good candidate for more human-shaped variety than a single-script bot.",
                "Medium.",
                Basic("Default", "Replay-derived strategy mix.")),
            new BotProfile(
                "betastar",
                "BetaStar",
                Race.Protoss,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/BetaStar/BetaStar.dll",
                "CSE/ML-flavored Protoss.",
                "Config contains many named Protoss strategies such as FE, DT, goon, proxy, and carrier lines.",
                "Medium to high.",
                WeightedOptions("bwapi-data/AI/practice-bots/BetaStar/BetaStar.json",
                    ("default", "Default mix", "Keep BetaStar's weighted Protoss strategy pool.", null),
                    ("pvt-goon", "PvT 10-15 Gate Goon", "Force 10-15GateGoon in the relevant Protoss matchup.", "10-15GateGoon"),
                    ("pvt-fe", "PvT 10 Gate 25 Nexus FE", "Force 10Gate25NexusFE where available.", "10Gate25NexusFE"),
                    ("pvt-13nexus", "PvT 13 Nexus", "Force 13Nexus where available.", "13Nexus"),
                    ("dt-drop", "DT Drop", "Force DTDrop where available.", "DTDrop"),
                    ("reaver-drop", "Reaver Drop", "Force ReaverDrop where available.", "ReaverDrop"),
                    ("forge-expand", "Forge Expand", "Force ForgeExpand where available.", "ForgeExpand"))),
            new BotProfile(
                "flash",
                "Flash",
                Race.Protoss,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/Flash/flash.dll",
                "UAlberta-style reactive Protoss.",
                "Useful as a mid-level Protoss check. Config names include zealot, dragoon, DT, drop, and carrier-like lines.",
                "Medium.",
                UAlbertaOptions("bwapi-data/AI/practice-bots/Flash/UAlbertaBot_Config.txt",
                    ("default", "Default", "Keep Flash's default Protoss strategy.", null),
                    ("zealot", "Zealot Rush", "Force Protoss_ZealotRush.", "Protoss_ZealotRush"),
                    ("dragoon", "Dragoon Rush", "Force Protoss_DragoonRush.", "Protoss_DragoonRush"),
                    ("dt", "DT Rush", "Force Protoss_DTRush.", "Protoss_DTRush"),
                    ("drop", "Drop Tech", "Force Protoss_Drop.", "Protoss_Drop"))),

            new BotProfile(
                "pineapple-cactus",
                "Pineapple Cactus",
                Race.Zerg,
                DifficultyTier.Recovery,
                "bwapi-data/AI/practice-bots/Pineapple_Cactus/CactusAIModule.dll",
                "Ling/hydra/muta Zerg.",
                "A practical Zerg warm-up if very low pool bots are not useful for your routine.",
                "Medium.",
                Basic("Default", "Ling/hydra/muta practice.")),
            new BotProfile(
                "zia",
                "Zia bot",
                Race.Zerg,
                DifficultyTier.Recovery,
                "bwapi-data/AI/practice-bots/Zia_bot/Ziabot.dll",
                "Lower-strength Zerg.",
                "Good early recovery opponent while you rebuild timings and wall/sim-city memory.",
                "Low to medium.",
                Basic("Default", "Zerg warm-up.")),
            new BotProfile(
                "cubot",
                "CUBOT",
                Race.Zerg,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/CUBOT/CUBOT.dll",
                "Adaptive-weights Zerg.",
                "Main Zerg practice candidate after your first build cycle stops breaking.",
                "Medium.",
                Basic("Default", "Adaptive Zerg practice.")),
            new BotProfile(
                "nlprbot",
                "NLPRbot",
                Race.Zerg,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/NLPRbot/NLPRbot.dll",
                "Mid-strength Zerg candidate.",
                "Good for ordinary anti-Zerg macro rhythm checks.",
                "Medium.",
                Basic("Default", "Mid Zerg practice.")),
            new BotProfile(
                "ailien",
                "AILien",
                Race.Zerg,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/AILien/AIL.dll",
                "Macro-focused Zerg candidate.",
                "Good when you want the game to become about economy and production rather than instant cheese.",
                "Medium.",
                UAlbertaOptions("bwapi-data/AI/practice-bots/AILien/AIL_Config.txt",
                    ("default", "Default", "Keep AILien's default Zerg strategy.", null),
                    ("lings", "Zergling Rush", "Force Zerg_ZerglingRush.", "Zerg_ZerglingRush"),
                    ("9pool", "9 Pool", "Force Zerg_9Pool.", "Zerg_9Pool"),
                    ("hydra", "2 Hatch Hydra", "Force Zerg_2HatchHydra.", "Zerg_2HatchHydra"),
                    ("muta", "3 Hatch Muta", "Force Zerg_3HatchMuta.", "Zerg_3HatchMuta"),
                    ("scourge", "3 Hatch Scourge", "Force Zerg_3HatchScourge.", "Zerg_3HatchScourge"))),
            new BotProfile(
                "sijia-xu",
                "Sijia Xu / Overkill",
                Race.Zerg,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots/Sijia_Xu/Overkill.dll",
                "Overkill Zerg.",
                "Mid-to-upper Zerg check once your openings are stable.",
                "Medium to high.",
                Basic("Default", "Overkill Zerg.")),
            new BotProfile(
                "arrakhammer",
                "Arrakhammer",
                Race.Zerg,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots/Arrakhammer/Arrakhammer.dll",
                "Steamhammer-derived Zerg.",
                "Challenge step before full Steamhammer.",
                "Medium to high.",
                WeightedOptions("bwapi-data/AI/practice-bots/Arrakhammer/Arrakhammer.json",
                    ("default", "Default mix", "Keep Arrakhammer's weighted strategy pool.", null),
                    ("fastpool", "Fast Pool", "Force FastPool in the relevant Zerg matchup.", "FastPool"),
                    ("9pool-speed", "9 Pool Speed", "Force 9PoolSpeed where available.", "9PoolSpeed"),
                    ("overpool", "Overpool Speed", "Force OverpoolSpeed where available.", "OverpoolSpeed"),
                    ("2hatch-hydra", "2 Hatch Hydra", "Force 2HatchHydra where available.", "2HatchHydra"),
                    ("3hatch-hydra", "3 Hatch Hydra", "Force 3HatchHydra/Expo where available.", "3HatchHydraExpo"),
                    ("2hatch-muta", "2 Hatch Muta", "Force a 2 Hatch Muta line where available.", "ZvT_2HatchMuta"),
                    ("3hatch-muta", "3 Hatch Muta", "Force a 3 Hatch Muta line where available.", "ZvT_3HatchMutaExpo"))),
            new BotProfile(
                "steamhammer",
                "Steamhammer",
                Race.Zerg,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots/Steamhammer/Steamhammer.dll",
                "Strong macro-oriented Zerg.",
                "Many config strategies: FastPool, 9PoolSpeed, 2HatchMuta, 3HatchHydra, and more. Use as a hard macro check.",
                "Medium. Usually more macro-pressure than impossible micro.",
                WeightedOptions("bwapi-data/AI/practice-bots/Steamhammer/Steamhammer_5.2.3.json",
                    ("default", "Default mix", "Keep Steamhammer's weighted strategy pool.", null),
                    ("fastpool", "Fast Pool", "Force FastPool in the relevant Zerg matchup.", "FastPool"),
                    ("9pool-speed", "9 Pool Speed", "Force 9PoolSpeed where available.", "9PoolSpeed"),
                    ("overpool", "Overpool Speed", "Force OverpoolSpeed where available.", "OverpoolSpeed"),
                    ("2hatch-hydra", "2 Hatch Hydra", "Force 2HatchHydra where available.", "2HatchHydra"),
                    ("3hatch-hydra", "3 Hatch Hydra", "Force 3HatchHydraExpo where available.", "3HatchHydraExpo"),
                    ("2hatch-muta", "2 Hatch Muta", "Force a 2 Hatch Muta line where available.", "ZvT_2HatchMuta"),
                    ("3hatch-muta", "3 Hatch Muta", "Force a 3 Hatch Muta line where available.", "ZvT_3HatchMutaExpo"))),

            new BotProfile(
                "dragon",
                "Dragon",
                Race.Terran,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/Dragon/dragon.dll",
                "Strong Terran bot from the SCHNAIL pool.",
                "Use after Iron/WillyT level feels manageable. Good hard PvT check, not a warm-up bot.",
                "High. Treat as stress test.",
                Basic("Default", "Strong Terran default behavior.")),
            new BotProfile(
                "willyt",
                "WillyT",
                Race.Terran,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/WillyT/WillyT.dll",
                "Strong Terran bot.",
                "Good late-stage PvT check after basic build recovery is stable.",
                "High.",
                Basic("Default", "Strong Terran default behavior.")),
            new BotProfile(
                "xiaoyicog2019",
                "XIAOYICOG2019",
                Race.Terran,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots-extra/XIAOYICOG2019/XIAOYI.dll",
                "Mid-strength Terran from SCHNAIL.",
                "Useful as another Terran sparring sample when NiteKatT/ICELab feel repetitive.",
                "Medium.",
                Basic("Default", "Terran practice.")),
            new BotProfile(
                "trident",
                "Trident",
                Race.Terran,
                DifficultyTier.Recovery,
                "bwapi-data/AI/practice-bots-extra/Trident/Trident.dll",
                "Tiny/low ELO Terran.",
                "Use only for low-pressure mechanics recovery.",
                "Low.",
                Basic("Default", "Very light Terran warm-up.")),
            new BotProfile(
                "terminus",
                "Terminus",
                Race.Terran,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/Terminus/BananaBrain.dll",
                "Very strong Terran/strategy bot package.",
                "Hard check only. Do not use as daily recovery sparring until your builds are back.",
                "High.",
                Basic("Default", "Hard Terran check.")),

            new BotProfile(
                "aiur",
                "AIUR",
                Race.Protoss,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots-extra/AIUR/AIUR.dll",
                "Mid Protoss bot.",
                "Good PvP/TvP/ZvP practice after simple warm-up bots.",
                "Medium.",
                Basic("Default", "Mid Protoss practice.")),
            new BotProfile(
                "wulibot",
                "WuliBot",
                Race.Protoss,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots-extra/WuliBot/Wuli.dll",
                "UAlbertaBot-derived Protoss.",
                "Useful extra Protoss sample with more variety than a single rush bot.",
                "Medium.",
                Basic("Default", "Protoss practice.")),
            new BotProfile(
                "megabot2017",
                "MegaBot2017",
                Race.Protoss,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots-extra/MegaBot2017/MegaBot.dll",
                "Meta bot choosing among sub-bots.",
                "Good for reducing single-pattern fatigue.",
                "Medium.",
                Basic("Default", "Sub-bot strategy mix.")),
            new BotProfile(
                "slater",
                "Slater",
                Race.Protoss,
                DifficultyTier.Recovery,
                "bwapi-data/AI/practice-bots-extra/Slater/BWAPI.dll",
                "Protoss shuttle/reaver flavored bot.",
                "Use as a specific reaver/drop response drill, not as a main daily bot.",
                "Medium.",
                Basic("Default", "Reaver/drop drill.")),
            new BotProfile(
                "bananabrain",
                "BananaBrain",
                Race.Protoss,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/BananaBrain/BananaBrain.dll",
                "Top Protoss bot.",
                "Hard check only. Strong build and tactical pressure.",
                "High.",
                Basic("Default", "Top Protoss hard check.")),
            new BotProfile(
                "stardust",
                "Stardust",
                Race.Protoss,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/Stardust/Stardust.dll",
                "Top tournament Protoss.",
                "Mostly for stress testing; can feel less human-practice-friendly.",
                "High.",
                Basic("Default", "Top Protoss hard check.")),
            new BotProfile(
                "locutus",
                "Locutus",
                Race.Protoss,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/Locutus/Locutus.dll",
                "Strong Protoss.",
                "Harder Protoss sample after BetaStar/Flash feel manageable.",
                "High.",
                Basic("Default", "Strong Protoss.")),

            new BotProfile(
                "chris-coxe",
                "Chris Coxe / ZZZKBot",
                Race.Zerg,
                DifficultyTier.Main,
                "bwapi-data/AI/practice-bots-extra/Chris_Coxe/ZZZKBot.dll",
                "Classic Zerg bot.",
                "Good extra mid Zerg sample.",
                "Medium.",
                Basic("Default", "Zerg practice.")),
            new BotProfile(
                "microwave",
                "Microwave",
                Race.Zerg,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/Microwave/Microwave.dll",
                "Strong Zerg.",
                "Use after Sijia/Arrakhammer. Good pressure check.",
                "Medium to high.",
                Basic("Default", "Strong Zerg.")),
            new BotProfile(
                "proxy",
                "Proxy",
                Race.Zerg,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/Proxy/ZergBot.dll",
                "Muta-oriented Zerg candidate.",
                "Useful for muta response practice.",
                "Medium to high.",
                Basic("Default", "Muta-pressure Zerg.")),
            new BotProfile(
                "randomhammer",
                "Randomhammer",
                Race.Random,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/Randomhammer/Steamhammer.dll",
                "Steamhammer random-race package.",
                "Good variety check once you want less predictable race/opening exposure.",
                "Medium to high.",
                Basic("Default", "Random-race Steamhammer style.")),
            new BotProfile(
                "mcravez",
                "McRaveZ",
                Race.Zerg,
                DifficultyTier.Challenge,
                "bwapi-data/AI/practice-bots-extra/McRaveZ/McRave.dll",
                "Strong bot aiming for more human-like play.",
                "Good late-stage Zerg hard check; directionally close to human-ish play but strong.",
                "High.",
                Basic("Default", "Strong human-ish Zerg check.")),
            new BotProfile(
                "newbie-zerg",
                "Newbie Zerg",
                Race.Zerg,
                DifficultyTier.Drill,
                "bwapi-data/AI/practice-bots-extra/Newbie_Zerg/Newbie Zerg.dll",
                "Very early-pool Zerg.",
                "Use only for cheese defense drills.",
                "Low but annoying.",
                Basic("Default", "5-pool style drill.")),
            new BotProfile(
                "sungguk-cha",
                "Sungguk Cha",
                Race.Terran,
                DifficultyTier.Drill,
                "bwapi-data/AI/practice-bots-extra/Sungguk_Cha/SunggukCha.dll",
                "Simple rush bot.",
                "Use for defense drill only.",
                "Low.",
                Basic("Default", "Rush drill.")),
            new BotProfile(
                "yuanheng-zhu",
                "Yuanheng Zhu",
                Race.Protoss,
                DifficultyTier.Drill,
                "bwapi-data/AI/practice-bots-extra/Yuanheng_Zhu/Juno.dll",
                "Probe rush bot.",
                "Use only for anti-cheese drill.",
                "Low.",
                Basic("Default", "Probe-rush drill.")),
            new BotProfile(
                "zealot-hell",
                "Zealot Hell",
                Race.Protoss,
                DifficultyTier.Drill,
                "bwapi-data/AI/practice-bots-extra/Zealot_Hell/BWAPI.dll",
                "Simple zealot rush.",
                "Use only for early defense drill.",
                "Low.",
                Basic("Default", "Zealot-rush drill."))
        }.Select(WithMetadata).ToArray();
    }

    public static IReadOnlyList<BotProfile> GetAvailableBots(string starCraftRoot)
    {
        return GetDefaultBots()
            .Where(bot => File.Exists(bot.DllPath(starCraftRoot)))
            .OrderBy(bot => bot.Race)
            .ThenBy(bot => bot.Tier)
            .ThenBy(bot => bot.Name)
            .ToArray();
    }

    public static IReadOnlyList<MapProfile> GetMaps(string starCraftRoot)
    {
        var mapsRoot = Path.Combine(starCraftRoot, "maps");
        if (!Directory.Exists(mapsRoot))
        {
            return Array.Empty<MapProfile>();
        }

        return Directory.EnumerateFiles(mapsRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".scx", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".scm", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}replays{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path =>
            {
                var relative = Path.GetRelativePath(starCraftRoot, path).Replace('\\', '/');
                var fileName = Path.GetFileNameWithoutExtension(path);
                return new MapProfile(CleanMapName(fileName), relative, ParsePlayers(fileName));
            })
            .GroupBy(map => map.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(map => map.Players ?? 99)
            .ThenBy(map => map.Name)
            .ToArray();
    }

    public static string DefaultRoot => Directory.Exists(@"C:\starai\SC116AI")
        ? @"C:\starai\SC116AI"
        : AppContext.BaseDirectory;

    private static BotProfile WithMetadata(BotProfile bot)
    {
        var metadata = BotMetadata.TryGetValue(bot.Id, out var value) ? value : (bot.Elo, Array.Empty<string>());
        return bot with { Elo = metadata.Elo, Tags = metadata.Tags };
    }

    private static readonly Dictionary<string, (int? Elo, IReadOnlyList<string> Tags)> BotMetadata = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trident"] = (360, new[] { "terran", "warmup", "low", "rush" }),
        ["sungguk-cha"] = (714, new[] { "terran", "rush", "drill" }),
        ["xiaoyicog2019"] = (923, new[] { "terran", "main" }),
        ["nitekatt"] = (2491, new[] { "terran", "warmup", "recovery", "bio", "macro" }),
        ["letabot"] = (2500, new[] { "terran", "wall", "main" }),
        ["icelab"] = (2545, new[] { "terran", "main", "heuristic" }),
        ["insanitybot"] = (2585, new[] { "terran", "defensive", "main" }),
        ["iron"] = (2857, new[] { "terran", "vulture", "harass", "challenge" }),
        ["willyt"] = (2856, new[] { "terran", "challenge" }),
        ["dragon"] = (2975, new[] { "terran", "challenge" }),
        ["terminus"] = (3028, new[] { "terran", "challenge" }),
        ["stone"] = (2289, new[] { "terran", "experimental", "warmup" }),

        ["skyforknet"] = (2273, new[] { "protoss", "warmup" }),
        ["slater"] = (2345, new[] { "protoss", "reaver", "drop", "drill" }),
        ["aiur"] = (2504, new[] { "protoss", "main" }),
        ["betastar"] = (2538, new[] { "protoss", "FE", "DT", "goon", "carrier", "main" }),
        ["flash"] = (2573, new[] { "protoss", "dragoon", "DT", "drop", "main" }),
        ["wulibot"] = (2602, new[] { "protoss", "main" }),
        ["megabot2017"] = (2602, new[] { "protoss", "mix", "main" }),
        ["tommybot"] = (2600, new[] { "protoss", "mix", "replay-derived" }),
        ["yuanheng-zhu"] = (900, new[] { "protoss", "probe rush", "drill" }),
        ["zealot-hell"] = (900, new[] { "protoss", "zealot rush", "drill" }),
        ["locutus"] = (3117, new[] { "protoss", "challenge" }),
        ["stardust"] = (3263, new[] { "protoss", "challenge", "top" }),
        ["bananabrain"] = (3216, new[] { "protoss", "challenge", "top" }),

        ["zia"] = (2474, new[] { "zerg", "warmup" }),
        ["pineapple-cactus"] = (2453, new[] { "zerg", "ling", "hydra", "muta", "warmup" }),
        ["cubot"] = (2482, new[] { "zerg", "adaptive", "main" }),
        ["nlprbot"] = (2565, new[] { "zerg", "main" }),
        ["ailien"] = (2592, new[] { "zerg", "hydra", "muta", "main" }),
        ["sijia-xu"] = (2598, new[] { "zerg", "overkill", "main" }),
        ["chris-coxe"] = (2609, new[] { "zerg", "main" }),
        ["arrakhammer"] = (2662, new[] { "zerg", "pool", "hydra", "muta", "challenge" }),
        ["proxy"] = (2712, new[] { "zerg", "muta", "challenge" }),
        ["steamhammer"] = (2848, new[] { "zerg", "macro", "hydra", "muta", "challenge" }),
        ["randomhammer"] = (2848, new[] { "random", "macro", "challenge" }),
        ["microwave"] = (2844, new[] { "zerg", "challenge" }),
        ["mcravez"] = (3025, new[] { "zerg", "human-like", "challenge" }),
        ["newbie-zerg"] = (1000, new[] { "zerg", "5 pool", "drill" })
    };

    private static IReadOnlyList<BuildOption> Basic(string name, string description)
    {
        return new[] { new BuildOption("default", name, description) };
    }

    private static IReadOnlyList<BuildOption> UAlbertaOptions(
        string configPath,
        params (string Id, string Name, string Description, string? StrategyId)[] options)
    {
        return options.Select(option => new BuildOption(
            option.Id,
            option.Name,
            option.Description,
            option.StrategyId is null ? null : new BuildPatch(BuildPatchKind.UAlbertaRaceStrategy, configPath, option.StrategyId))).ToArray();
    }

    private static IReadOnlyList<BuildOption> WeightedOptions(
        string configPath,
        params (string Id, string Name, string Description, string? StrategyId)[] options)
    {
        return options.Select(option => new BuildOption(
            option.Id,
            option.Name,
            option.Description,
            option.StrategyId is null ? null : new BuildPatch(BuildPatchKind.MatchupWeightedStrategy, configPath, option.StrategyId))).ToArray();
    }

    private static string CleanMapName(string fileName)
    {
        var cleaned = MapPrefixRegex().Replace(fileName, "").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? fileName : cleaned;
    }

    private static int? ParsePlayers(string fileName)
    {
        var match = PlayerCountRegex().Match(fileName);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var players))
        {
            return players;
        }

        return null;
    }

    [GeneratedRegex(@"^\(?([2-8])\)?", RegexOptions.Compiled)]
    private static partial Regex PlayerCountRegex();

    [GeneratedRegex(@"^\(?[2-8]\)?\s*", RegexOptions.Compiled)]
    private static partial Regex MapPrefixRegex();
}
