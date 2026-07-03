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
        Assert.Contains($"unit->getTarget() == {parameterName}", source);
        Assert.Contains($"unit->getOrderTarget() == {parameterName}", source);
        Assert.Contains($"gasTarget = {workerVariable}->getOrderTarget();", source);
        if (botName.Contains("Protoss", StringComparison.Ordinal))
        {
            Assert.Contains($"int {botName}::gasWorkerTarget(Unit {parameterName}, const PolicyAction& action) const", source);
            Assert.Contains("gasWorkersFor(gasTarget) > gasWorkerTarget(gasTarget, action)", source);
            Assert.Contains("return std::max(0, std::min(3, action.GasWorkersPerAssimilator));", source);
        }
        else
        {
            Assert.Contains($"int {botName}::gasWorkerTarget(Unit {parameterName}) const", source);
            Assert.Contains("gasWorkersFor(gasTarget) > gasWorkerTarget(gasTarget)", source);
            Assert.Contains("return 3;", source);
        }
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

        Assert.Contains("action.WorkerTarget = countOutput(\"worker_target\", { 24.0", protoss);
        Assert.Contains("action.AttackPressure = scorePolicyOutput(\"attack_pressure\", features, { -0.6", protoss);
        Assert.Contains("action.Attack = state.OpeningComplete && action.AttackPressure > 0.65;", protoss);
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

    [Theory]
    [InlineData("NeoProtossF")]
    [InlineData("NeoProtossE")]
    public void NeoProtossBuildWorkerSelectionKeepsSpecialDutyProbesOnTask(string botName)
    {
        var source = ReadBotSource(botName);
        var pickWorker = ExtractFunction(
            source,
            $"Unit {botName}::pickWorker(Position near) const",
            $"Unit {botName}::nearestOwned");

        Assert.Contains("defenseProbeIds_.find(unit->getID()) != defenseProbeIds_.end()", pickWorker);
        Assert.Contains("scout_ && scout_->exists() && scout_->getID() == unit->getID()", pickWorker);
        Assert.Contains("unit->isGatheringGas()", pickWorker);
        Assert.Contains("unit->isCarryingMinerals()", pickWorker);
        Assert.Contains("unit->isCarryingGas()", pickWorker);
    }

    [Theory]
    [InlineData("NeoProtossF")]
    [InlineData("NeoProtossE")]
    public void NeoProtossEmergencyDefenseIgnoresPassiveWorkerScouts(string botName)
    {
        var source = ReadBotSource(botName);
        var emergencyDefense = ExtractFunction(
            source,
            $"bool {botName}::handleEmergencyDefense()",
            $"void {botName}::releaseDefenseProbes()");

        Assert.Contains("const bool closeWorkerHarass = workerHarass &&", emergencyDefense);
        Assert.Contains("distance(threat->getPosition(), mainPosition) <= 240", emergencyDefense);
        Assert.Contains("const bool realWorkerHarass =", emergencyDefense);
        Assert.Contains("if (workerHarass && !realWorkerHarass)", emergencyDefense);
    }

    [Theory]
    [InlineData("NeoProtossF")]
    [InlineData("NeoProtossE")]
    public void NeoProtossReservesPendingBuildBeforeSendingAnotherProbe(string botName)
    {
        var source = ReadBotSource(botName);
        var header = ReadBotHeader(botName);
        var ensureBuilding = ExtractFunction(
            source,
            $"bool {botName}::ensureBuilding(UnitType type, int targetCount, TilePosition near, int maxRange)",
            $"bool {botName}::ensureAssimilator");
        var ensureAssimilator = ExtractFunction(
            source,
            $"bool {botName}::ensureAssimilator(int targetCount)",
            $"bool {botName}::ensureExpansion");

        Assert.Contains("#include <map>", header);
        Assert.Contains("std::map<int, PendingBuild> pendingBuilds_;", header);
        Assert.Contains("pendingBuildCount(type)", ensureBuilding);
        Assert.Contains("rememberPendingBuild(type, observedCount);", ensureBuilding);
        Assert.Contains("pendingBuildCount(UnitTypes::Protoss_Assimilator)", ensureAssimilator);
        Assert.Contains("rememberPendingBuild(UnitTypes::Protoss_Assimilator, observedCount);", ensureAssimilator);
    }

    [Theory]
    [InlineData("NeoProtossF")]
    [InlineData("NeoProtossE")]
    public void NeoProtossQueuesProbeProductionInsteadOfWaitingForIdleNexus(string botName)
    {
        var source = ReadBotSource(botName);
        var header = ReadBotHeader(botName);
        var manageWorkers = ExtractFunction(
            source,
            $"void {botName}::manageWorkers(const PolicyAction& action)",
            $"void {botName}::manageScout()");

        Assert.Contains("int queuedProbeCount() const;", header);
        Assert.Contains("const int queuedProbes = queuedProbeCount();", manageWorkers);
        Assert.Contains("unit->getTrainingQueue().size() < 2", manageWorkers);
        Assert.DoesNotContain("unit->isIdle() &&\r\n            allCount(UnitTypes::Protoss_Probe)", manageWorkers);
    }

    [Theory]
    [InlineData("NeoProtossF")]
    [InlineData("NeoProtossE")]
    public void NeoProtossUsesPolicyModelForPostOpeningJudgment(string botName)
    {
        var source = ReadBotSource(botName);
        var header = ReadBotHeader(botName);
        var onFrame = ExtractFunction(
            source,
            $"void {botName}::onFrame()",
            $"void {botName}::onSendText");
        var manageWorkers = ExtractFunction(
            source,
            $"void {botName}::manageWorkers(const PolicyAction& action)",
            $"void {botName}::manageScout()");
        var evaluatePolicy = ExtractFunction(
            source,
            $"{botName}::PolicyAction {botName}::evaluatePolicy(const PolicyState& state) const",
            $"void {botName}::executePolicyAction");
        var executePolicyAction = ExtractFunction(
            source,
            $"void {botName}::executePolicyAction(const PolicyAction& action)",
            $"int {botName}::queuedProbeCount() const");

        Assert.Contains("struct PolicyState", header);
        Assert.Contains("struct PolicyAction", header);
        Assert.Contains("int WorkerTarget", header);
        Assert.Contains("int GasWorkersPerAssimilator", header);
        Assert.Contains("int NexusTarget", header);
        Assert.Contains("int GatewayTarget", header);
        Assert.Contains("int ZealotCap", header);
        Assert.Contains("int DragoonCap", header);
        Assert.Contains("double AttackPressure", header);
        Assert.Contains("void loadPolicyModel();", header);
        Assert.Contains("PolicyState capturePolicyState() const;", header);
        Assert.Contains("PolicyAction evaluatePolicy(const PolicyState& state) const;", header);
        Assert.Contains("void executePolicyAction(const PolicyAction& action);", header);
        Assert.Contains("double scorePolicyOutput(const std::string& output", header);
        Assert.Contains("loadPolicyModel();", source);
        Assert.Contains("neo-policy.tsv", source);
        Assert.Contains("const PolicyState policyState = capturePolicyState();", onFrame);
        Assert.Contains("const PolicyAction policyAction = evaluatePolicy(policyState);", onFrame);
        Assert.Contains("manageWorkers(policyAction);", onFrame);
        Assert.Contains("executePolicyAction(policyAction);", onFrame);
        Assert.DoesNotContain("manageProduction();", onFrame);
        Assert.DoesNotContain("manageCombat();", onFrame);
        Assert.DoesNotContain($"void {botName}::manageProduction()", source);
        Assert.DoesNotContain($"void {botName}::manageCombat()", source);
        Assert.Contains("plannedProbes < action.WorkerTarget", manageWorkers);
        Assert.Contains("gasWorkerTarget(gasTarget, action)", manageWorkers);
        Assert.Contains("countOutput(\"worker_target\"", evaluatePolicy);
        Assert.Contains("countOutput(\"gas_workers\"", evaluatePolicy);
        Assert.Contains("countOutput(\"nexus_target\"", evaluatePolicy);
        Assert.Contains("countOutput(\"gateway_target\"", evaluatePolicy);
        Assert.Contains("countOutput(\"zealot_cap\"", evaluatePolicy);
        Assert.Contains("countOutput(\"dragoon_cap\"", evaluatePolicy);
        Assert.Contains("scorePolicyOutput(\"attack_pressure\"", evaluatePolicy);
        Assert.Contains("trainGatewayUnit(UnitTypes::Protoss_Zealot, action.ZealotCap)", executePolicyAction);
        Assert.Contains("trainGatewayUnit(UnitTypes::Protoss_Dragoon, action.DragoonCap)", executePolicyAction);
        Assert.Contains("ensureBuilding(UnitTypes::Protoss_Gateway, action.GatewayTarget, mainTile_)", executePolicyAction);
        Assert.DoesNotContain("state.Army >= 5", evaluatePolicy);
        Assert.DoesNotContain("state.Supply >= 70", evaluatePolicy);
    }

    private static string ReadBotSource(string botName)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "src", "Sparring.Bots", botName, $"{botName}.cpp");
        return File.ReadAllText(path);
    }

    private static string ReadBotHeader(string botName)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "src", "Sparring.Bots", botName, $"{botName}.h");
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
