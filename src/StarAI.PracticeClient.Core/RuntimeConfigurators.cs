using System.IO.Compression;
using System.Text;

namespace StarAI.PracticeClient.Core;

public sealed record PracticeRuntimeOptions(
    string ReplayRoot,
    int SpeedOverrideMs = -1)
{
    public static PracticeRuntimeOptions Defaults()
    {
        return new PracticeRuntimeOptions(@"D:\OneDrive\Documents\StarCraft\Maps\Replays\ai");
    }
}

public sealed record WModeSettings(
    bool WindowedMode,
    bool ClipCursor,
    bool DoubleSize,
    bool MuteNotFocused,
    int WindowX = 0,
    int WindowY = 0);

public static class PracticeIniConfigurator
{
    public const string BwapiIniRelativePath = @"bwapi-data\bwapi.ini";
    public const string PlayerTournamentModule = @"bwapi-data\TM\TournamentModule.dll";

    public static string Apply(ClientLaunchSettings settings, PracticeRuntimeOptions options)
    {
        var iniPath = Path.Combine(settings.RuntimeRoot, BwapiIniRelativePath);
        EnsureBackup(iniPath);

        var ini = IniDocument.LoadOrCreate(iniPath, "ai");
        if (settings.Role == ClientRuntimeRole.PlayerHost && string.IsNullOrWhiteSpace(settings.AiModule))
        {
            ini.Remove("ai", "ai");
            ini.Set("ai", "tournament", PlayerTournamentModule);
        }
        else
        {
            ini.Set("ai", "ai", settings.AiModule);
            ini.Set("ai", "tournament", string.Empty);
        }
        ini.Set("auto_menu", "auto_menu", "LAN");
        ini.Set("auto_menu", "character_name", settings.CharacterName);
        ini.Set("auto_menu", "lan_mode", "Local PC");
        ini.Set("auto_menu", "map", settings.MapFileName);
        ini.Set("auto_menu", "game", settings.GameName);
        ini.Set("auto_menu", "game_type", "MELEE");
        ini.Set("auto_menu", "race", FormatRace(settings.Race));
        ini.Set("auto_menu", "enemy_race", FormatRace(settings.EnemyRace));
        ini.Set("auto_menu", "save_replay", ReplayPattern(options.ReplayRoot));
        ini.Set("auto_menu", "wait_for_min_players", "2");
        ini.Set("auto_menu", "wait_for_max_players", "2");
        ini.Set("auto_menu", "wait_for_time", "5000");
        ini.Set("window", "windowed", settings.WindowedMode ? "ON" : "OFF");
        ini.Set("starcraft", "sound", settings.SoundEnabled ? "ON" : "OFF");
        ini.Set("starcraft", "speed_override", options.SpeedOverrideMs.ToString());
        ini.Save(iniPath);
        return iniPath;
    }

    public static string DisableAutoMenu(string runtimeRoot)
    {
        var iniPath = Path.Combine(runtimeRoot, BwapiIniRelativePath);
        var ini = IniDocument.LoadOrCreate(iniPath, "auto_menu");
        ini.Set("auto_menu", "auto_menu", "OFF");
        ini.Set("auto_menu", "map", string.Empty);
        ini.Set("auto_menu", "game", string.Empty);
        ini.Save(iniPath);
        return iniPath;
    }

    private static string ReplayPattern(string replayRoot)
    {
        return Path.Combine(replayRoot, "$Y $b $d", "StarAI_%BOTNAME6%_%MAP%_$H$M$S.rep");
    }

    private static string FormatRace(StarCraftRace race)
    {
        return race switch
        {
            StarCraftRace.Terran => "Terran",
            StarCraftRace.Protoss => "Protoss",
            StarCraftRace.Zerg => "Zerg",
            StarCraftRace.Random => "Random",
            _ => "Default"
        };
    }

    private static void EnsureBackup(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var backup = path + ".starai-original";
        if (!File.Exists(backup))
        {
            File.Copy(path, backup, overwrite: false);
        }
    }
}

