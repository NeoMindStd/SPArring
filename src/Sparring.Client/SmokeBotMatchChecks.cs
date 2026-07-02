using Sparring.Core;
using System.Diagnostics;

namespace Sparring.Client;

internal static partial class SmokeChecks
{
    public static int RunBotMatch(IReadOnlyList<string>? args = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return 1;
        }

        var request = SmokeBotMatchRequest.Parse(args ?? []);
        var basePaths = PracticePaths.Defaults();
        var botMatchRoot = Path.Combine(
            Path.GetDirectoryName(basePaths.PlayerRuntimeRoot) ?? @"C:\sparring",
            "bot-match");
        var leftRoot = Path.Combine(botMatchRoot, "left");
        var rightRoot = Path.Combine(botMatchRoot, "right");
        var paths = basePaths with
        {
            PlayerRuntimeRoot = leftRoot,
            AiRuntimeRoot = rightRoot
        };
        var runtimeOptions = PracticeRuntimeOptions.Defaults();
        if (!string.IsNullOrWhiteSpace(request.ReplayRoot))
        {
            runtimeOptions = runtimeOptions with { ReplayRoot = request.ReplayRoot.Trim() };
            Directory.CreateDirectory(runtimeOptions.ReplayRoot);
        }

        LocalRuntimeProcessCleaner? cleaner = null;
        HashSet<int>? preExistingStarCraftProcessIds = null;
        var runtimeLaunchStarted = false;
        int? launchedLeftStarCraftProcessId = null;
        int? launchedRightStarCraftProcessId = null;
        WindowsApplicationErrorDialogCloser.CloseAllDialogs();
        var applicationErrorDialogBaseline = WindowsApplicationErrorDialogCloser.Capture();
        IDisposable? applicationErrorDialogCloser = null;

