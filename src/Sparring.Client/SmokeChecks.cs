using Sparring.Core;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Sparring.Client;

internal static partial class SmokeChecks
{
    private static readonly TimeSpan FinalRuntimeShutdownCrashCleanupRetryWindow = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan FinalRuntimeShutdownCrashInitialQuietWindow = TimeSpan.FromSeconds(1);

    public static int Run()
    {
        var paths = PracticePaths.Defaults();
        var issues = RuntimeWritePolicy.ValidateLayout(paths);
        if (issues.Count > 0)
        {
            return 1;
        }

        if (!File.Exists(Path.Combine(paths.RepositoryRoot, "VERSION")))
        {
            return 1;
        }

        var catalog = PracticeAssetCatalogReader.Read(paths);
        if (catalog.Bots.Count == 0 || catalog.Maps.Count == 0)
        {
            return 1;
        }

        if (!catalog.Bots.Any(bot => PracticeCatalogCompatibility.MapsForBot(catalog, bot.Id).Any()))
        {
            return 1;
        }

        var mapIssues = ValidateBundledLadderMaps(catalog, paths);
        if (mapIssues.Count > 0)
        {
            Console.Error.WriteLine("smoke: bundled ladder map issues were found:");
            foreach (var issue in mapIssues)
            {
                Console.Error.WriteLine("- " + issue);
            }

            return 1;
        }

        var launcherUi = CaptureLauncherScreenshots(paths);
        if (!launcherUi.HasMapPreviewBox || !launcherUi.HasMapPreviewImage)
        {
            Console.Error.WriteLine("smoke: map preview box/image was not visible in the launcher.");
            return 1;
        }

        if (launcherUi.LayoutIssues.Count > 0)
        {
            Console.Error.WriteLine("smoke: launcher UI layout issues were found:");
            foreach (var issue in launcherUi.LayoutIssues)
            {
                Console.Error.WriteLine("- " + issue);
            }

            return 1;
        }

        return 0;
    }

    private static IReadOnlyList<string> ValidateBundledLadderMaps(PracticeCatalog catalog, PracticePaths paths)
    {
        var issues = new List<string>();
        var requiredNames = new[]
        {
            "(4)Fighting Spirit 1.4",
            "(4)Allegro 1.1",
            "(4)Retro 1.2",
            "(4)Polypoid 1.65",
            "(4)Polypoid 1.75",
            "(8)Scan Hunters 2025",
            "(8)uNiq Hunters 2026",
            "(2)Eclipse",
            "(2)Cross Game"
        };

        foreach (var requiredName in requiredNames)
        {
            var map = catalog.Maps.FirstOrDefault(candidate =>
                candidate.Name.Equals(requiredName, StringComparison.OrdinalIgnoreCase));
            if (map is null)
            {
                issues.Add($"{requiredName}: catalog entry missing");
                continue;
            }

            var mapPath = map.SourcePath ?? Path.Combine(paths.AssetRoot, "maps", map.FileName);
            if (!File.Exists(mapPath))
            {
                issues.Add($"{requiredName}: map file missing ({mapPath})");
            }

            if (!string.IsNullOrWhiteSpace(map.ImagePath))
            {
                var imagePath = Path.Combine(paths.AssetRoot, map.ImagePath);
                if (!File.Exists(imagePath))
                {
                    issues.Add($"{requiredName}: preview image missing ({imagePath})");
                }
            }
        }

        return issues;
    }

    public static int RunStart(IReadOnlyList<string>? args = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return 1;
        }

        var request = SmokeStartRequest.Parse(args ?? []);
        var paths = PracticePaths.Defaults();
        LocalRuntimeProcessCleaner? cleaner = null;
        HashSet<int>? preExistingStarCraftProcessIds = null;
        var runtimeLaunchStarted = false;
        int? launchedPlayerStarCraftProcessId = null;
        int? launchedAiStarCraftProcessId = null;
        StarCraftStartupTrace? startupTrace = null;
        Bitmap? aiActivityStartBitmap = null;
        Bitmap? aiActivityEndBitmap = null;

