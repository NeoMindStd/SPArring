using Sparring.Core;

namespace Sparring.Tests;

public sealed class PracticeCompatibilityAuditorTests
{
    [Fact]
    public void AuditReportsCrashEvidenceWhenPairIsStillCompatible()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-audit-tests", Guid.NewGuid().ToString("N"));
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
        var root = Path.Combine(Path.GetTempPath(), "sparring-audit-tests", Guid.NewGuid().ToString("N"));
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
        var root = Path.Combine(Path.GetTempPath(), "sparring-audit-tests", Guid.NewGuid().ToString("N"));
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
        var root = Path.Combine(Path.GetTempPath(), "sparring-audit-tests", Guid.NewGuid().ToString("N"));
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
    public void AuditPrefersFaultPathBotDirectoryOverCompanionModuleOnlyCrash()
    {
        var root = Path.Combine(Path.GetTempPath(), "sparring-audit-tests", Guid.NewGuid().ToString("N"));
        var feintRoot = Path.Combine(root, "bots", "Feint");
        var steamhammerRoot = Path.Combine(root, "bots", "Steamhammer");
        var mapPath = Path.Combine(root, "maps", "(2)Benzene.scx");
        var errorRoot = Path.Combine(root, "errors");
        Directory.CreateDirectory(feintRoot);
        Directory.CreateDirectory(steamhammerRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(mapPath)!);
        Directory.CreateDirectory(errorRoot);
        File.WriteAllText(Path.Combine(feintRoot, "Steamhammer.dll"), "bot");
        File.WriteAllText(Path.Combine(steamhammerRoot, "Steamhammer.dll"), "bot");
        File.WriteAllText(mapPath, "map");
        File.WriteAllText(Path.Combine(errorRoot, "2026 Jul 01.txt"), """
            TIME: Wed Jul  1 17:12:19 2026
            MAP: Benzene 1.1
                 (2)Benzene.scx
            EXCEPTION: 0xC0000005    EXCEPTION_ACCESS_VIOLATION
            FAULT:     0x739B3722    Steamhammer.dll
            """);
        File.WriteAllText(Path.Combine(errorRoot, "dwj19160101.ERR"), """
            Fault address:	739B3722 01:00052722 C:\sparring\SC116AI_ai\bwapi-data\AI\Sparring\Bots\Feint\Steamhammer.dll
            """);
        var mapId = Guid.NewGuid();
        var catalog = new PracticeCatalog(
            [
                Bot("Feint", "Steamhammer.dll", mapId, feintRoot),
                Bot("Steamhammer", "Steamhammer.dll", mapId, steamhammerRoot)
            ],
            [new PracticeMap(mapId, "(2)Benzene", "(2)Benzene.scx", null, true, mapPath)]);

        var report = PracticeCompatibilityAuditor.Audit(catalog, errorRoot);

        Assert.DoesNotContain(
            report.Issues,
            issue => issue.Kind == PracticeCompatibilityAuditIssueKind.RuntimeCrashEvidence &&
                issue.BotName == "Steamhammer");
    }

    [Fact]
    public void ParserCapturesBotDirectoryFromFaultPath()
    {
        var crashes = PracticeCompatibilityAuditor.ParseRuntimeCrashEvidence(
            """
            Fault address:	79F13722 01:00052722 C:\sparring\SC116AI_ai\bwapi-data\AI\Sparring\Bots\Feint\Steamhammer.dll
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