        try
        {
            var catalog = LoadSmokeStartCatalog(basePaths, includeConfiguredMaps: !request.BundledCatalogOnly);
            var leftBot = SelectDllBot(catalog.Bots, request.LeftBotName, "left");
            var rightBot = SelectDllBot(catalog.Bots, request.RightBotName, "right");
            var map = SelectBotMatchMap(catalog, leftBot, rightBot, request.MapName, request.AllowIncompatible);
            if (request.DryRun)
            {
                Console.WriteLine(
                    $"smoke-bot-match dry-run: left={leftBot.Name}, right={rightBot.Name}, map={map.Name}, " +
                    $"leftRuntime={leftRoot}, rightRuntime={rightRoot}, allowIncompatible={request.AllowIncompatible}, " +
                    $"untilEnd={request.UntilEnd}, replayRoot={runtimeOptions.ReplayRoot}");
                return 0;
            }

            cleaner = new LocalRuntimeProcessCleaner();
            cleaner.Stop(leftRoot, rightRoot);
            RuntimeProvisioner.CopyRuntimeBase(basePaths.PlayerRuntimeRoot, leftRoot);
            RuntimeProvisioner.CopyRuntimeBase(basePaths.PlayerRuntimeRoot, rightRoot);

            var launchCatalog = request.AllowIncompatible
                ? MarkMapAsUserMapForSmokeVerification(catalog, map.Id)
                : catalog;
            var plan = BotMatchLaunchPlanBuilder.Build(
                launchCatalog,
                paths,
                new BotMatchSelection(
                    leftBot.Id,
                    rightBot.Id,
                    map.Id,
                    "Sparring Bot Match",
                    request.LeftBotBuildId,
                    request.RightBotBuildId,
                    request.AllowIncompatible));
            var prepared = RuntimeProvisioner.PrepareBotMatchRuntimeAssets(plan);
            PracticeRuntimeConfigurator.Apply(prepared, runtimeOptions);
            var leftRuntimeErrorSnapshot = RuntimeErrorLogSnapshot.Capture(Path.Combine(leftRoot, "Errors"));
            var rightRuntimeErrorSnapshot = RuntimeErrorLogSnapshot.Capture(Path.Combine(rightRoot, "Errors"));
            if (request.PrepareOnly)
            {
                var prepareOk = VerifyPreparedBotMatch(prepared);
                Console.WriteLine(
                    $"smoke-bot-match prepare-only: left={leftBot.Name}, right={rightBot.Name}, map={map.Name}, " +
                    $"leftAi='{prepared.Left.AiModule}', rightAi='{prepared.Right.AiModule}', ok={prepareOk}");
                return prepareOk ? 0 : 1;
            }

            ApplicationConfiguration.Initialize();
            preExistingStarCraftProcessIds = CurrentStarCraftProcessIds();
            applicationErrorDialogCloser = WindowsApplicationErrorDialogCloser
                .CloseNewDialogsUntilDisposed(applicationErrorDialogBaseline, TimeSpan.FromMilliseconds(500));
            runtimeLaunchStarted = true;
            var launchStartUtc = DateTime.UtcNow;
            var report = new PracticeSessionLauncher().Launch(
                prepared,
                runtimeOptions,
                PracticeSessionLaunchOptions.Defaults() with
                {
                    StarCraftStartupTimeout = TimeSpan.FromSeconds(50),
                    AiLaunchDelay = TimeSpan.FromSeconds(4),
                    StopExistingLocalRuntime = true
                });
            launchedLeftStarCraftProcessId = report.Left.StarCraftProcessId;
            launchedRightStarCraftProcessId = report.Right.StarCraftProcessId;
            Console.WriteLine(
                $"smoke-bot-match: left={leftBot.Name}, right={rightBot.Name}, map={map.Name}, " +
                $"leftPid={report.Left.StarCraftProcessId}, rightPid={report.Right.StarCraftProcessId}, " +
                $"leftPath={ProcessPath(report.Left.StarCraftProcessId)}, rightPath={ProcessPath(report.Right.StarCraftProcessId)}");

            var screenBounds = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
            var (leftBounds, rightBounds) = BotMatchWindowLayout.SideBySide(screenBounds);
            var leftArranged = ArrangeBotMatchWindow(report.Left.StarCraftProcessId, leftBounds);
            var rightArranged = ArrangeBotMatchWindow(report.Right.StarCraftProcessId, rightBounds);
            if (report.Right.StarCraftProcessId is not null)
            {
                _ = StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(
                    report.Right.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(2));
            }

            if (report.Left.StarCraftProcessId is not null)
            {
                _ = StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(
                    report.Left.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(2));
            }

            var leftInGame = WaitForBotMatchInGame(report.Left.StarCraftProcessId, TimeSpan.FromSeconds(30));
            var rightInGame = WaitForBotMatchInGame(report.Right.StarCraftProcessId, TimeSpan.FromSeconds(30));
            var leftState = report.Left.StarCraftProcessId is null
                ? StarCraftScreenState.Unknown
                : StarCraftScreenDetector.Detect(report.Left.StarCraftProcessId.Value);
            var rightState = report.Right.StarCraftProcessId is null
                ? StarCraftScreenState.Unknown
                : StarCraftScreenDetector.Detect(report.Right.StarCraftProcessId.Value);
            SaveSmokeWindowScreenshot(basePaths, report.Left.StarCraftProcessId, "smoke-bot-match-left-final.png");
            SaveSmokeWindowScreenshot(basePaths, report.Right.StarCraftProcessId, "smoke-bot-match-right-final.png");
            BotMatchUntilEndResult? untilEndResult = null;

            if (request.ObserveSeconds > 0 && leftInGame && rightInGame)
            {
                PumpWinFormsFor(TimeSpan.FromSeconds(request.ObserveSeconds));
                leftState = report.Left.StarCraftProcessId is null
                    ? StarCraftScreenState.Unknown
                    : StarCraftScreenDetector.Detect(report.Left.StarCraftProcessId.Value);
                rightState = report.Right.StarCraftProcessId is null
                    ? StarCraftScreenState.Unknown
                    : StarCraftScreenDetector.Detect(report.Right.StarCraftProcessId.Value);
                SaveSmokeWindowScreenshot(basePaths, report.Left.StarCraftProcessId, "smoke-bot-match-left-observe.png");
                SaveSmokeWindowScreenshot(basePaths, report.Right.StarCraftProcessId, "smoke-bot-match-right-observe.png");
            }

            if (request.UntilEnd && leftInGame && rightInGame)
            {
                untilEndResult = MonitorBotMatchUntilEnd(
                    basePaths,
                    report.Left.StarCraftProcessId,
                    report.Right.StarCraftProcessId,
                    runtimeOptions.ReplayRoot,
                    launchStartUtc,
                    TimeSpan.FromSeconds(request.MaxSeconds),
                    TimeSpan.FromSeconds(request.SampleSeconds));
                leftState = untilEndResult.LeftState;
                rightState = untilEndResult.RightState;
            }

            if (ShouldFlushReplayAfterBotMatchEnd(
                    request.UntilEnd,
                    request.KeepOpen,
                    untilEndResult?.Ended ?? false,
                    untilEndResult?.NewReplayFiles.Count ?? 0))
            {
                if (report.Left.StarCraftProcessId is { } leftProcessId)
                {
                    StarCraftGameExitController.LeaveGameThenCloseProcess(
                        leftProcessId,
                        TimeSpan.FromSeconds(8),
                        TimeSpan.FromSeconds(4));
                }

                if (report.Right.StarCraftProcessId is { } rightProcessId)
                {
                    StarCraftGameExitController.LeaveGameThenCloseProcess(
                        rightProcessId,
                        TimeSpan.FromSeconds(8),
                        TimeSpan.FromSeconds(4));
                }

                StarCraftGameExitController.RemoveExpectedShutdownCrashes(
                    Path.Combine(leftRoot, "Errors"),
                    FinalRuntimeShutdownCrashCleanupRetryWindow,
                    FinalRuntimeShutdownCrashInitialQuietWindow);
                StarCraftGameExitController.RemoveExpectedShutdownCrashes(
                    Path.Combine(rightRoot, "Errors"),
                    FinalRuntimeShutdownCrashCleanupRetryWindow,
                    FinalRuntimeShutdownCrashInitialQuietWindow);

                var flushedReplayFiles = WaitForReplayFilesAfterEnd(
                    () => FindNewReplayFiles(runtimeOptions.ReplayRoot, launchStartUtc),
                    TimeSpan.FromSeconds(20),
                    TimeSpan.FromMilliseconds(500),
                    PumpWinFormsFor);
                if (flushedReplayFiles.Count > 0 && untilEndResult is not null)
                {
                    untilEndResult = untilEndResult with { NewReplayFiles = flushedReplayFiles };
                }
            }

            var leftAlive = report.Left.StarCraftProcessId is not null &&
                IsProcessRunning(report.Left.StarCraftProcessId.Value);
            var rightAlive = report.Right.StarCraftProcessId is not null &&
                IsProcessRunning(report.Right.StarCraftProcessId.Value);
            var closedApplicationErrorDialogs = WindowsApplicationErrorDialogCloser
                .CloseNewDialogs(applicationErrorDialogBaseline);
            var newLeftRuntimeErrors = RuntimeErrorLogSnapshot.Capture(Path.Combine(leftRoot, "Errors"))
                .FindNewOrChanged(leftRuntimeErrorSnapshot);
            var newRightRuntimeErrors = RuntimeErrorLogSnapshot.Capture(Path.Combine(rightRoot, "Errors"))
                .FindNewOrChanged(rightRuntimeErrorSnapshot);
            var leftRuntimeErrorsClean = newLeftRuntimeErrors.Count == 0;
            var rightRuntimeErrorsClean = newRightRuntimeErrors.Count == 0;
            var untilEndReplayCount = untilEndResult?.NewReplayFiles.Count ?? 0;
            var untilEndOk = !request.UntilEnd ||
                             untilEndResult is { Ended: true } &&
                             untilEndReplayCount > 0;
            var aliveOk = request.UntilEnd && untilEndResult is { Ended: true }
                ? true
                : leftAlive && rightAlive;
            var passed = report.Left.CompletedStartCount > 0 &&
                         report.Right.CompletedStartCount > 0 &&
                         leftInGame &&
                         rightInGame &&
                         aliveOk &&
                         leftRuntimeErrorsClean &&
                         rightRuntimeErrorsClean &&
                         untilEndOk;
            Console.Error.WriteLine(
                $"smoke-bot-match: left={leftBot.Name}, right={rightBot.Name}, map={map.Name}, " +
                $"leftStarts={report.Left.CompletedStartCount}, rightStarts={report.Right.CompletedStartCount}, " +
                $"leftPid={report.Left.StarCraftProcessId?.ToString() ?? "null"}, rightPid={report.Right.StarCraftProcessId?.ToString() ?? "null"}, " +
                $"leftArranged={leftArranged}, rightArranged={rightArranged}, leftState={leftState}, rightState={rightState}, " +
                $"leftInGame={leftInGame}, rightInGame={rightInGame}, leftAlive={leftAlive}, rightAlive={rightAlive}, " +
                $"leftRuntimeErrorsClean={leftRuntimeErrorsClean}, rightRuntimeErrorsClean={rightRuntimeErrorsClean}, " +
                $"leftRuntimeErrorFiles={RuntimeErrorLogSnapshot.Format(newLeftRuntimeErrors)}, " +
                $"rightRuntimeErrorFiles={RuntimeErrorLogSnapshot.Format(newRightRuntimeErrors)}, " +
                $"closedApplicationErrorDialogs={closedApplicationErrorDialogs}, " +
                $"untilEnd={request.UntilEnd}, botMatchEnded={untilEndResult?.Ended.ToString() ?? "n/a"}, " +
                $"endReason={untilEndResult?.Reason ?? "n/a"}, newReplayFiles={FormatReplayFiles(untilEndResult?.NewReplayFiles)}, " +
                $"keepOpen={request.KeepOpen}");

            if (request.KeepOpen)
            {
                PracticeRuntimeConfigurator.DisableAutoMenuAfterGameStart(prepared);
                return passed ? 0 : 1;
            }

            WindowsApplicationErrorDialogCloser.CloseNewDialogs(applicationErrorDialogBaseline);
            cleaner?.StopKnown(report.Right.StarCraftProcessId, report.Left.StarCraftProcessId);
            WindowsApplicationErrorDialogCloser.CloseNewDialogs(applicationErrorDialogBaseline);

            return passed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (!request.KeepOpen && runtimeLaunchStarted)
            {
                WindowsApplicationErrorDialogCloser.CloseNewDialogs(applicationErrorDialogBaseline);
                cleaner?.StopKnown(launchedLeftStarCraftProcessId, launchedRightStarCraftProcessId);
                cleaner?.Stop(leftRoot, rightRoot);
                if (preExistingStarCraftProcessIds is not null)
                {
                    StopNewStarCraftProcesses(preExistingStarCraftProcessIds);
                }

                StarCraftGameExitController.RemoveExpectedShutdownCrashes(
                    Path.Combine(leftRoot, "Errors"),
                    FinalRuntimeShutdownCrashCleanupRetryWindow,
                    FinalRuntimeShutdownCrashInitialQuietWindow);
                StarCraftGameExitController.RemoveExpectedShutdownCrashes(
                    Path.Combine(rightRoot, "Errors"),
                    FinalRuntimeShutdownCrashCleanupRetryWindow,
                    FinalRuntimeShutdownCrashInitialQuietWindow);
            }

            applicationErrorDialogCloser?.Dispose();
        }
    }

    private static bool ArrangeBotMatchWindow(int? processId, Rectangle bounds)
    {
        return processId is not null &&
            StarCraftBorderlessWindow.MoveProcessWindowWhenReady(
                processId.Value,
                bounds,
                TimeSpan.FromSeconds(8));
    }

    private static bool WaitForBotMatchInGame(int? processId, TimeSpan timeout)
    {
        if (processId is null)
        {
            return false;
        }

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (!IsProcessRunning(processId.Value))
            {
                return false;
            }

            if (StarCraftScreenDetector.Detect(processId.Value) == StarCraftScreenState.InGame)
            {
                return true;
            }

            PumpWinFormsFor(TimeSpan.FromMilliseconds(250));
        }

        return false;
    }

    private static BotMatchUntilEndResult MonitorBotMatchUntilEnd(
        PracticePaths paths,
        int? leftProcessId,
        int? rightProcessId,
        string replayRoot,
        DateTime launchStartUtc,
        TimeSpan maxDuration,
        TimeSpan sampleInterval)
    {
        var monitor = new BotMatchEndMonitor(stableNonInGameSamples: 3, replayGraceSamples: 4);
        var startedUtc = DateTime.UtcNow;
        var lastScreenshotUtc = DateTime.MinValue;
        var lastLeftState = StarCraftScreenState.Unknown;
        var lastRightState = StarCraftScreenState.Unknown;
        IReadOnlyList<string> lastNewReplays = [];

        while (DateTime.UtcNow - startedUtc < maxDuration)
        {
            PumpWinFormsFor(sampleInterval);
            var leftAlive = leftProcessId is not null && IsProcessRunning(leftProcessId.Value);
            var rightAlive = rightProcessId is not null && IsProcessRunning(rightProcessId.Value);
            lastLeftState = leftAlive && leftProcessId is not null
                ? StarCraftScreenDetector.Detect(leftProcessId.Value)
                : StarCraftScreenState.Unknown;
            lastRightState = rightAlive && rightProcessId is not null
                ? StarCraftScreenDetector.Detect(rightProcessId.Value)
                : StarCraftScreenState.Unknown;
            lastNewReplays = FindNewReplayFiles(replayRoot, launchStartUtc);

            var observation = monitor.Observe(new BotMatchEndSample(
                leftAlive,
                rightAlive,
                lastLeftState,
                lastRightState,
                lastNewReplays.Count > 0));

            var now = DateTime.UtcNow;
            if (now - lastScreenshotUtc >= TimeSpan.FromMinutes(2))
            {
                var elapsedSeconds = (int)Math.Round((now - startedUtc).TotalSeconds);
                SaveSmokeWindowScreenshot(paths, leftProcessId, $"smoke-bot-match-left-until-end-{elapsedSeconds:0000}.png");
                SaveSmokeWindowScreenshot(paths, rightProcessId, $"smoke-bot-match-right-until-end-{elapsedSeconds:0000}.png");
                lastScreenshotUtc = now;
            }

            if (observation.Ended)
            {
                SaveSmokeWindowScreenshot(paths, leftProcessId, "smoke-bot-match-left-end.png");
                SaveSmokeWindowScreenshot(paths, rightProcessId, "smoke-bot-match-right-end.png");
                if (lastNewReplays.Count == 0)
                {
                    lastNewReplays = WaitForReplayFilesAfterEnd(
                        () => FindNewReplayFiles(replayRoot, launchStartUtc),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromMilliseconds(500),
                        PumpWinFormsFor);
                }

                return new BotMatchUntilEndResult(
                    Ended: true,
                    Reason: observation.Reason,
                    LeftState: lastLeftState,
                    RightState: lastRightState,
                    NewReplayFiles: lastNewReplays);
            }
        }

        SaveSmokeWindowScreenshot(paths, leftProcessId, "smoke-bot-match-left-timeout.png");
        SaveSmokeWindowScreenshot(paths, rightProcessId, "smoke-bot-match-right-timeout.png");
        return new BotMatchUntilEndResult(
            Ended: false,
            Reason: "timeout",
            LeftState: lastLeftState,
            RightState: lastRightState,
            NewReplayFiles: lastNewReplays);
    }

    private static IReadOnlyList<string> FindNewReplayFiles(string replayRoot, DateTime launchStartUtc)
    {
        if (string.IsNullOrWhiteSpace(replayRoot) || !Directory.Exists(replayRoot))
        {
            return [];
        }

        try
        {
            var threshold = launchStartUtc.AddSeconds(-5);
            return Directory
                .EnumerateFiles(replayRoot, "*.rep", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .Where(file => file.Exists && file.LastWriteTimeUtc >= threshold)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => file.FullName)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    internal static IReadOnlyList<string> WaitForReplayFilesAfterEnd(
        Func<IReadOnlyList<string>> findReplayFiles,
        TimeSpan timeout,
        TimeSpan pollInterval,
        Action<TimeSpan>? wait = null)
    {
        var attempts = Math.Max(
            1,
            (int)Math.Ceiling(timeout.TotalMilliseconds / Math.Max(1, pollInterval.TotalMilliseconds)));
        var waitAction = wait ?? (duration => Thread.Sleep(duration));
        IReadOnlyList<string> replayFiles = [];

        for (var attempt = 0; attempt <= attempts; attempt++)
        {
            replayFiles = findReplayFiles();
            if (replayFiles.Count > 0 || attempt == attempts)
            {
                return replayFiles;
            }

            waitAction(pollInterval);
        }

        return replayFiles;
    }

    internal static bool ShouldFlushReplayAfterBotMatchEnd(
        bool untilEnd,
        bool keepOpen,
        bool ended,
        int replayCount)
    {
        return untilEnd &&
               !keepOpen &&
               ended &&
               replayCount == 0;
    }

    private static string FormatReplayFiles(IReadOnlyList<string>? replayFiles)
    {
        if (replayFiles is null || replayFiles.Count == 0)
        {
            return "none";
        }

        return string.Join(";", replayFiles.Select(Path.GetFileName));
    }

    private static PracticeBot SelectDllBot(IReadOnlyList<PracticeBot> bots, string? requestedName, string side)
    {
        var candidates = bots
            .Where(bot => bot.ExecutableKind == BotExecutableKind.Dll)
            .ToList();
        if (string.IsNullOrWhiteSpace(requestedName) ||
            string.Equals(requestedName, "Random", StringComparison.OrdinalIgnoreCase))
        {
            return candidates
                .OrderByDescending(bot => bot.Elo ?? int.MinValue)
                .First();
        }

        return candidates.FirstOrDefault(bot => string.Equals(bot.Name, requestedName, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(bot => bot.Name.Contains(requestedName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Bot-match {side} bot was not found: {requestedName}");
    }

    private static PracticeMap SelectBotMatchMap(
        PracticeCatalog catalog,
        PracticeBot leftBot,
        PracticeBot rightBot,
        string? requestedName,
        bool allowIncompatible)
    {
        var candidates = catalog.Maps
            .Where(map => map.Enabled)
            .Where(map => allowIncompatible ||
                          PracticeCatalogCompatibility.IsCompatible(catalog, leftBot.Id, map.Id) &&
                          PracticeCatalogCompatibility.IsCompatible(catalog, rightBot.Id, map.Id))
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No bot-match map candidates were available.");
        }

        if (string.IsNullOrWhiteSpace(requestedName) ||
            string.Equals(requestedName, "Random", StringComparison.OrdinalIgnoreCase))
        {
            return candidates.First();
        }

        return candidates.FirstOrDefault(map => string.Equals(map.Name, requestedName, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(map => string.Equals(map.FileName, requestedName, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(map =>
                map.Name.Contains(requestedName, StringComparison.OrdinalIgnoreCase) ||
                map.FileName.Contains(requestedName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Bot-match map was not found: {requestedName}");
    }

    private static bool VerifyPreparedBotMatch(BotMatchLaunchPlan prepared)
    {
        return VerifyPreparedBotClient(prepared.Left, expectMap: true) &&
               VerifyPreparedBotClient(prepared.Right, expectMap: false);
    }

    private static bool VerifyPreparedBotClient(ClientLaunchSettings settings, bool expectMap)
    {
        var iniPath = Path.Combine(settings.RuntimeRoot, PracticeIniConfigurator.BwapiIniRelativePath);
        if (!File.Exists(iniPath))
        {
            return false;
        }

        var ini = IniDocument.Parse(File.ReadAllText(iniPath));
        var aiModule = ini.Get("ai", "ai");
        var map = ini.Get("auto_menu", "map");
        return !string.IsNullOrWhiteSpace(aiModule) &&
               File.Exists(Path.Combine(settings.RuntimeRoot, settings.AiModule)) &&
               string.Equals(aiModule, settings.AiModule.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase) &&
               string.IsNullOrWhiteSpace(ini.Get("ai", "tournament")) &&
               (expectMap ? !string.IsNullOrWhiteSpace(map) : string.IsNullOrWhiteSpace(map));
    }

    private sealed record SmokeBotMatchRequest(
        string? LeftBotName,
        string? RightBotName,
        string? MapName,
        string? LeftBotBuildId,
        string? RightBotBuildId,
        int ObserveSeconds,
        bool DryRun,
        bool PrepareOnly,
        bool BundledCatalogOnly,
        bool AllowIncompatible,
        bool UntilEnd,
        int MaxSeconds,
        int SampleSeconds,
        string? ReplayRoot,
        bool KeepOpen)
    {
        public static SmokeBotMatchRequest Parse(IReadOnlyList<string> args)
        {
            return new SmokeBotMatchRequest(
                ValueAfter(args, "--left-bot") ?? ValueAfter(args, "--bot-left") ?? ValueAfter(args, "--bot"),
                ValueAfter(args, "--right-bot") ?? ValueAfter(args, "--bot-right"),
                ValueAfter(args, "--map"),
                ValueAfter(args, "--left-build") ?? ValueAfter(args, "--left-bot-build"),
                ValueAfter(args, "--right-build") ?? ValueAfter(args, "--right-bot-build"),
                ParseObserveSeconds(ValueAfter(args, "--observe-seconds")),
                args.Any(arg => string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase)),
                args.Any(arg => string.Equals(arg, "--prepare-only", StringComparison.OrdinalIgnoreCase)),
                args.Any(arg => string.Equals(arg, "--bundled-catalog-only", StringComparison.OrdinalIgnoreCase)),
                args.Any(arg => string.Equals(arg, "--allow-incompatible", StringComparison.OrdinalIgnoreCase)),
                args.Any(arg => string.Equals(arg, "--until-end", StringComparison.OrdinalIgnoreCase)),
                ParseBoundedSeconds(ValueAfter(args, "--max-seconds"), 1800, 10, 7200, "bot-match max seconds"),
                ParseBoundedSeconds(ValueAfter(args, "--sample-seconds"), 5, 1, 60, "bot-match sample seconds"),
                ValueAfter(args, "--replay-root"),
                args.Any(arg => string.Equals(arg, "--keep-open", StringComparison.OrdinalIgnoreCase)));
        }

        private static string? ValueAfter(IReadOnlyList<string> args, string name)
        {
            for (var index = 0; index < args.Count - 1; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return null;
        }

        private static int ParseObserveSeconds(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return int.TryParse(value, out var seconds)
                ? Math.Clamp(seconds, 0, 600)
                : throw new InvalidOperationException($"Invalid bot-match observe seconds: {value}");
        }

        private static int ParseBoundedSeconds(
            string? value,
            int defaultValue,
            int min,
            int max,
            string label)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return int.TryParse(value, out var seconds)
                ? Math.Clamp(seconds, min, max)
                : throw new InvalidOperationException($"Invalid {label}: {value}");
        }
    }

    internal sealed record BotMatchEndSample(
        bool LeftAlive,
        bool RightAlive,
        StarCraftScreenState LeftState,
        StarCraftScreenState RightState,
        bool HasNewReplay);

    internal sealed record BotMatchEndObservation(
        bool Ended,
        string Reason,
        int StableNonInGameSamples,
        int ReplayGraceSamples,
        bool SawBothInGame);

    internal sealed class BotMatchEndMonitor
    {
        private readonly int _stableNonInGameSamples;
        private readonly int _replayGraceSamples;
        private bool _sawBothInGame;
        private int _nonInGameSamples;
        private int _replayGraceSamplesElapsed;

        public BotMatchEndMonitor(int stableNonInGameSamples, int replayGraceSamples)
        {
            _stableNonInGameSamples = Math.Max(1, stableNonInGameSamples);
            _replayGraceSamples = Math.Max(1, replayGraceSamples);
        }

        public BotMatchEndObservation Observe(BotMatchEndSample sample)
        {
            if (_sawBothInGame && sample.HasNewReplay)
            {
                return Ended("replay-created");
            }

            if (sample.LeftAlive &&
                sample.RightAlive &&
                sample.LeftState == StarCraftScreenState.InGame &&
                sample.RightState == StarCraftScreenState.InGame)
            {
                _sawBothInGame = true;
                _nonInGameSamples = 0;
                _replayGraceSamplesElapsed = 0;
                return Active();
            }

            if (!_sawBothInGame)
            {
                return Active();
            }

            if (!sample.LeftAlive || !sample.RightAlive)
            {
                return Ended("process-exited");
            }

            if (sample.LeftState != StarCraftScreenState.InGame ||
                sample.RightState != StarCraftScreenState.InGame)
            {
                _nonInGameSamples++;
                if (_nonInGameSamples >= _stableNonInGameSamples)
                {
                    _replayGraceSamplesElapsed++;
                    if (_replayGraceSamplesElapsed >= _replayGraceSamples)
                    {
                        return Ended("stable-non-ingame");
                    }
                }
            }
            else
            {
                _nonInGameSamples = 0;
                _replayGraceSamplesElapsed = 0;
            }

            return Active();
        }

        private BotMatchEndObservation Active()
        {
            return new BotMatchEndObservation(
                Ended: false,
                Reason: "running",
                StableNonInGameSamples: _nonInGameSamples,
                ReplayGraceSamples: _replayGraceSamplesElapsed,
                SawBothInGame: _sawBothInGame);
        }

        private BotMatchEndObservation Ended(string reason)
        {
            return new BotMatchEndObservation(
                Ended: true,
                Reason: reason,
                StableNonInGameSamples: _nonInGameSamples,
                ReplayGraceSamples: _replayGraceSamplesElapsed,
                SawBothInGame: _sawBothInGame);
        }
    }

    private sealed record BotMatchUntilEndResult(
        bool Ended,
        string Reason,
        StarCraftScreenState LeftState,
        StarCraftScreenState RightState,
        IReadOnlyList<string> NewReplayFiles);
}