public static class WModeConfigurator
{
    public static string Apply(string runtimeRoot, WModeSettings settings)
    {
        var path = Path.Combine(runtimeRoot, "wmode.ini");
        var ini = IniDocument.LoadOrCreate(path, "W-MODE");
        ini.Set("W-MODE", "SaveClipCursor", "1");
        ini.Set("W-MODE", "ClipCursor", settings.WindowedMode && settings.ClipCursor ? "1" : "0");
        ini.Set("W-MODE", "SaveWindowed", "1");
        ini.Set("W-MODE", "Windowed", settings.WindowedMode ? "1" : "0");
        ini.Set("W-MODE", "SaveDblSizeMode", "1");
        ini.Set("W-MODE", "DblSizeMode", settings.DoubleSize ? "1" : "0");
        ini.Set("W-MODE", "WindowClientX", settings.WindowX.ToString());
        ini.Set("W-MODE", "WindowClientY", settings.WindowY.ToString());
        ini.Set("W-MODE", "WindowClientXDblSized", settings.WindowX.ToString());
        ini.Set("W-MODE", "WindowClientYDblSized", settings.WindowY.ToString());
        ini.Set("W-MODE", "SaveEnableWindowMove", "1");
        ini.Set("W-MODE", "EnableWindowMove", "1");
        ini.Set("W-MODE", "AlwaysOnTop", "0");
        ini.Set("W-MODE", "DisableControls", "0");
        ini.Set("W-MODE", "MuteNotFocused", settings.MuteNotFocused ? "1" : "0");
        ini.Save(path);
        return path;
    }
}

public static class ChaosPluginFileConfigurator
{
    private const string DisabledSuffix = ".starai-disabled";

    public static void Apply(string runtimeRoot, bool enableWMode, bool enableApmAlert)
    {
        ConfigurePlugin(runtimeRoot, "wmode.bwl", enableWMode);
        ConfigurePlugin(runtimeRoot, "APMAlert.bwl", enableApmAlert);
    }

    private static void ConfigurePlugin(string runtimeRoot, string fileName, bool enabled)
    {
        var activePath = Path.Combine(runtimeRoot, "Plugins", fileName);
        var disabledPath = activePath + DisabledSuffix;

        if (enabled)
        {
            if (!File.Exists(activePath) && File.Exists(disabledPath))
            {
                File.Move(disabledPath, activePath);
            }

            return;
        }

        if (!File.Exists(activePath))
        {
            return;
        }

        if (File.Exists(disabledPath))
        {
            File.Delete(disabledPath);
        }

        File.Move(activePath, disabledPath);
    }
}

public static class PracticeRuntimeConfigurator
{
    public static void Apply(PracticeLaunchPlan plan, PracticeRuntimeOptions options)
    {
        PracticeIniConfigurator.Apply(plan.Player, options);
        PracticeIniConfigurator.Apply(plan.Ai, options);
        ChaosPluginFileConfigurator.Apply(plan.Player.RuntimeRoot, plan.Player.EnableWModePlugin, plan.Player.ApmAlertEnabled);
        ChaosPluginFileConfigurator.Apply(plan.Ai.RuntimeRoot, plan.Ai.EnableWModePlugin, plan.Ai.ApmAlertEnabled);
        CncDdrawConfigurator.Apply(plan.Player.RuntimeRoot, plan.Player.CncDdrawMode);
        CncDdrawConfigurator.Apply(plan.Ai.RuntimeRoot, plan.Ai.CncDdrawMode);
        WModeConfigurator.Apply(plan.Player.RuntimeRoot, new WModeSettings(
            WindowedMode: plan.Player.EnableWModePlugin,
            ClipCursor: plan.Player.ClipCursor,
            DoubleSize: true,
            MuteNotFocused: false,
            WindowX: 0,
            WindowY: 0));
        WModeConfigurator.Apply(plan.Ai.RuntimeRoot, new WModeSettings(
            WindowedMode: plan.Ai.EnableWModePlugin,
            ClipCursor: false,
            DoubleSize: false,
            MuteNotFocused: true,
            WindowX: 32,
            WindowY: 32));
    }

    public static void DisableAutoMenuAfterGameStart(PracticeLaunchPlan plan)
    {
        PracticeIniConfigurator.DisableAutoMenu(plan.Player.RuntimeRoot);
        PracticeIniConfigurator.DisableAutoMenu(plan.Ai.RuntimeRoot);
    }
}

public static class CncDdrawConfigurator
{
    public const string Version = "v7.1.0.0";
    public const string DownloadUrl = "https://github.com/FunkyFr3sh/cnc-ddraw/releases/download/v7.1.0.0/cnc-ddraw.zip";

