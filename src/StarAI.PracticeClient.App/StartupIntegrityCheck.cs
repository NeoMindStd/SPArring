using StarAI.PracticeClient.Core;
using System.Diagnostics;

namespace StarAI.PracticeClient.App;

internal sealed record RuntimeRepairRequest(
    string InstallRoot,
    string PlayerRuntimeRoot,
    string AiRuntimeRoot,
    string? StarCraftSourceRoot);

internal sealed record StartupIntegrityCheckReport(
    bool ManifestFound,
    IReadOnlyList<InstallationVerificationIssue> AppIssuesBeforeRepair,
    InstallationRepairResult? AppRepairResult,
    IReadOnlyList<InstallationVerificationIssue> AppIssuesAfterRepair,
    IReadOnlyList<string> RuntimeMissingBeforeRepair,
    bool RuntimeRepairAttempted,
    int? RuntimeRepairExitCode,
    IReadOnlyList<string> RuntimeMissingAfterRepair,
    string? Error)
{
    public bool ShouldNotify =>
        Error is not null ||
        AppIssuesBeforeRepair.Count > 0 ||
        RuntimeMissingBeforeRepair.Count > 0;

    public bool FullyRepaired =>
        Error is null &&
        AppIssuesBeforeRepair.Count + RuntimeMissingBeforeRepair.Count > 0 &&
        AppIssuesAfterRepair.Count == 0 &&
        RuntimeMissingAfterRepair.Count == 0;
}

internal static class StartupIntegrityCheck
{
    public static StartupIntegrityCheckReport Run(PracticePaths paths)
    {
        return Run(paths, RunRuntimeSetup);
    }

    internal static StartupIntegrityCheckReport Run(
        PracticePaths paths,
        Func<RuntimeRepairRequest, int> runtimeRepair)
    {
        var installRoot = paths.RepositoryRoot;
        var state = InstallationVerifier.LoadState(Path.Combine(installRoot, InstallationVerifier.StateFileName));
        var manifestPath = Path.Combine(installRoot, InstallationVerifier.ManifestFileName);
        var appIssuesBefore = Array.Empty<InstallationVerificationIssue>();
        var appIssuesAfter = Array.Empty<InstallationVerificationIssue>();
        InstallationRepairResult? appRepairResult = null;

        try
        {
            var manifest = InstallationVerifier.LoadManifest(manifestPath);
            if (manifest.Count > 0)
            {
                appIssuesBefore = InstallationVerifier.Verify(installRoot, manifest).ToArray();
                if (appIssuesBefore.Length > 0)
                {
                    appRepairResult = InstallationVerifier.RepairFromPayloadZip(
                        installRoot,
                        InstallationVerifier.RepairPayloadPath(installRoot),
                        appIssuesBefore,
                        CanRepairAppPayloadFile);
                    appIssuesAfter = InstallationVerifier.Verify(installRoot, manifest).ToArray();
                }
            }

            var runtimeMissingBefore = InstallationVerifier.MissingRequiredRuntimeFiles(
                installRoot,
                paths.PlayerRuntimeRoot,
                paths.AiRuntimeRoot,
                state?.InstallJava ?? false);
            var runtimeMissingAfter = runtimeMissingBefore;
            var runtimeRepairAttempted = false;
            int? runtimeRepairExitCode = null;
            if (runtimeMissingBefore.Count > 0 && ShouldRunRuntimeRepair(runtimeMissingBefore))
            {
                runtimeRepairAttempted = true;
                runtimeRepairExitCode = runtimeRepair(new RuntimeRepairRequest(
                    installRoot,
                    paths.PlayerRuntimeRoot,
                    paths.AiRuntimeRoot,
                    state?.StarCraftSourceRoot));
                runtimeMissingAfter = InstallationVerifier.MissingRequiredRuntimeFiles(
                    installRoot,
                    paths.PlayerRuntimeRoot,
                    paths.AiRuntimeRoot,
                    state?.InstallJava ?? false);
            }

            return new StartupIntegrityCheckReport(
                ManifestFound: File.Exists(manifestPath),
                AppIssuesBeforeRepair: appIssuesBefore,
                AppRepairResult: appRepairResult,
                AppIssuesAfterRepair: appIssuesAfter,
                RuntimeMissingBeforeRepair: runtimeMissingBefore,
                RuntimeRepairAttempted: runtimeRepairAttempted,
                RuntimeRepairExitCode: runtimeRepairExitCode,
                RuntimeMissingAfterRepair: runtimeMissingAfter,
                Error: null);
        }
        catch (Exception ex)
        {
            return new StartupIntegrityCheckReport(
                ManifestFound: File.Exists(manifestPath),
                AppIssuesBeforeRepair: appIssuesBefore,
                AppRepairResult: appRepairResult,
                AppIssuesAfterRepair: appIssuesAfter,
                RuntimeMissingBeforeRepair: [],
                RuntimeRepairAttempted: false,
                RuntimeRepairExitCode: null,
                RuntimeMissingAfterRepair: [],
                Error: ex.Message);
        }
    }