        try
        {
            var catalog = LoadSmokeStartCatalog(paths, includeConfiguredMaps: !request.BundledCatalogOnly);
            var playerRace = request.PlayerRaceOrDefault();
            var enemyRace = request.EnemyRaceOrDefault();
            var (bot, map) = request.IsLadderMode
                ? ResolveSmokeLadderSelection(catalog, request, enemyRace)
                : ResolveSmokeSparringSelection(catalog, request, enemyRace);
            if (request.DryRun)
            {
                Console.WriteLine(
                    $"smoke-start dry-run: mode={request.ModeLabel}, playerRace={playerRace}, " +
                    $"enemyRace={(enemyRace?.ToString() ?? "Any")}, bot={bot.Name}, map={map.Name}, " +
                    $"executable={bot.ExecutableName}, kind={bot.ExecutableKind}, botBuild={request.BotBuildId ?? "random"}, " +
                    $"allowIncompatible={request.AllowIncompatible}");
                return 0;
            }

            var aiRoot = RuntimeProvisioner.EnsureAiRoot(paths.PlayerRuntimeRoot);
            var selection = new PracticeSelection(
                bot.Id,
                map.Id,
                playerRace,
                "Sparring Smoke",
                PlayerBorderless: true,
                ClipCursor: false,
                AllowApmAlert: false,
                BotBuildId: request.BotBuildId);
            var launchCatalog = request.AllowIncompatible
                ? MarkMapAsUserMapForSmokeVerification(catalog, map.Id)
                : catalog;
            var plan = PracticeLaunchPlanBuilder.Build(launchCatalog, paths with { AiRuntimeRoot = aiRoot }, selection);
            if (request.PrepareOnly)
            {
                var prepared = RuntimeProvisioner.PrepareRuntimeAssets(plan);
                PracticeRuntimeConfigurator.Apply(prepared, PracticeRuntimeOptions.Defaults());
                var aiModuleExists = !string.IsNullOrWhiteSpace(prepared.Ai.AiModule) &&
                    File.Exists(Path.Combine(prepared.Ai.RuntimeRoot, prepared.Ai.AiModule));
                var botConfig = ReadSmokeBotConfig(prepared.Ai.RuntimeRoot);
                var botBuildOk = string.IsNullOrWhiteSpace(request.BotBuildId) ||
                    string.Equals(botConfig, $"build={request.BotBuildId.Trim()}", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine($"smoke-start prepare-only: bot={bot.Name}, map={map.Name}, playerAi='{prepared.Player.AiModule}', aiModule='{prepared.Ai.AiModule}', aiModuleExists={aiModuleExists}, botBuild='{request.BotBuildId ?? string.Empty}', botConfig='{botConfig}'");
                return string.IsNullOrEmpty(prepared.Player.AiModule) && aiModuleExists && botBuildOk ? 0 : 1;
            }

            if (ShouldInitializeWinFormsForStart(request.DryRun, request.PrepareOnly))
            {
                ApplicationConfiguration.Initialize();
            }

            var screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);
            var cncDdrawHandlesPlayerDisplay = plan.Player.CncDdrawMode == CncDdrawMode.BorderlessFullscreen;
            var borderlessApplied = false;
            var playerErrorDirectory = Path.Combine(paths.PlayerRuntimeRoot, "Errors");
            var aiWriteDirectory = Path.Combine(aiRoot, "bwapi-data", "write");
            var aiRuntimeErrorSnapshot = RuntimeErrorLogSnapshot.Capture(Path.Combine(aiRoot, "Errors"));
            var playerRuntimeErrorSnapshot = RuntimeErrorLogSnapshot.Capture(playerErrorDirectory);
            var aiActivityLogSnapshot = SmokeAiActivityLogSnapshot.Capture(aiWriteDirectory);
            var timing = Stopwatch.StartNew();
            cleaner = new LocalRuntimeProcessCleaner();
            preExistingStarCraftProcessIds = CurrentStarCraftProcessIds();
            runtimeLaunchStarted = true;
            var report = new PracticeSessionLauncher().Launch(
                plan,
                PracticeRuntimeOptions.Defaults(),
                PracticeSessionLaunchOptions.Defaults() with
                {
                    StarCraftStartupTimeout = TimeSpan.FromSeconds(45),
                    AiLaunchDelay = TimeSpan.FromSeconds(3),
                    StopExistingLocalRuntime = true
                });
            launchedPlayerStarCraftProcessId = report.Player.StarCraftProcessId;
            launchedAiStarCraftProcessId = report.Ai.StarCraftProcessId;
            var playerProcessPath = ProcessPath(report.Player.StarCraftProcessId);
            var aiProcessPath = ProcessPath(report.Ai.StarCraftProcessId);
            Console.WriteLine(
                $"smoke-start: bot={bot.Name}, map={map.Name}, " +
                $"playerPid={report.Player.StarCraftProcessId}, aiPid={report.Ai.StarCraftProcessId}, " +
                $"playerPath={playerProcessPath}, aiPath={aiProcessPath}");

            if (report.Player.StarCraftProcessId is not null)
            {
                borderlessApplied = cncDdrawHandlesPlayerDisplay ||
                    StarCraftBorderlessWindow.ApplyToProcessWhenReady(
                        report.Player.StarCraftProcessId.Value,
                        screenBounds,
                        TimeSpan.FromSeconds(8)).Applied;
                _ = StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(
                    report.Player.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(8));
                startupTrace = StarCraftStartupTrace.Start(paths, report.Player.StarCraftProcessId.Value, timing);
            }

            var inGameDetected = report.Player.StarCraftProcessId is not null &&
                StarCraftScreenDetector.WaitForInGameAsync(
                    report.Player.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(90)).GetAwaiter().GetResult();
            var playerHudDetectedMs = timing.ElapsedMilliseconds;

            var timerOverlayVisible = false;
            var overlayStartedMs = -1L;
            var overlayScreenshotMs = -1L;
            var playerStateAtOverlay = StarCraftScreenState.Unknown;
            PracticeOverlayForm? overlay = null;
            if (inGameDetected && report.Player.StarCraftProcessId is not null)
            {
                try
                {
                    var actionCounter = new ActionRateCounter();
                    overlay = new PracticeOverlayForm();
                    var overlayBounds = screenBounds;
                    if (StarCraftBorderlessWindow.TryGetProcessWindowBounds(
                        report.Player.StarCraftProcessId.Value,
                        out var playerWindowBounds))
                    {
                        overlayBounds = playerWindowBounds;
                    }

                    overlay.StartSession(overlayBounds, DateTime.UtcNow, actionCounter);
                    overlayStartedMs = timing.ElapsedMilliseconds;
                    timerOverlayVisible = WaitForPracticeOverlay(
                        () => CaptureContainsPracticeOverlay(report.Player.StarCraftProcessId.Value),
                        PumpWinFormsFor,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromMilliseconds(100));
                    SaveSmokeWindowScreenshot(paths, report.Player.StarCraftProcessId, "smoke-start-player-overlay.png");
                    overlayScreenshotMs = timing.ElapsedMilliseconds;
                    playerStateAtOverlay = StarCraftScreenDetector.Detect(report.Player.StarCraftProcessId.Value);
                }
                finally
                {
                    overlay?.Close();
                    overlay?.Dispose();
                }
            }

            var startupTraceSummary = startupTrace?.StopAndWait(TimeSpan.FromSeconds(3)) ??
                new StarCraftStartupTraceSummary(0, null, null, 0, null);

            var aiInGameDetected = report.Ai.StarCraftProcessId is not null &&
                StarCraftScreenDetector.WaitForInGameAsync(
                    report.Ai.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
            var aiState = report.Ai.StarCraftProcessId is null
                ? StarCraftScreenState.Unknown
                : StarCraftScreenDetector.Detect(report.Ai.StarCraftProcessId.Value);
            aiActivityStartBitmap = SaveSmokeWindowScreenshot(
                paths,
                report.Ai.StarCraftProcessId,
                "smoke-start-ai-final.png",
                keepBitmap: request.RequireAiActivity);
            var aiWindowMinimized = false;
            if (aiInGameDetected && report.Ai.StarCraftProcessId is not null)
            {
                using var aiMinimizer = new StarCraftWindowMinimizeOnceWhenReady(
                    report.Ai.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(8),
                    StarCraftScreenDetector.Detect,
                    StarCraftBorderlessWindow.MinimizeProcessWindowWhenReady,
                    startTimer: false);
                while (!aiMinimizer.StepOnce())
                {
                    Thread.Sleep(250);
                }

                Thread.Sleep(300);
                aiWindowMinimized = StarCraftBorderlessWindow.IsProcessWindowMinimized(report.Ai.StarCraftProcessId.Value);
            }

            var aiHudDetectedMs = timing.ElapsedMilliseconds;
            if (report.Player.StarCraftProcessId is not null)
            {
                _ = StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(
                    report.Player.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(3));
            }

            var observeMs = 0L;
            var observeElapsedMs = 0L;
            var playerProcessAliveAfterObserve = report.Player.StarCraftProcessId is null ||
                IsProcessRunning(report.Player.StarCraftProcessId.Value);
            var aiProcessAliveAfterObserve = report.Ai.StarCraftProcessId is null ||
                IsProcessRunning(report.Ai.StarCraftProcessId.Value);
            var playerStateAfterObserve = StarCraftScreenState.Unknown;
            var aiStateAfterObserve = StarCraftScreenState.Unknown;
            var observeStoppedEarly = false;
            var aiLogActivityDuringObserve = SmokeAiActivityLogSummary.Empty;
            if (request.ObserveSeconds > 0 &&
                inGameDetected &&
                (aiInGameDetected || report.Ai.StarCraftProcessId is not null))
            {
                var observeStartedMs = timing.ElapsedMilliseconds;
                var observeDuration = TimeSpan.FromSeconds(request.ObserveSeconds);
                void CapturePostObserveScreens()
                {
                    playerProcessAliveAfterObserve = report.Player.StarCraftProcessId is not null &&
                        IsProcessRunning(report.Player.StarCraftProcessId.Value);
                    aiProcessAliveAfterObserve = report.Ai.StarCraftProcessId is not null &&
                        IsProcessRunning(report.Ai.StarCraftProcessId.Value);
                    if (report.Ai.StarCraftProcessId is not null)
                    {
                        _ = StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(
                            report.Ai.StarCraftProcessId.Value,
                            TimeSpan.FromSeconds(3));
                        PumpWinFormsFor(TimeSpan.FromMilliseconds(500));
                        aiStateAfterObserve = StarCraftScreenDetector.Detect(report.Ai.StarCraftProcessId.Value);
                        aiProcessAliveAfterObserve = aiProcessAliveAfterObserve ||
                            aiStateAfterObserve != StarCraftScreenState.Unknown;
                    }

                    aiActivityEndBitmap?.Dispose();
                    aiActivityEndBitmap = SaveSmokeWindowScreenshot(
                        paths,
                        report.Ai.StarCraftProcessId,
                        "smoke-start-ai-observe.png",
                        keepBitmap: request.RequireAiActivity);
                    if (report.Player.StarCraftProcessId is not null)
                    {
                        _ = StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(
                            report.Player.StarCraftProcessId.Value,
                            TimeSpan.FromSeconds(3));
                        PumpWinFormsFor(TimeSpan.FromMilliseconds(500));
                        playerStateAfterObserve = StarCraftScreenDetector.Detect(report.Player.StarCraftProcessId.Value);
                        playerProcessAliveAfterObserve = playerProcessAliveAfterObserve ||
                            playerStateAfterObserve != StarCraftScreenState.Unknown;
                    }

                    SaveSmokeWindowScreenshot(paths, report.Player.StarCraftProcessId, "smoke-start-player-observe.png");
                    aiLogActivityDuringObserve = SmokeAiActivityLogSnapshot
                        .Capture(aiWriteDirectory)
                        .FindActivitySince(aiActivityLogSnapshot);
                }

                if (request.RequireAiActivity)
                {
                    var observeTimer = Stopwatch.StartNew();
                    while (observeTimer.Elapsed < observeDuration)
                    {
                        var remaining = observeDuration - observeTimer.Elapsed;
                        var interval = remaining < SmokeStartObservationPolicy.ActivityProbeInterval
                            ? remaining
                            : SmokeStartObservationPolicy.ActivityProbeInterval;
                        if (interval > TimeSpan.Zero)
                        {
                            PumpWinFormsFor(interval);
                        }

                        playerProcessAliveAfterObserve = report.Player.StarCraftProcessId is not null &&
                            IsProcessRunning(report.Player.StarCraftProcessId.Value);
                        aiProcessAliveAfterObserve = report.Ai.StarCraftProcessId is not null &&
                            IsProcessRunning(report.Ai.StarCraftProcessId.Value);
                        if (!playerProcessAliveAfterObserve || !aiProcessAliveAfterObserve)
                        {
                            break;
                        }

                        if (observeTimer.Elapsed < SmokeStartObservationPolicy.MinimumEarlyActivityObserveDuration &&
                            observeTimer.Elapsed < observeDuration)
                        {
                            continue;
                        }

                        CapturePostObserveScreens();
                        var currentActivity = SmokeScreenActivityAnalyzer.Compare(
                            aiActivityStartBitmap,
                            aiActivityEndBitmap);
                        var currentAiActivityDetected =
                            currentActivity.HasMeaningfulActivity ||
                            aiLogActivityDuringObserve.HasMeaningfulActivity;
                        if (SmokeStartObservationPolicy.CanStopEarlyAfterActivity(
                            request.ObserveSeconds,
                            request.RequireAiActivity,
                            observeTimer.Elapsed,
                            playerProcessAliveAfterObserve,
                            aiProcessAliveAfterObserve,
                            playerStateAfterObserve,
                            aiStateAfterObserve,
                            currentAiActivityDetected,
                            aiLogActivityDuringObserve.HasMeaningfulActivity))
                        {
                            observeStoppedEarly = true;
                            break;
                        }
                    }

                    if (aiActivityEndBitmap is null)
                    {
                        CapturePostObserveScreens();
                    }
                }
                else
                {
                    PumpWinFormsFor(observeDuration);
                    CapturePostObserveScreens();
                }

                observeMs = timing.ElapsedMilliseconds;
                observeElapsedMs = Math.Max(0, observeMs - observeStartedMs);
            }

            var playerState = report.Player.StarCraftProcessId is null
                ? StarCraftScreenState.Unknown
                : playerStateAtOverlay != StarCraftScreenState.Unknown
                    ? playerStateAtOverlay
                    : StarCraftScreenDetector.Detect(report.Player.StarCraftProcessId.Value);
            SaveSmokeWindowScreenshot(paths, report.Player.StarCraftProcessId, "smoke-start-player-final.png");
            var aiActivity = SmokeScreenActivityAnalyzer.Compare(aiActivityStartBitmap, aiActivityEndBitmap);
            var aiLogActivity = SmokeAiActivityLogSnapshot
                .Capture(aiWriteDirectory)
                .FindActivitySince(aiActivityLogSnapshot);
            var aiRuntimeActivityDetected =
                aiActivity.HasMeaningfulActivity ||
                aiLogActivity.HasMeaningfulActivity;
            var aiInGameVerified = aiInGameDetected || aiLogActivity.HasMeaningfulActivity;

            StarCraftAiShutdownResult? playerShutdown = null;
            var playerCleanupStopped = 0;
            var playerProcessGoneAfterCleanup = report.Player.StarCraftProcessId is null;
            if (inGameDetected && report.Player.StarCraftProcessId is not null)
            {
                playerShutdown = StarCraftGameExitController.LeaveGameThenCloseProcess(
                    report.Player.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(3));

                Thread.Sleep(1000);
                if (!playerShutdown.Exited)
                {
                    playerCleanupStopped = cleaner.StopKnown(report.Player.StarCraftProcessId.Value);
                }

                StarCraftGameExitController.RemoveExpectedShutdownCrashes(
                    playerErrorDirectory,
                    StarCraftGameExitController.ExpectedShutdownCrashCleanupRetryWindow);
                playerProcessGoneAfterCleanup = !IsProcessRunning(report.Player.StarCraftProcessId.Value);
            }

            StarCraftAiShutdownResult? aiShutdown = null;
            var playerStateAfterAiShutdown = StarCraftScreenState.Unknown;
            var aiCleanupStopped = 0;
            var aiProcessGoneAfterCleanup = false;
            var aiErrorDirectory = Path.Combine(aiRoot, "Errors");
            if (aiInGameVerified && report.Ai.StarCraftProcessId is not null)
            {
                aiShutdown = StarCraftGameExitController.LeaveGameThenCloseProcess(
                    report.Ai.StarCraftProcessId.Value,
                    TimeSpan.FromSeconds(8),
                    TimeSpan.FromSeconds(8));

                Thread.Sleep(1500);
                if (!aiShutdown.Exited)
                {
                    aiCleanupStopped = cleaner.StopKnown(report.Ai.StarCraftProcessId.Value);
                }

                StarCraftGameExitController.RemoveExpectedShutdownCrashes(
                    aiErrorDirectory,
                    StarCraftGameExitController.ExpectedShutdownCrashCleanupRetryWindow);
                aiProcessGoneAfterCleanup = !IsProcessRunning(report.Ai.StarCraftProcessId.Value);
                if (report.Player.StarCraftProcessId is not null &&
                    IsProcessRunning(report.Player.StarCraftProcessId.Value))
                {
                    _ = StarCraftBorderlessWindow.ActivateProcessWindowWhenReady(
                        report.Player.StarCraftProcessId.Value,
                        TimeSpan.FromSeconds(2));
                    playerStateAfterAiShutdown = StarCraftScreenDetector.Detect(report.Player.StarCraftProcessId.Value);
                    SaveSmokeWindowScreenshot(paths, report.Player.StarCraftProcessId, "smoke-start-player-after-ai-shutdown.png");
                }
            }

            var newAiRuntimeErrors = RuntimeErrorLogSnapshot.Capture(Path.Combine(aiRoot, "Errors"))
                .FindNewOrChanged(aiRuntimeErrorSnapshot);
            var aiRuntimeErrorsClean = newAiRuntimeErrors.Count == 0;
            var newPlayerRuntimeErrors = RuntimeErrorLogSnapshot.Capture(playerErrorDirectory)
                .FindNewOrChanged(playerRuntimeErrorSnapshot);
            var playerRuntimeErrorsClean = newPlayerRuntimeErrors.Count == 0;
            var playerGracefulShutdown = playerShutdown is null ||
                StarCraftGameExitController.IsGracefulAiShutdown(
                    playerShutdown,
                    playerProcessGoneAfterCleanup,
                    StarCraftScreenState.Unknown,
                    playerRuntimeErrorsClean);
            var aiShutdownExitedFinal = aiShutdown is { Exited: true } || aiProcessGoneAfterCleanup;
            var aiGracefulShutdown = StarCraftGameExitController.IsGracefulAiShutdown(
                aiShutdown,
                aiProcessGoneAfterCleanup,
                playerStateAfterAiShutdown,
                aiRuntimeErrorsClean);
            var observeStable = SmokeStartObservationPolicy.IsStableAfterObserve(
                request.ObserveSeconds,
                playerProcessAliveAfterObserve,
                aiProcessAliveAfterObserve,
                playerStateAfterObserve,
                aiStateAfterObserve,
                aiLogActivity.HasMeaningfulActivity);
            var aiActivityOk = !request.RequireAiActivity ||
                               (request.ObserveSeconds > 0 && aiRuntimeActivityDetected);

            var passed = report.Player.CompletedStartCount > 0 &&
                          report.Ai.CompletedStartCount > 0 &&
                          borderlessApplied &&
                          inGameDetected &&
                          aiInGameVerified &&
                           observeStable &&
                           aiWindowMinimized &&
                           timerOverlayVisible &&
                          aiActivityOk &&
                          playerGracefulShutdown &&
                          aiGracefulShutdown;
            Console.Error.WriteLine(
                $"smoke-start: bot={bot.Name}, map={map.Name}, playerStarts={report.Player.CompletedStartCount}, aiStarts={report.Ai.CompletedStartCount}, " +
                $"playerPid={report.Player.StarCraftProcessId?.ToString() ?? "null"}, aiPid={report.Ai.StarCraftProcessId?.ToString() ?? "null"}, " +
                $"playerPath={playerProcessPath}, aiPath={aiProcessPath}, " +
                $"borderless={borderlessApplied}, playerState={playerState}, aiState={aiState}, inGame={inGameDetected}, aiInGame={aiInGameDetected}, aiInGameVerified={aiInGameVerified}, aiMinimized={aiWindowMinimized}, " +
                $"timerOverlay={timerOverlayVisible}, playerHudMs={playerHudDetectedMs}, overlayStartMs={overlayStartedMs}, " +
                $"overlayShotMs={overlayScreenshotMs}, aiHudMs={aiHudDetectedMs}, observeSeconds={request.ObserveSeconds}, observeMs={observeMs}, observeElapsedMs={observeElapsedMs}, observeStoppedEarly={observeStoppedEarly}, " +
                $"observeStable={observeStable}, playerAliveAfterObserve={playerProcessAliveAfterObserve}, aiAliveAfterObserve={aiProcessAliveAfterObserve}, " +
                $"playerStateAfterObserve={playerStateAfterObserve}, aiStateAfterObserve={aiStateAfterObserve}, " +
                $"requireAiActivity={request.RequireAiActivity}, aiActivitySamples={aiActivity.SampleCount}, " +
                $"aiActivityChanged={aiActivity.ChangedSamples}, aiActivityRatio={aiActivity.ChangedRatio:F6}, " +
                $"aiActivityAverageDelta={aiActivity.AverageDelta:F3}, aiActivityDetected={aiActivity.HasMeaningfulActivity}, " +
                $"aiLogActivityDetected={aiLogActivity.HasMeaningfulActivity}, aiLogActivityLines={aiLogActivity.MeaningfulLineCount}, aiLogActivityFiles={aiLogActivity.FormatFiles()}, " +
                $"playerShutdownSent={playerShutdown?.LeaveSequenceSent.ToString() ?? "null"}, playerShutdownExited={playerProcessGoneAfterCleanup}, " +
                $"playerLeaveConfirmed={playerShutdown?.LeaveConfirmed.ToString() ?? "null"}, playerForcedKill={playerShutdown?.ForcedKillUsed.ToString() ?? "null"}, " +
                $"playerCleanupStopped={playerCleanupStopped}, playerRuntimeErrorsClean={playerRuntimeErrorsClean}, " +
                $"playerRuntimeErrorFiles={RuntimeErrorLogSnapshot.Format(newPlayerRuntimeErrors)}, playerGracefulShutdown={playerGracefulShutdown}, " +
                $"aiShutdownSent={aiShutdown?.LeaveSequenceSent.ToString() ?? "null"}, aiShutdownExited={aiShutdownExitedFinal}, " +
                $"aiLeaveConfirmed={aiShutdown?.LeaveConfirmed.ToString() ?? "null"}, aiForcedKill={aiShutdown?.ForcedKillUsed.ToString() ?? "null"}, " +
                $"aiCleanupStopped={aiCleanupStopped}, aiProcessGoneAfterCleanup={aiProcessGoneAfterCleanup}, " +
                $"playerAfterAiShutdownState={playerStateAfterAiShutdown}, aiRuntimeErrorsClean={aiRuntimeErrorsClean}, " +
                $"aiRuntimeErrorFiles={RuntimeErrorLogSnapshot.Format(newAiRuntimeErrors)}, aiGracefulShutdown={aiGracefulShutdown}, " +
                $"traceSamples={startupTraceSummary.SampleCount}, traceFirstCaptureMs={startupTraceSummary.FirstCaptureMs?.ToString() ?? "null"}, " +
                $"traceFirstInGameMs={startupTraceSummary.FirstInGameMs?.ToString() ?? "null"}, traceMaxRed={startupTraceSummary.MaxRedErrorPixels}, " +
                $"traceMaxRedFrame={startupTraceSummary.MaxRedErrorFrame ?? "null"}, traceDir={startupTrace?.TraceDirectory ?? "null"}");
            return passed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            aiActivityStartBitmap?.Dispose();
            aiActivityEndBitmap?.Dispose();
            startupTrace?.Dispose();
            if (ShouldRunRuntimeCleanupForStart(runtimeLaunchStarted))
            {
                cleaner?.StopKnown(launchedPlayerStarCraftProcessId, launchedAiStarCraftProcessId);
                cleaner?.Stop(paths.PlayerRuntimeRoot, paths.AiRuntimeRoot);
                if (preExistingStarCraftProcessIds is not null)
                {
                    StopNewStarCraftProcesses(preExistingStarCraftProcessIds);
                }

                StarCraftGameExitController.RemoveExpectedShutdownCrashes(
                    Path.Combine(paths.PlayerRuntimeRoot, "Errors"),
                    FinalRuntimeShutdownCrashCleanupRetryWindow,
                    FinalRuntimeShutdownCrashInitialQuietWindow);
                StarCraftGameExitController.RemoveExpectedShutdownCrashes(
                    Path.Combine(paths.AiRuntimeRoot, "Errors"),
                    FinalRuntimeShutdownCrashCleanupRetryWindow,
                    FinalRuntimeShutdownCrashInitialQuietWindow);
            }
        }
    }