    public static void Apply(string runtimeRoot, CncDdrawMode mode)
    {
        if (mode == CncDdrawMode.Disabled)
        {
            return;
        }

        var dependencyRoot = EnsureDependency();
        CopyIfDifferent(Path.Combine(dependencyRoot, "ddraw.dll"), Path.Combine(runtimeRoot, "ddraw.dll"));
        CopyDirectory(Path.Combine(dependencyRoot, "Shaders"), Path.Combine(runtimeRoot, "Shaders"));
        File.WriteAllText(Path.Combine(runtimeRoot, "ddraw.ini"), RenderIni(mode), new UTF8Encoding(false));
    }

    public static string RenderIni(CncDdrawMode mode)
    {
        var borderless = mode == CncDdrawMode.BorderlessFullscreen;
        var width = borderless ? "0" : "640";
        var height = borderless ? "0" : "480";
        return $$"""
            ; Managed by StarAI Practice Client. Source: cnc-ddraw {{Version}}
            ; Runtime-only file. Original SCHNAIL and StarCraft folders are not modified.

            [ddraw]
            width={{width}}
            height={{height}}
            fullscreen={{FormatBool(borderless)}}
            windowed=true
            maintas={{FormatBool(borderless)}}
            aspect_ratio=
            boxing=false
            maxfps=-1
            vsync=false
            adjmouse={{FormatBool(borderless)}}
            shader=Shaders\interpolation\catmull-rom-bilinear.glsl
            posX=-32000
            posY=-32000
            renderer=auto
            devmode=false
            border={{FormatBool(!borderless)}}
            savesettings=0
            resizable=false
            d3d9_filter=2
            center_window=2
            inject_resolution=
            toggle_borderless=true
            toggle_upscaled=false
            noactivateapp=true
            maxgameticks=0
            limiter_type=0
            minfps=0
            nonexclusive=true
            singlecpu=false
            resolutions=0
            fixchilds=0
            hook_peekmessage=false
            no_compat_warning=true
            hook=4
            refresh_rate=0
            keytogglefullscreen=0x0D
            keytogglefullscreen2=0x00
            keytogglemaximize=0x22
            keytogglemaximize2=0x00
            keyunlockcursor1=0x09
            keyunlockcursor2=0xA3
            keyscreenshot=0x2C
            """;
    }

    private static string EnsureDependency()
    {
        var root = PracticePaths.Defaults().RepositoryRoot;
        var cacheRoot = Path.Combine(root, "artifacts", "deps", "cnc-ddraw", Version);
        var extractRoot = Path.Combine(cacheRoot, "extracted");
        var dllPath = Path.Combine(extractRoot, "ddraw.dll");
        var shaderPath = Path.Combine(extractRoot, "Shaders", "interpolation", "catmull-rom-bilinear.glsl");
        if (File.Exists(dllPath) && File.Exists(shaderPath))
        {
            return extractRoot;
        }

        Directory.CreateDirectory(cacheRoot);
        var zipPath = Path.Combine(cacheRoot, "cnc-ddraw.zip");
        if (!File.Exists(zipPath))
        {
            using var client = new HttpClient();
            using var response = client.GetAsync(DownloadUrl).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            using var zipStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var fileStream = File.Create(zipPath);
            zipStream.CopyTo(fileStream);
        }

        if (Directory.Exists(extractRoot))
        {
            Directory.Delete(extractRoot, recursive: true);
        }

        ZipFile.ExtractToDirectory(zipPath, extractRoot);
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException("cnc-ddraw ddraw.dll was not found in the release package.", dllPath);
        }

        return extractRoot;
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            CopyIfDifferent(sourcePath, Path.Combine(targetDirectory, relativePath));
        }
    }

    private static void CopyIfDifferent(string sourcePath, string targetPath)
    {
        var source = new FileInfo(sourcePath);
        var target = new FileInfo(targetPath);
        if (target.Exists &&
            target.Length == source.Length &&
            target.LastWriteTimeUtc >= source.LastWriteTimeUtc)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(sourcePath, targetPath, overwrite: true);
        File.SetLastWriteTimeUtc(targetPath, source.LastWriteTimeUtc);
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }
}