    public static string FormatUserMessage(StartupIntegrityCheckReport report)
    {
        var lines = new List<string>();
        if (report.FullyRepaired)
        {
            lines.Add("StarAI 설치 파일 일부가 누락되었거나 손상되어 자동 복구했습니다.");
        }
        else
        {
            lines.Add("StarAI 설치 파일 일부를 복구하지 못했습니다.");
        }

        if (report.AppRepairResult is not null)
        {
            lines.Add($"앱/데이터 복구: {report.AppRepairResult.RestoredCount}/{report.AppRepairResult.RequestedCount}개");
        }

        if (report.RuntimeRepairAttempted)
        {
            var resultText = report.RuntimeRepairExitCode == 0 ? "성공" : $"실패(exit {report.RuntimeRepairExitCode})";
            lines.Add($"StarCraft/BWAPI 런타임 복구: {resultText}");
        }

        var unresolved = report.AppIssuesAfterRepair
            .Select(issue => issue.RelativePath)
            .Concat(report.RuntimeMissingAfterRepair)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
        if (unresolved.Count > 0)
        {
            lines.Add("");
            lines.Add("아직 확인이 필요한 파일:");
            lines.AddRange(unresolved.Select(item => "- " + item));
        }

        if (report.Error is not null)
        {
            lines.Add("");
            lines.Add("오류:");
            lines.Add(report.Error);
        }

        lines.Add("");
        lines.Add("Windows Defender, SmartScreen, 백신이 오래된 32비트 봇 DLL/EXE, BWAPI 파일, cnc-ddraw 파일을 격리했을 수 있습니다.");
        lines.Add("Windows 보안 > 바이러스 및 위협 방지 > 보호 기록에서 StarAI 관련 항목을 확인하고, 신뢰할 수 있는 공식 릴리즈 파일이면 복원 또는 허용을 선택하세요.");
        lines.Add(@"반복 차단되면 C:\starai, C:\starai\SC116AI, C:\starai\SC116AI_ai 폴더를 Windows 보안 예외에 추가한 뒤 설치 프로그램을 다시 실행하세요.");

        return string.Join(Environment.NewLine, lines);
    }

    private static bool CanRepairAppPayloadFile(string relativePath)
    {
        return !string.Equals(
            relativePath,
            "StarAI.PracticeClient.App.exe",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldRunRuntimeRepair(IReadOnlyCollection<string> missing)
    {
        return missing.Any(path =>
            path.Contains("SC116AI", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("SC116AI_ai", StringComparison.OrdinalIgnoreCase));
    }

    private static int RunRuntimeSetup(RuntimeRepairRequest request)
    {
        var scriptPath = Path.Combine(request.InstallRoot, "scripts", "setup-runtime.ps1");
        if (!File.Exists(scriptPath))
        {
            return -1;
        }

        var arguments = string.Join(
            " ",
            "-NoProfile",
            "-ExecutionPolicy Bypass",
            "-File", Quote(scriptPath),
            "-AppRoot", Quote(request.InstallRoot),
            "-PlayerRuntimeRoot", Quote(request.PlayerRuntimeRoot),
            "-AiRuntimeRoot", Quote(request.AiRuntimeRoot),
            "-NonInteractive");

        if (!string.IsNullOrWhiteSpace(request.StarCraftSourceRoot))
        {
            arguments += " -StarCraftSourceRoot " + Quote(request.StarCraftSourceRoot);
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            WorkingDirectory = request.InstallRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        if (process is null)
        {
            return -1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
