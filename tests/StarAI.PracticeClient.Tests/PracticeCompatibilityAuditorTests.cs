using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class PracticeCompatibilityAuditorTests
{
    [Fact]
    public void AuditReportsCrashEvidenceWhenPairIsStillCompatible()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-audit-tests", Guid.NewGuid().ToString("N"));
        var botRoot = Path.Combine(root, "bots", "CrashBot");
        var mapPath = Path.Combine(root, "maps", "(4)Jade.scx");
        var errorRoot = Path.Combine(root, "errors");
        Directory.CreateDirectory(botRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(mapPath)!);
        Directory.CreateDirectory(errorRoot);
        File.WriteAllText(Path.Combine(botRoot, "CrashBot.dll"), "bot");
        File.WriteAllText(mapPath, "map");
        File.WriteAllText(Path.Combine(errorRoot, "crash.txt"), """
            TIME: Mon Jun  8 11:40:59 2026
            MAP: Jade 1.0
                 (4)Jade.scx
            EXCEPTION: 0xC0000005    EXCEPTION_ACCESS_VIOLATION
            FAULT:     0x7B2BEC83    CrashBot.dll
            """);
        var mapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [Bot("CrashBot", "CrashBot.dll", mapId, botRoot)],
            [new PracticeMap(mapId, "(4)Jade", "(4)Jade.scx", null, true, mapPath)]);

        var report = PracticeCompatibilityAuditor.Audit(catalog, errorRoot);

        var issue = Assert.Single(report.Issues, issue => issue.Kind == PracticeCompatibilityAuditIssueKind.RuntimeCrashEvidence);
        Assert.Equal("CrashBot", issue.BotName);
        Assert.Equal("(4)Jade", issue.MapName);
    }

    [Fact]
    public void AuditDoesNotReportCrashEvidenceForKnownBlockedPair()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-audit-tests", Guid.NewGuid().ToString("N"));
        var botRoot = Path.Combine(root, "bots", "Stone");
        var mapPath = Path.Combine(root, "maps", "(4)Jade.scx");
        var errorRoot = Path.Combine(root, "errors");
        Directory.CreateDirectory(botRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(mapPath)!);
        Directory.CreateDirectory(errorRoot);
        File.WriteAllText(Path.Combine(botRoot, "Stone.dll"), "bot");
        File.WriteAllText(mapPath, "map");
        File.WriteAllText(Path.Combine(errorRoot, "crash.txt"), """
            TIME: Mon Jun  8 11:40:59 2026
            MAP: Jade 1.0
                 (4)Jade.scx
            EXCEPTION: 0xC0000005    EXCEPTION_ACCESS_VIOLATION
            FAULT:     0x7B2BEC83    Stone.dll
            """);
        var mapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [Bot("Stone", "Stone.dll", mapId, botRoot)],
            [new PracticeMap(mapId, "(4)Jade", "(4)Jade.scx", null, true, mapPath)]);

        var report = PracticeCompatibilityAuditor.Audit(catalog, errorRoot);

        Assert.DoesNotContain(report.Issues, issue => issue.Kind == PracticeCompatibilityAuditIssueKind.RuntimeCrashEvidence);
        Assert.Equal(1, report.BlockedDeclaredDllPairCount);
        Assert.Equal(0, report.CompatibleDllPairCount);
    }

    [Fact]
    public void AuditReportsMissingBotExecutable()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-audit-tests", Guid.NewGuid().ToString("N"));
        var botRoot = Path.Combine(root, "bots", "MissingBot");
        var mapPath = Path.Combine(root, "maps", "(2)Test.scx");
        Directory.CreateDirectory(botRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(mapPath)!);
        File.WriteAllText(mapPath, "map");
        var mapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [Bot("MissingBot", "MissingBot.dll", mapId, botRoot)],
            [new PracticeMap(mapId, "(2)Test", "(2)Test.scx", null, true, mapPath)]);

        var report = PracticeCompatibilityAuditor.Audit(catalog, null);

        var issue = Assert.Single(report.Issues, issue => issue.Kind == PracticeCompatibilityAuditIssueKind.MissingBotExecutable);
        Assert.Equal("MissingBot", issue.BotName);
    }

    [Fact]
    public void AuditReportsSharedDllCrashForEveryStillCompatibleCandidate()
    {
        var root = Path.Combine(Path.GetTempPath(), "starai-audit-tests", Guid.NewGuid().ToString("N"));
        var firstBotRoot = Path.Combine(root, "bots", "FirstSharedBot");
        var secondBotRoot = Path.Combine(root, "bots", "SecondSharedBot");
        var mapPath = Path.Combine(root, "maps", "(4)Fighting Spirit.scx");
        var errorRoot = Path.Combine(root, "errors");
        Directory.CreateDirectory(firstBotRoot);
        Directory.CreateDirectory(secondBotRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(mapPath)!);
        Directory.CreateDirectory(errorRoot);
        File.WriteAllText(Path.Combine(firstBotRoot, "SharedEngine.dll"), "bot");
        File.WriteAllText(Path.Combine(secondBotRoot, "SharedEngine.dll"), "bot");
        File.WriteAllText(mapPath, "map");
        File.WriteAllText(Path.Combine(errorRoot, "crash.txt"), """
            TIME: Mon Jun  8 11:40:59 2026
            MAP: Fighting Spirit
                 (4)Fighting Spirit.scx
            EXCEPTION: 0xC0000005    EXCEPTION_ACCESS_VIOLATION
            FAULT:     0x79F13722    SharedEngine.dll
            """);
        var mapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot("FirstSharedBot", "SharedEngine.dll", mapId, firstBotRoot),
                Bot("SecondSharedBot", "SharedEngine.dll", mapId, secondBotRoot)
            ],
            [new PracticeMap(mapId, "(4)Fighting Spirit", "(4)Fighting Spirit.scx", null, true, mapPath)]);

        var report = PracticeCompatibilityAuditor.Audit(catalog, errorRoot);

        var issues = report.Issues
            .Where(issue => issue.Kind == PracticeCompatibilityAuditIssueKind.RuntimeCrashEvidence)
            .OrderBy(issue => issue.BotName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Assert.Equal(2, issues.Count);
        Assert.Equal("FirstSharedBot", issues[0].BotName);
        Assert.Equal("SecondSharedBot", issues[1].BotName);
        Assert.All(issues, issue => Assert.Equal("(4)Fighting Spirit", issue.MapName));
        Assert.Single(report.RuntimeCrashes);
    }

    [Fact]
    public void ParserCapturesBotDirectoryFromFaultPath()
    {
        var crashes = PracticeCompatibilityAuditor.ParseRuntimeCrashEvidence(
            """
            Fault address: 79F13722 01:00052722 C:\starai\SC116AI_ai\bwapi-data\AI\StarAI\Bots\Feint\Steamhammer.dll
            """,
            "NeoMind160101.ERR");

        var crash = Assert.Single(crashes);
        Assert.Equal("Steamhammer.dll", crash.ModuleName);
        Assert.Equal("Feint", crash.BotDirectoryName);
    }

    private static PracticeBot Bot(string name, string executable, Guid mapId, string sourceDirectory)
    {
        return new PracticeBot(
            Guid.NewGuid(),
            name,
            StarCraftRace.Terran,
            executable,
            BotExecutableKind.Dll,
            "4.4.0",
            1000,
            false,
            new HashSet<Guid> { mapId },
            null,
            null,
            sourceDirectory);
    }
}
