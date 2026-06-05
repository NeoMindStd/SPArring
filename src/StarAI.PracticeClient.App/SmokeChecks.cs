using StarAI.PracticeClient.Core;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace StarAI.PracticeClient.App;

internal static class SmokeChecks
{
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

        var catalog = SchnailCatalogReader.Read(paths.SchnailRoot);
        if (catalog.Bots.Count == 0 || catalog.Maps.Count == 0)
        {
            return 1;
        }

        if (!catalog.Bots.Any(bot => PracticeCatalogCompatibility.MapsForBot(catalog, bot.Id).Any()))
        {
            return 1;
        }

        CaptureLauncherScreenshots(paths);
        return 0;
    }

    public static int RunStart()
    {
        if (!OperatingSystem.IsWindows())
        {
            return 1;
        }

        var paths = PracticePaths.Defaults();
        var cleaner = new LocalRuntimeProcessCleaner();
        var preExistingStarCraftProcessIds = CurrentStarCraftProcessIds();

        try
        {
            var catalog = SchnailCatalogReader.Read(paths.SchnailRoot);
            var bot = catalog.Bots
                .Where(candidate => candidate.ExecutableKind == BotExecutableKind.Dll)
                .OrderByDescending(candidate => candidate.Elo ?? int.MinValue)
                .First(candidate => PracticeCatalogCompatibility.MapsForBot(catalog, candidate.Id).Any());
            var map = PracticeCatalogCompatibility.MapsForBot(catalog, bot.Id).First();
            var aiRoot = RuntimeProvisioner.EnsureAiRoot(paths.PlayerRuntimeRoot);
            var selection = new PracticeSelection(
                bot.Id,
                map.Id,
                StarCraftRace.Terran,
                "StarAI Smoke",
                PlayerBorderless: true,
                ClipCursor: false,
                AllowApmAlert: false);
            var plan = PracticeLaunchPlanBuilder.Build(catalog, paths with { AiRuntimeRoot = aiRoot }, selection);
            var screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);
            var cncDdrawHandlesPlayerDisplay = plan.Player.CncDdrawMode == CncDdrawMode.BorderlessFullscreen;
            var borderlessApplied = false;
            var report = new PracticeSessionLauncher().Launch(
                plan,
                PracticeRuntimeOptions.Defaults(),
                PracticeSessionLaunchOptions.Defaults() with
                {
                    StarCraftStartupTimeout = TimeSpan.FromSeconds(45),
                    AiLaunchDelay = TimeSpan.FromSeconds(3),
                    StopExistingLocalRuntime = true
                });

            if (report.Player.StarCraftProcessId is not null)
            {
                borderlessApplied = cncDdrawHandlesPlayerDisplay ||
                    StarCraftBorderlessWindow.ApplyToProcessWhenReady(
                        report.Player.StarCraftProcessId.Value,
                        screenBounds,
                        TimeSpan.FromSeconds(8)).Applied;
            }

            return report.Player.CompletedStartCount > 0 &&
                   report.Ai.CompletedStartCount > 0 &&
                   borderlessApplied
                ? 0
                : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            cleaner.Stop(paths.PlayerRuntimeRoot, paths.AiRuntimeRoot);
            StopNewStarCraftProcesses(preExistingStarCraftProcessIds);
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

    private static void CaptureLauncherScreenshots(PracticePaths paths)
    {
        ApplicationConfiguration.Initialize();
        var screenshotDirectory = Path.Combine(paths.RepositoryRoot, "artifacts", "screenshots");
        Directory.CreateDirectory(screenshotDirectory);

        var form = new MainForm
        {
            Size = new Size(1180, 820),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            ShowInTaskbar = false
        };
        try
        {
            CreateControlTree(form);
            form.PerformLayout();
            _ = ShowWindow(form.Handle, ShowWindowNoActivate);
            Application.DoEvents();
            SaveFormScreenshot(form, Path.Combine(screenshotDirectory, "starai-launcher-smoke.png"));

            var modeCombo = FindControls<ComboBox>(form)
                .FirstOrDefault(combo => combo.Items.Cast<object>().Any(item => item.ToString() == "래더"));
            if (modeCombo is not null)
            {
                modeCombo.SelectedItem = "래더";
                Application.DoEvents();
                SaveFormScreenshot(form, Path.Combine(screenshotDirectory, "starai-launcher-ladder-smoke.png"));
            }

            var tabs = form.Controls.OfType<TabControl>().FirstOrDefault();
            if (tabs is not null && tabs.TabPages.Count > 2)
            {
                tabs.SelectedIndex = 2;
                Application.DoEvents();
                SaveFormScreenshot(form, Path.Combine(screenshotDirectory, "starai-launcher-hotkeys-smoke.png"));
            }
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

    private static void CreateControlTree(Control control)
    {
        control.CreateControl();
        foreach (Control child in control.Controls)
        {
            CreateControlTree(child);
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
