namespace Sparring.Tests;

public sealed class NeoBotSourceTests
{
    [Theory]
    [InlineData("NeoProtossF", "Protoss_Probe", "Protoss_Assimilator", "refinery", "probe")]
    [InlineData("NeoProtossE", "Protoss_Probe", "Protoss_Assimilator", "refinery", "probe")]
    [InlineData("NeoTerranF", "Terran_SCV", "Terran_Refinery", "refinery", "scv")]
    [InlineData("NeoTerranE", "Terran_SCV", "Terran_Refinery", "refinery", "scv")]
    public void TerranAndProtossGasWorkerLimitCountsAssignedAndGatheringWorkers(
        string botName,
        string workerType,
        string gasBuildingType,
        string parameterName,
        string workerVariable)
    {
        var source = ReadBotSource(botName);

        Assert.Contains($"unit->getType() == UnitTypes::{workerType}", source);
        Assert.Contains($"target->getType() == UnitTypes::{gasBuildingType}", source);
        Assert.Contains($"int {botName}::gasWorkerTarget(Unit {parameterName}) const", source);
        Assert.Contains($"unit->getTarget() == {parameterName}", source);
        Assert.Contains($"unit->getOrderTarget() == {parameterName}", source);
        Assert.Contains($"gasTarget = {workerVariable}->getOrderTarget();", source);
        Assert.Contains("gasWorkersFor(gasTarget) > gasWorkerTarget(gasTarget)", source);
        Assert.Contains("return 3;", source);
    }

    [Fact]
    public void ZergGasWorkerLimitUsesFlexibleTargetAndKeepsOperatorPrecedenceExplicit()
    {
        AssertZergGasWorkerLimit("NeoZergF");
        AssertZergGasWorkerLimit("NeoZergE");
    }

    [Fact]
    public void NeoEBotsUseSeparatedConfigPathsAndSlightlyStrongerTimingConstants()
    {
        var protoss = ReadBotSource("NeoProtossE");
        var terran = ReadBotSource("NeoTerranE");
        var zerg = ReadBotSource("NeoZergE");

        Assert.Contains(@"bwapi-data\\AI\\Sparring\\Bots\\NeoProtossE\\sparring-bot.ini", protoss);
        Assert.Contains(@"bwapi-data\\AI\\Sparring\\Bots\\NeoTerranE\\sparring-bot.ini", terran);
        Assert.Contains(@"bwapi-data\\AI\\Sparring\\Bots\\NeoZergE\\sparring-bot.ini", zerg);
        Assert.DoesNotContain(@"bwapi-data\\AI\\Sparring\\Bots\\NeoProtossF\\sparring-bot.ini", protoss);
        Assert.DoesNotContain(@"bwapi-data\\AI\\Sparring\\Bots\\NeoTerranF\\sparring-bot.ini", terran);
        Assert.DoesNotContain(@"bwapi-data\\AI\\Sparring\\Bots\\NeoZergF\\sparring-bot.ini", zerg);

        Assert.Contains("shouldAttack = zealots >= 2 || frame > 24 * 240;", protoss);
        Assert.Contains("return std::min(70, 24 + nexuses * 17);", protoss);
        Assert.Contains("shouldAttack = marines >= 8 || frame > 24 * 320;", terran);
        Assert.Contains("return std::min(70, 24 + commandCenters * 17);", terran);
        Assert.Contains("shouldAttack = zerglings >= 8 || armyCount() >= 14 || frame > 24 * 290;", zerg);
        Assert.Contains("return std::min(target, 64);", zerg);
    }

    private static void AssertZergGasWorkerLimit(string botName)
    {
        var source = ReadBotSource(botName);

        Assert.Contains($"int {botName}::gasWorkerTarget(Unit extractor) const", source);
        Assert.Contains("gasTarget = unit->getOrderTarget();", source);
        Assert.Contains("gasWorkersFor(gasTarget) > gasWorkerTarget(gasTarget)", source);
        Assert.Contains("unit->getTarget() == extractor", source);
        Assert.Contains("unit->getOrderTarget() == extractor", source);
        Assert.Contains("return 3;", source);
    }

    [Fact]
    public void NeoProtossDefaultRandomOpeningAvoidsGimmickOrGreedyBuilds()
    {
        var source = ReadBotSource("NeoProtossF");
        var chooseOpening = ExtractFunction(source, "void NeoProtossF::chooseOpening()", "void NeoProtossF::updateEnemyStart");

        Assert.Contains("candidates.push_back(Opening::TwoGate1012);", chooseOpening);
        Assert.Contains("candidates.push_back(Opening::FastPowerDragoon);", chooseOpening);
        Assert.DoesNotContain("candidates.push_back(Opening::Nexus23);", chooseOpening);
        Assert.DoesNotContain("candidates.push_back(Opening::NakedDouble);", chooseOpening);
        Assert.DoesNotContain("candidates.push_back(Opening::Arbiter29);", chooseOpening);
        Assert.DoesNotContain("candidates.push_back(Opening::RealFastDark);", chooseOpening);
        Assert.DoesNotContain("candidates.push_back(Opening::ForwardGateDark);", chooseOpening);
    }

    [Fact]
    public void NeoProtossBuildsSupplyBeforeHardPylonBlock()
    {
        var source = ReadBotSource("NeoProtossF");
        var ensurePylon = ExtractFunction(source, "bool NeoProtossF::ensurePylon()", "bool NeoProtossF::ensureBuilding");

        Assert.Contains("supplyTotal() - supplyUsed() <= 6", ensurePylon);
        Assert.Contains("incompleteUnitCount(UnitTypes::Protoss_Pylon) == 0", ensurePylon);
    }

    private static string ReadBotSource(string botName)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "src", "Sparring.Bots", botName, $"{botName}.cpp");
        return File.ReadAllText(path);
    }

    private static string ExtractFunction(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find start marker: {startMarker}");

        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end > start, $"Could not find end marker after {startMarker}: {endMarker}");

        return source[start..end];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "Sparring.Bots")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from test output.");
    }
}