    private static void PumpWinFormsFor(TimeSpan duration)
    {
        var deadline = DateTime.UtcNow + duration;
        while (DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(50);
        }
    }

    internal static bool WaitForPracticeOverlay(
        Func<bool> captureContainsOverlay,
        Action<TimeSpan> pump,
        TimeSpan timeout,
        TimeSpan interval)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (captureContainsOverlay())
            {
                return true;
            }

            if (stopwatch.Elapsed >= timeout)
            {
                return false;
            }

            pump(interval);
        }
    }

    private static string ProcessPath(int? processId)
    {
        if (processId is null)
        {
            return "null";
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId.Value);
            return process.MainModule?.FileName ?? "<unknown>";
        }
        catch
        {
            return "<unknown>";
        }
    }

    private static bool CaptureContainsPracticeOverlay(int processId)
    {
        if (!StarCraftScreenDetector.TryCaptureWindowBitmap(processId, out var bitmap) || bitmap is null)
        {
            return false;
        }

        using (bitmap)
        {
            return ContainsPracticeOverlay(bitmap);
        }
    }

    internal static bool ContainsPracticeOverlay(Bitmap bitmap)
    {
        var maxX = Math.Min(bitmap.Width, 360);
        var maxY = Math.Min(bitmap.Height, 140);
        var greenPixels = 0;
        var darkPixels = 0;
        for (var y = 0; y < maxY; y++)
        {
            for (var x = 0; x < maxX; x++)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.G > color.R + 20 && color.G > color.B + 20 && color.G > 80)
                {
                    greenPixels++;
                }

                if (color.R <= 45 && color.G <= 45 && color.B <= 45)
                {
                    darkPixels++;
                }
            }
        }

        return greenPixels >= 80 && darkPixels >= 1200;
    }

    private static Bitmap? SaveSmokeWindowScreenshot(
        PracticePaths paths,
        int? processId,
        string fileName,
        bool keepBitmap = false)
    {
        if (processId is null ||
            !StarCraftScreenDetector.TryCaptureWindowBitmap(processId.Value, out var bitmap) ||
            bitmap is null)
        {
            return null;
        }

        using (bitmap)
        {
            var screenshotDirectory = Path.Combine(paths.RepositoryRoot, "artifacts", "screenshots");
            Directory.CreateDirectory(screenshotDirectory);
            bitmap.Save(Path.Combine(screenshotDirectory, fileName), ImageFormat.Png);
            return keepBitmap ? new Bitmap(bitmap) : null;
        }
    }

    private static string ReadSmokeBotConfig(string aiRuntimeRoot)
    {
        var path = Path.Combine(aiRuntimeRoot, "bwapi-data", "AI", "sparring-bot.ini");
        return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
    }

    private static PracticeBot SelectBot(IReadOnlyList<PracticeBot> candidates, string? requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName) ||
            string.Equals(requestedName, "Random", StringComparison.OrdinalIgnoreCase))
        {
            return candidates
                .OrderByDescending(candidate => candidate.Elo ?? int.MinValue)
                .First();
        }

        return candidates.FirstOrDefault(candidate => string.Equals(candidate.Name, requestedName, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(candidate => candidate.Name.Contains(requestedName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Smoke bot was not found or has no supported maps: {requestedName}");
    }

    internal static PracticeCatalog LoadSmokeStartCatalog(
        PracticePaths paths,
        SparringSettings? settings = null,
        bool includeConfiguredMaps = true)
    {
        var baseCatalog = PracticeAssetCatalogReader.Read(paths);
        if (!includeConfiguredMaps)
        {
            return baseCatalog;
        }

        settings ??= SparringSettingsStore.Default().Load();
        var ladderMapRoot = string.IsNullOrWhiteSpace(settings.LadderMapRoot)
            ? RemasteredLadderMapCatalogReader.DefaultDirectory()
            : settings.LadderMapRoot;
        var ladderMaps = RemasteredLadderMapCatalogReader.ReadDirectory(ladderMapRoot, baseCatalog);
        var userMaps = UserMapCatalogReader.ReadDirectory(settings.UserMapRoot);
        return UserMapCatalogReader.Merge(
            UserMapCatalogReader.Merge(baseCatalog, ladderMaps),
            userMaps);
    }

    internal static bool ShouldInitializeWinFormsForStart(bool dryRun, bool prepareOnly)
    {
        return !dryRun && !prepareOnly;
    }

    internal static bool ShouldRunRuntimeCleanupForStart(bool runtimeLaunchStarted)
    {
        return runtimeLaunchStarted;
    }

    private static (PracticeBot Bot, PracticeMap Map) ResolveSmokeSparringSelection(
        PracticeCatalog catalog,
        SmokeStartRequest request,
        StarCraftRace? enemyRace)
    {
        var explicitBotRequested = !string.IsNullOrWhiteSpace(request.BotName) &&
            !string.Equals(request.BotName, "Random", StringComparison.OrdinalIgnoreCase);
        var candidateBots = catalog.Bots
            .Where(candidate => candidate.ExecutableKind == BotExecutableKind.Dll)
            .Where(candidate => enemyRace is null || candidate.Race == enemyRace.Value);
        if (!request.AllowIncompatible)
        {
            candidateBots = candidateBots
                .Where(candidate => PracticeCatalogCompatibility.MapsForBot(catalog, candidate.Id).Any());
        }

        if (!explicitBotRequested)
        {
            candidateBots = candidateBots.Where(PracticeBotCandidatePolicy.IsSparringRandomEligible);
        }

        var candidates = candidateBots.ToList();
        var bot = SelectBot(candidates, request.BotName);
        var mapCandidates = request.AllowIncompatible
            ? catalog.Maps.Where(map => map.Enabled).OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase).ToList()
            : PracticeCatalogCompatibility.MapsForBot(catalog, bot.Id);
        var map = SelectMap(mapCandidates, request.MapName);
        return (bot, map);
    }

    private static PracticeCatalog MarkMapAsUserMapForSmokeVerification(PracticeCatalog catalog, Guid mapId)
    {
        return catalog with
        {
            Maps = catalog.Maps
                .Select(map => map.Id == mapId ? map with { IsUserMap = true } : map)
                .ToList()
        };
    }

    private static (PracticeBot Bot, PracticeMap Map) ResolveSmokeLadderSelection(
        PracticeCatalog catalog,
        SmokeStartRequest request,
        StarCraftRace? enemyRace)
    {
        var candidateMaps = catalog.Maps
            .Where(map => map.Enabled)
            .Where(map => LadderBotSelector.CandidatesForMap(catalog, map.Id, enemyRace).Count > 0)
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var map = SelectMap(candidateMaps, request.MapName);
        var bot = SelectBot(LadderBotSelector.CandidatesForMap(catalog, map.Id, enemyRace), request.BotName);
        return (bot, map);
    }

    private static PracticeMap SelectMap(IReadOnlyList<PracticeMap> candidates, string? requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName) ||
            string.Equals(requestedName, "Random", StringComparison.OrdinalIgnoreCase))
        {
            return candidates.First();
        }

        return candidates.FirstOrDefault(candidate => string.Equals(candidate.Name, requestedName, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(candidate => string.Equals(candidate.FileName, requestedName, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(candidate =>
                candidate.Name.Contains(requestedName, StringComparison.OrdinalIgnoreCase) ||
                candidate.FileName.Contains(requestedName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Smoke map was not available for the selected bot: {requestedName}");
    }

    private sealed record SmokeStartRequest(
        string? BotName,
        string? MapName,
        string? Mode,
        string? PlayerRace,
        string? EnemyRace,
        string? BotBuildId,
        int ObserveSeconds,
        bool DryRun,
        bool PrepareOnly,
        bool BundledCatalogOnly,
        bool RequireAiActivity,
        bool AllowIncompatible)
    {
        public bool IsLadderMode =>
            string.Equals(Mode, "Ladder", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Mode, "래더", StringComparison.OrdinalIgnoreCase);

        public string ModeLabel => IsLadderMode ? "Ladder" : "Sparring";

        public static SmokeStartRequest Parse(IReadOnlyList<string> args)
        {
            return new SmokeStartRequest(
                ValueAfter(args, "--bot"),
                ValueAfter(args, "--map"),
                ValueAfter(args, "--mode"),
                ValueAfter(args, "--player-race"),
                ValueAfter(args, "--enemy-race"),
                ValueAfter(args, "--bot-build"),
                ParseObserveSeconds(ValueAfter(args, "--observe-seconds")),
                args.Any(arg => string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase)),
                args.Any(arg => string.Equals(arg, "--prepare-only", StringComparison.OrdinalIgnoreCase)),
                args.Any(arg => string.Equals(arg, "--bundled-catalog-only", StringComparison.OrdinalIgnoreCase)),
                args.Any(arg => string.Equals(arg, "--require-ai-activity", StringComparison.OrdinalIgnoreCase)),
                args.Any(arg => string.Equals(arg, "--allow-incompatible", StringComparison.OrdinalIgnoreCase)));
        }

        public StarCraftRace PlayerRaceOrDefault()
        {
            return ParseRace(PlayerRace) ?? StarCraftRace.Terran;
        }

        public StarCraftRace? EnemyRaceOrDefault()
        {
            return ParseRace(EnemyRace);
        }

        private static StarCraftRace? ParseRace(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, "Any", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "All", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return value.Trim() switch
            {
                var text when string.Equals(text, "Terran", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Terran,
                var text when string.Equals(text, "T", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Terran,
                var text when string.Equals(text, "테란", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Terran,
                var text when string.Equals(text, "Protoss", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Protoss,
                var text when string.Equals(text, "P", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Protoss,
                var text when string.Equals(text, "토스", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Protoss,
                var text when string.Equals(text, "프로토스", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Protoss,
                var text when string.Equals(text, "Zerg", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Zerg,
                var text when string.Equals(text, "Z", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Zerg,
                var text when string.Equals(text, "저그", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Zerg,
                var text when string.Equals(text, "Random", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Random,
                var text when string.Equals(text, "랜덤", StringComparison.OrdinalIgnoreCase) => StarCraftRace.Random,
                _ => throw new InvalidOperationException($"Unknown smoke race: {value}")
            };
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
                : throw new InvalidOperationException($"Invalid observe seconds: {value}");
        }
    }

    private static HashSet<int> CurrentStarCraftProcessIds()
    {
        return System.Diagnostics.Process.GetProcessesByName("StarCraft")
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .ToHashSet();
    }

    private static bool IsProcessRunning(int processId)
    {
        return ProcessEnumeration.IsProcessRunning(processId);
    }

    private static LauncherUiSmokeSummary CaptureLauncherScreenshots(PracticePaths paths)
    {
        ApplicationConfiguration.Initialize();
        var screenshotDirectory = Path.Combine(paths.RepositoryRoot, "artifacts", "screenshots");
        Directory.CreateDirectory(screenshotDirectory);

        using var form = new MainForm
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            ShowInTaskbar = false
        };
        try
        {
            CreateControlTree(form);
            _ = ShowWindow(form.Handle, ShowWindowNoActivate);
            var layoutIssues = new List<string>();
            var hasMapPreviewBox = false;
            var hasMapPreviewImage = false;
            var sizes = LauncherSmokeSizes();
            var tabs = FindControls<TabControl>(form).FirstOrDefault();
            var modeCombo = FindControls<ComboBox>(form)
                .FirstOrDefault(combo => combo.Items.Cast<object>().Any(item => item.ToString() == "래더"));

            foreach (var (name, size) in sizes)
            {
                form.Size = size;
                form.PerformLayout();
                Application.DoEvents();

                if (tabs is not { TabPages.Count: > 0 } tabControl)
                {
                    continue;
                }

                tabControl.SelectedIndex = 0;
                Application.DoEvents();

                if (modeCombo is not null)
                {
                    modeCombo.SelectedItem = "스파링";
                }

                SelectFirstConcreteItem(form, "MapList");
                Application.DoEvents();
                var mapPreviewBox = FindControls<PictureBox>(form)
                    .FirstOrDefault(control => string.Equals(control.Name, "MapPreviewBox", StringComparison.Ordinal));
                hasMapPreviewBox |= mapPreviewBox is not null;
                hasMapPreviewImage |= mapPreviewBox?.Image is not null;
                CaptureTabScreenshot(tabControl, 0, "game-sparring", name, screenshotDirectory, layoutIssues, form);

                if (modeCombo is not null)
                {
                    modeCombo.SelectedItem = "래더";
                    Application.DoEvents();
                    CaptureTabScreenshot(tabControl, 0, "game-ladder", name, screenshotDirectory, layoutIssues, form);
                }

                CaptureTabScreenshot(tabControl, 1, "settings", name, screenshotDirectory, layoutIssues, form);
                CaptureTabScreenshot(tabControl, 2, "hotkeys", name, screenshotDirectory, layoutIssues, form);
                CaptureTabScreenshot(tabControl, 3, "history", name, screenshotDirectory, layoutIssues, form);
            }

            if (tabs is { TabPages.Count: > 0 } finalTabs)
            {
                finalTabs.SelectedIndex = Math.Min(2, finalTabs.TabPages.Count - 1);
                Application.DoEvents();
                SaveFormScreenshot(form, Path.Combine(screenshotDirectory, "sparring-launcher-hotkeys-smoke.png"));
            }

            return new LauncherUiSmokeSummary(
                HasMapPreviewBox: hasMapPreviewBox,
                HasMapPreviewImage: hasMapPreviewImage,
                LayoutIssues: layoutIssues);
        }
        finally
        {
            _ = ShowWindow(form.Handle, ShowWindowHide);
            try
            {
                form.Dispose();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Collection was modified", StringComparison.OrdinalIgnoreCase))
            {
                // WinForms can throw this while tearing down ListBox UIA providers after screenshot automation.
            }
        }
    }

    private static void SaveFormScreenshot(Form form, string screenshotPath)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(screenshotPath, ImageFormat.Png);
    }

    private static IReadOnlyList<(string Name, Size Size)> LauncherSmokeSizes()
    {
        return
        [
            ("narrow", new Size(760, 560)),
            ("short", new Size(980, 540)),
            ("compact", new Size(980, 700)),
            ("default", new Size(1180, 820)),
            ("fhd", new Size(1600, 900)),
            ("qhd", new Size(2200, 1240)),
            ("uhd", new Size(3000, 1688))
        ];
    }

    private static void CaptureTabScreenshot(
        TabControl tabs,
        int tabIndex,
        string tabName,
        string sizeName,
        string screenshotDirectory,
        List<string> layoutIssues,
        Form form)
    {
        if (tabs.TabPages.Count <= tabIndex)
        {
            return;
        }

        tabs.SelectedIndex = tabIndex;
        Application.DoEvents();
        if (tabs.SelectedTab is not { } selectedTab)
        {
            return;
        }

        layoutIssues.AddRange(FindLayoutIssues(form).Select(issue => $"{tabName} {sizeName}: {issue}"));
        layoutIssues.AddRange(FindHorizontalPageScrollIssues(selectedTab)
            .Select(issue => $"{tabName} {sizeName}: {issue}"));
        if (tabName.StartsWith("game-", StringComparison.Ordinal))
        {
            layoutIssues.AddRange(FindGameTabScaleIssues(selectedTab)
                .Select(issue => $"{tabName} {sizeName}: {issue}"));
        }
        else if (tabName == "hotkeys")
        {
            layoutIssues.AddRange(FindHotkeyTabReadabilityIssues(selectedTab)
                .Select(issue => $"{tabName} {sizeName}: {issue}"));
        }
        else if (tabName == "history")
        {
            layoutIssues.AddRange(FindHistoryTabReadabilityIssues(selectedTab)
                .Select(issue => $"{tabName} {sizeName}: {issue}"));
        }

        layoutIssues.AddRange(FindImportantOverlapIssues(selectedTab)
            .Select(issue => $"{tabName} {sizeName}: {issue}"));

        SaveFormScreenshot(form, Path.Combine(screenshotDirectory, $"sparring-launcher-{tabName}-{sizeName}-smoke.png"));
    }

    private static IEnumerable<string> FindHorizontalPageScrollIssues(TabPage page)
    {
        if (!page.AutoScroll)
        {
            yield break;
        }

        var horizontalScrollBudget = Math.Max(12, SystemInformation.VerticalScrollBarWidth);
        if (page.AutoScrollMinSize.Width > page.ClientSize.Width + horizontalScrollBudget)
        {
            yield return $"page creates a horizontal scroll area. clientWidth={page.ClientSize.Width}, scrollWidth={page.AutoScrollMinSize.Width}.";
        }

        foreach (Control control in page.Controls)
        {
            if (!control.Visible || control.Left < 0)
            {
                continue;
            }

            if (control.Right > page.ClientSize.Width + horizontalScrollBudget)
            {
                yield return $"{DescribeControl(control)} extends past the visible page width. right={control.Right}, clientWidth={page.ClientSize.Width}.";
            }
        }
    }

    private static IEnumerable<string> FindGameTabScaleIssues(TabPage page)
    {
        var scaledWidth = LauncherLayoutScale.ToBaseDpi(page.ClientSize.Width, page.DeviceDpi);
        var scaledHeight = LauncherLayoutScale.ToBaseDpi(page.ClientSize.Height, page.DeviceDpi);

        if (scaledWidth < 700)
        {
            yield break;
        }

        var mapPreview = FindControls<PictureBox>(page)
            .FirstOrDefault(control => string.Equals(control.Name, "MapPreviewBox", StringComparison.Ordinal));
        var minimumPreviewSize = scaledWidth >= 1400
            ? scaledHeight >= 760 ? 360 : 320
            : 300;
        if (mapPreview is not null && mapPreview.Width < minimumPreviewSize)
        {
            yield return $"{DescribeControl(mapPreview)} is too small for a large game tab. size={mapPreview.Size}, page={page.ClientSize}.";
        }

        var runtimeBox = FindControls<TextBox>(page)
            .FirstOrDefault(textBox => textBox.Multiline && textBox.Text.StartsWith("사람:", StringComparison.Ordinal));
        if (runtimeBox is not null && runtimeBox.Height < 70)
        {
            yield return $"{DescribeControl(runtimeBox)} is too short for runtime paths on a large game tab. size={runtimeBox.Size}, page={page.ClientSize}.";
        }

        if (scaledWidth >= 1400)
        {
            var minimumListWidth = scaledWidth >= 1900 ? 520 : 480;
            foreach (var listName in new[] { "BotList", "MapList" })
            {
                var list = FindControls<ListBox>(page)
                    .FirstOrDefault(control => string.Equals(control.Name, listName, StringComparison.Ordinal));
                if (list is not null && list.Width < minimumListWidth)
                {
                    yield return $"{DescribeControl(list)} is too narrow for readable selection on a large game tab. width={list.Width}, minimum={minimumListWidth}, page={page.ClientSize}.";
                }
            }
        }
    }

    private static IEnumerable<string> FindHotkeyTabReadabilityIssues(TabPage page)
    {
        var scaledWidth = LauncherLayoutScale.ToBaseDpi(page.ClientSize.Width, page.DeviceDpi);
        if (scaledWidth < 700)
        {
            yield break;
        }

        var objectList = FindControls<ListBox>(page)
            .FirstOrDefault(control => string.Equals(control.Name, "HotkeyObjectList", StringComparison.Ordinal));
        objectList ??= FindControls<ListBox>(page)
            .OrderBy(control => control.Left)
            .FirstOrDefault();
        if (objectList is not null && objectList.Width < 320)
        {
            yield return $"{DescribeControl(objectList)} is too narrow for readable hotkey objects. width={objectList.Width}, page={page.ClientSize}.";
        }
    }

    private static IEnumerable<string> FindHistoryTabReadabilityIssues(TabPage page)
    {
        var grid = FindControls<DataGridView>(page).FirstOrDefault();
        if (grid is null ||
            LauncherLayoutScale.ToBaseDpi(grid.Width, page.DeviceDpi) >= 1220)
        {
            yield break;
        }

        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (column.Visible &&
                column.DataPropertyName is nameof(PracticeSessionRecord.ActionCount)
                    or nameof(PracticeSessionRecord.MatchupText)
                    or nameof(PracticeSessionRecord.RatingText)
                    or nameof(PracticeSessionRecord.ResultSourceText))
            {
                yield return $"{DescribeControl(grid)} keeps low-priority column '{column.HeaderText}' visible in a medium/narrow history tab.";
            }
        }
    }

    private static IEnumerable<string> FindImportantOverlapIssues(Control root)
    {
        var controls = root.Controls
            .Cast<Control>()
            .Where(control => control.Visible)
            .Where(control => control.Left >= 0 && control.Top >= 0)
            .Where(IsOverlapRelevantControl)
            .Where(control => control.Width > 0 && control.Height > 0)
            .ToArray();

        for (var leftIndex = 0; leftIndex < controls.Length; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < controls.Length; rightIndex++)
            {
                var left = controls[leftIndex];
                var right = controls[rightIndex];
                var intersection = Rectangle.Intersect(left.Bounds, right.Bounds);
                if (intersection.Width <= 2 || intersection.Height <= 2)
                {
                    continue;
                }

                if (IsToleratedEdgeOverlap(left, right, intersection))
                {
                    continue;
                }

                yield return $"{DescribeControl(left)} overlaps {DescribeControl(right)} at {intersection}.";
            }
        }
    }

    private static bool IsToleratedEdgeOverlap(Control left, Control right, Rectangle intersection)
    {
        if (left is TrackBar && right is TrackBar && intersection.Height <= 4)
        {
            return true;
        }

        if (left is not Label && right is not Label)
        {
            return false;
        }

        return intersection.Height <= 4 || intersection.Width <= 8;
    }

    private static bool IsOverlapRelevantControl(Control control)
    {
        return control switch
        {
            Label label => !string.IsNullOrWhiteSpace(label.Text),
            Button => true,
            ComboBox => true,
            TextBox => true,
            CheckBox => true,
            NumericUpDown => true,
            TrackBar => true,
            ListBox => true,
            PictureBox => true,
            DataGridView => true,
            TableLayoutPanel => true,
            _ => false
        };
    }

    private static void CreateControlTree(Control control)
    {
        control.CreateControl();
        foreach (Control child in control.Controls)
        {
            CreateControlTree(child);
        }
    }

    private static IEnumerable<string> FindLayoutIssues(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (!child.Visible)
            {
                continue;
            }

            if (child.Parent is { } parent &&
                parent is not ScrollableControl { AutoScroll: true })
            {
                var parentBounds = parent.ClientRectangle;
                if (child.Right > parentBounds.Right + 4 || child.Bottom > parentBounds.Bottom + 4)
                {
                    yield return $"{DescribeControl(child)} exceeds parent bounds {parentBounds}.";
                }
            }

            if (child is Button button && !string.IsNullOrWhiteSpace(button.Text))
            {
                var preferred = TextRenderer.MeasureText(button.Text, button.Font);
                if (preferred.Width + 12 > button.ClientSize.Width ||
                    preferred.Height > button.ClientSize.Height + 2)
                {
                    yield return $"{DescribeControl(button)} text does not fit. text='{button.Text}', preferred={preferred}, size={button.ClientSize}.";
                }
            }

            if (child is Label label && ShouldValidateLabelTextFit(label))
            {
                var proposedSize = new Size(Math.Max(1, label.ClientSize.Width), int.MaxValue);
                var preferred = TextRenderer.MeasureText(
                    label.Text,
                    label.Font,
                    proposedSize,
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                if (preferred.Height > label.ClientSize.Height + 4)
                {
                    yield return $"{DescribeControl(label)} text is clipped. text='{label.Text}', preferred={preferred}, size={label.ClientSize}.";
                }
            }

            if (child is ComboBox combo)
            {
                if (combo.DrawMode != DrawMode.OwnerDrawFixed)
                {
                    yield return $"{DescribeControl(combo)} uses the default white dropdown rendering.";
                }

                if (combo.BackColor.GetBrightness() > 0.18f)
                {
                    yield return $"{DescribeControl(combo)} dropdown background is too bright. color={combo.BackColor}.";
                }
            }

            foreach (var issue in FindLayoutIssues(child))
            {
                yield return issue;
            }
        }
    }

    private static bool ShouldValidateLabelTextFit(Label label)
    {
        return !label.AutoSize &&
               label.Name is "DifficultyLabel" &&
               !string.IsNullOrWhiteSpace(label.Text);
    }

    private static string DescribeControl(Control control)
    {
        var text = string.IsNullOrWhiteSpace(control.Text) ? control.Name : control.Text;
        return $"{control.GetType().Name}({text})";
    }

    private static void SelectFirstConcreteItem(Control root, string controlName)
    {
        var list = FindControls<ListBox>(root)
            .FirstOrDefault(control => string.Equals(control.Name, controlName, StringComparison.Ordinal));
        if (list is null)
        {
            return;
        }

        for (var index = 0; index < list.Items.Count; index++)
        {
            if (!string.Equals(list.Items[index]?.ToString(), "Random", StringComparison.OrdinalIgnoreCase))
            {
                list.SelectedIndex = index;
                return;
            }
        }
    }

    private static IEnumerable<T> FindControls<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }

    private sealed record LauncherUiSmokeSummary(
        bool HasMapPreviewBox,
        bool HasMapPreviewImage,
        IReadOnlyList<string> LayoutIssues);

    internal sealed record RuntimeErrorLogSnapshot(IReadOnlyDictionary<string, RuntimeErrorLogEntry> Entries)
    {
        public static RuntimeErrorLogSnapshot Capture(string errorDirectory)
        {
            if (!Directory.Exists(errorDirectory))
            {
                return new RuntimeErrorLogSnapshot(new Dictionary<string, RuntimeErrorLogEntry>(StringComparer.OrdinalIgnoreCase));
            }

            var entries = Directory.EnumerateFiles(errorDirectory)
                .Where(path => Path.GetExtension(path) is ".txt" or ".ERR" or ".err")
                .Where(path => !IsIgnorableRuntimeErrorFile(path))
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new KeyValuePair<string, RuntimeErrorLogEntry>(
                        info.FullName,
                        new RuntimeErrorLogEntry(info.Length, info.LastWriteTimeUtc));
                })
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            return new RuntimeErrorLogSnapshot(entries);
        }

        internal static bool IsIgnorableRuntimeErrorFile(string path)
        {
            if (!Path.GetFileName(path).Equals("bwapi-error.txt", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch
            {
                return false;
            }

            var meaningfulLines = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
            return meaningfulLines.Length > 0 &&
                   meaningfulLines.All(IsExpectedMissingHumanAiModuleLine);
        }

        private static bool IsExpectedMissingHumanAiModuleLine(string line)
        {
            return line.Contains("Could not find ai under ai", StringComparison.OrdinalIgnoreCase) &&
                   line.Contains("bwapi-data", StringComparison.OrdinalIgnoreCase) &&
                   line.Contains("bwapi.ini", StringComparison.OrdinalIgnoreCase);
        }

        public IReadOnlyList<string> FindNewOrChanged(RuntimeErrorLogSnapshot baseline)
        {
            return Entries
                .Where(pair =>
                    !baseline.Entries.TryGetValue(pair.Key, out var previous) ||
                    previous.Length != pair.Value.Length ||
                    previous.LastWriteTimeUtc != pair.Value.LastWriteTimeUtc)
                .Select(pair => Path.GetFileName(pair.Key))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static string Format(IReadOnlyList<string> files)
        {
            return files.Count == 0 ? "none" : string.Join("|", files);
        }
    }

    internal sealed record RuntimeErrorLogEntry(long Length, DateTime LastWriteTimeUtc);

    private const int ShowWindowHide = 0;
    private const int ShowWindowNoActivate = 4;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static void StopNewStarCraftProcesses(IReadOnlySet<int> preExistingProcessIds)
    {
        foreach (var process in System.Diagnostics.Process.GetProcessesByName("StarCraft"))
        {
            using (process)
            {
                if (!ProcessEnumeration.IsRunning(process))
                {
                    continue;
                }

                if (preExistingProcessIds.Contains(process.Id))
                {
                    continue;
                }

                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
                catch
                {
                    // Smoke cleanup is best-effort; launch validation has already completed.
                }
            }
        }
    }
}
