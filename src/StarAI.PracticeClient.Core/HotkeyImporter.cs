using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public sealed record HotkeyImportResult(bool AppliedPatch, string Message, string? SourcePath, string? TargetPath);

public static partial class HotkeyImporter
{
    public const string DefaultSchnailRoot = @"C:\Program Files (x86)\SCHNAIL Client";
    public const string DefaultRemasteredSettingsPath = @"D:\OneDrive\Documents\StarCraft\CSettings.json";

    public static HotkeyImportResult ImportBestAvailable(
        string starCraftRoot,
        string schnailRoot = DefaultSchnailRoot,
        string remasteredSettingsPath = DefaultRemasteredSettingsPath)
    {
        Directory.CreateDirectory(starCraftRoot);
        var readDir = Path.Combine(starCraftRoot, "bwapi-data", "read");
        Directory.CreateDirectory(readDir);

        TryExportRemasteredHotkeys(remasteredSettingsPath, Path.Combine(readDir, "remastered_hotkeys.txt"));

        var sourceCsv = Path.Combine(schnailRoot, "res", "sc_hotkeys.csv");
        var workingCsv = Path.Combine(readDir, "sc_hotkeys.csv");
        if (File.Exists(sourceCsv) && !File.Exists(workingCsv))
        {
            File.Copy(sourceCsv, workingCsv, overwrite: true);
        }

        var patchResult = TryCopySchnailPatch(starCraftRoot, schnailRoot);

        return new HotkeyImportResult(
            patchResult.Applied,
            $"핫키 참고 파일을 갱신했습니다. {patchResult.Message}",
            File.Exists(sourceCsv) ? sourceCsv : remasteredSettingsPath,
            readDir);
    }

    private static (bool Applied, string Message) TryCopySchnailPatch(string starCraftRoot, string schnailRoot)
    {
        var sourcePatch = Path.Combine(schnailRoot, "starcraft_bundled", "patch_rt.mpq");
        var targetPatch = Path.Combine(starCraftRoot, "patch_rt.mpq");
        if (!File.Exists(sourcePatch))
        {
            return (false, "SCHNAIL patch_rt.mpq를 찾지 못해 실제 1.16.1 핫키 적용은 건너뛰었습니다.");
        }

        if (!File.Exists(targetPatch))
        {
            return (false, "StarCraft 폴더에 patch_rt.mpq가 없어 실제 1.16.1 핫키 적용은 건너뛰었습니다.");
        }

        try
        {
            var backup = targetPatch + ".starai-hotkey-original";
            if (!File.Exists(backup))
            {
                File.Copy(targetPatch, backup, overwrite: false);
            }

            File.Copy(sourcePatch, targetPatch, overwrite: true);
            return (true, "SCHNAIL patch_rt.mpq를 복사해서 1.16.1 핫키 적용을 시도했습니다.");
        }
        catch (IOException)
        {
            return (false, "StarCraft가 patch_rt.mpq를 사용 중이라 핫키 패치 갱신을 건너뛰었습니다. 이미 적용된 패치 파일로 계속 진행합니다.");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "StarCraft가 patch_rt.mpq를 사용 중이라 핫키 패치 갱신을 건너뛰었습니다. 이미 적용된 패치 파일로 계속 진행합니다.");
        }
    }

    private static void TryExportRemasteredHotkeys(string settingsPath, string outputPath)
    {
        if (!File.Exists(settingsPath))
        {
            return;
        }

        var text = File.ReadAllText(settingsPath);
        var match = HotkeysRegex().Match(text);
        if (!match.Success)
        {
            return;
        }

        var hotkeys = Regex.Unescape(match.Groups["hotkeys"].Value).Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        File.WriteAllText(outputPath, hotkeys);
    }

    [GeneratedRegex("\"Hotkeys\"\\s*:\\s*\"(?<hotkeys>(?:\\\\.|[^\"\\\\])*)\"", RegexOptions.Singleline)]
    private static partial Regex HotkeysRegex();
}
