#pragma once

#include <BWAPI.h>

#include <map>
#include <set>
#include <string>
#include <vector>

class NeoProtossF : public BWAPI::AIModule
{
public:
    void onStart() override;
    void onEnd(bool isWinner) override;
    void onFrame() override;
    void onSendText(std::string text) override;
    void onReceiveText(BWAPI::Player player, std::string text) override;
    void onPlayerLeft(BWAPI::Player player) override;
    void onNukeDetect(BWAPI::Position target) override;
    void onUnitDiscover(BWAPI::Unit unit) override;
    void onUnitEvade(BWAPI::Unit unit) override;
    void onUnitShow(BWAPI::Unit unit) override;
    void onUnitHide(BWAPI::Unit unit) override;
    void onUnitCreate(BWAPI::Unit unit) override;
    void onUnitDestroy(BWAPI::Unit unit) override;
    void onUnitMorph(BWAPI::Unit unit) override;
    void onUnitRenegade(BWAPI::Unit unit) override;
    void onSaveGame(std::string gameName) override;
    void onUnitComplete(BWAPI::Unit unit) override;

public:
    enum class Opening
    {
        TwoGate1012,
        FastPowerDragoon,
        Nexus23,
        Arbiter29,
        ForgeDouble,
        GateDoubleCorsairDark,
        NakedDouble,
        RealFastDark,
        ForwardGateDark
    };

private:
    struct PolicyAction;

    void chooseOpening();
    void updateEnemyStart(BWAPI::Unit unit);
    void setInitialMapPositions();
    void manageWorkers(const PolicyAction& action);
    void manageScout();
    bool handleEmergencyDefense();
    void releaseDefenseProbes();
    void maybeSendGg();
    void executeOpening();
    void executeTwoGate1012();
    void executeFastPowerDragoon();
    void executeNexus23();
    void executeArbiter29();
    void executeForgeDouble();
    void executeGateDoubleCorsairDark();
    void executeNakedDouble();
    void executeRealFastDark();
    void executeForwardGateDark();
    void manageResearch();
    void drawDebug() const;

    int supplyUsed() const;
    int supplyTotal() const;
    int allCount(BWAPI::UnitType type) const;
    int completedCount(BWAPI::UnitType type) const;
    int armyCount() const;
    bool hasOrBuilding(BWAPI::UnitType type, int count = 1) const;
    bool hasCompleted(BWAPI::UnitType type, int count = 1) const;
    bool canAfford(BWAPI::UnitType type) const;
    bool canAfford(BWAPI::UpgradeType type) const;
    bool canAfford(BWAPI::TechType type) const;
    bool ensurePylon();
    bool ensureBuilding(BWAPI::UnitType type, int targetCount, BWAPI::TilePosition near, int maxRange = 48);
    bool ensureAssimilator(int targetCount);
    bool ensureExpansion(int targetCount);
    bool trainFromIdle(BWAPI::UnitType producer, BWAPI::UnitType unitType, int maxCount);
    bool trainGatewayUnit(BWAPI::UnitType unitType, int maxCount);
    bool upgradeFromIdle(BWAPI::UnitType producer, BWAPI::UpgradeType upgradeType);
    bool researchFromIdle(BWAPI::UnitType producer, BWAPI::TechType techType);

    BWAPI::Unit pickWorker(BWAPI::Position near) const;
    BWAPI::Unit nearestOwned(BWAPI::UnitType type, BWAPI::Position near) const;
    BWAPI::Unit nearestEnemyThreat(BWAPI::Position near, int radius) const;
    int combatUnitCountNear(BWAPI::Position near, int radius) const;
    int enemyThreatCountNear(BWAPI::Position near, int radius, bool workersOnly) const;
    BWAPI::Unit nearestEnemyTarget() const;
    BWAPI::Unit nearestMineral(BWAPI::Position near) const;
    BWAPI::Unit nearestRefineryNeedingWorkers(BWAPI::Position near, const PolicyAction& action) const;
    BWAPI::TilePosition findExpansionTile() const;
    BWAPI::TilePosition findForwardTile() const;
    BWAPI::Position attackTarget() const;
    int gasWorkerTarget(BWAPI::Unit refinery, const PolicyAction& action) const;
    int gasWorkersFor(BWAPI::Unit refinery) const;
    double distance(BWAPI::Position a, BWAPI::Position b) const;
    std::string configuredBuildId() const;
    std::string openingName() const;

    enum class PolicyIntent
    {
        Macro,
        Defend,
        Produce,
        Expand,
        Attack
    };

    struct PolicyState
    {
        int Frame = 0;
        int Supply = 0;
        int Workers = 0;
        int Army = 0;
        int Gateways = 0;
        int Nexuses = 0;
        int Minerals = 0;
        int Gas = 0;
        int EnemyThreatsNearMain = 0;
        bool OpeningComplete = false;
    };

    struct PolicyAction
    {
        PolicyIntent Intent = PolicyIntent::Macro;
        int WorkerTarget = 0;
        int GasWorkersPerAssimilator = 3;
        int NexusTarget = 1;
        int GatewayTarget = 1;
        int ZealotCap = 0;
        int DragoonCap = 0;
        int DarkTemplarCap = 0;
        int ObserverCap = 0;
        int StargateUnitCap = 0;
        BWAPI::UnitType StargateUnit;
        bool TakeExpansion = false;
        bool Attack = false;
        double AttackPressure = 0.0;
        int WorkerDefenders = 0;
    };

    struct PolicyModel
    {
        std::map<std::string, std::vector<double>> Outputs;
    };

    struct PendingBuild
    {
        BWAPI::UnitType Type;
        int ObservedCount = 0;
        int Frame = 0;
    };

    void loadPolicyModel();
    PolicyState capturePolicyState() const;
    PolicyAction evaluatePolicy(const PolicyState& state) const;
    void executePolicyAction(const PolicyAction& action);
    std::vector<double> policyFeatures(const PolicyState& state) const;
    double scorePolicyOutput(const std::string& output, const std::vector<double>& features, const std::vector<double>& fallback) const;
    int queuedProbeCount() const;
    int pendingBuildCount(BWAPI::UnitType type) const;
    void rememberPendingBuild(BWAPI::UnitType type, int observedCount);

    Opening opening_ = Opening::FastPowerDragoon;
    BWAPI::TilePosition mainTile_ = BWAPI::TilePositions::Invalid;
    BWAPI::TilePosition naturalTile_ = BWAPI::TilePositions::Invalid;
    BWAPI::TilePosition enemyStartTile_ = BWAPI::TilePositions::Invalid;
    BWAPI::Unit scout_ = nullptr;
    int lastBuildFrame_ = -1000;
    int lastAttackFrame_ = -1000;
    int lastScoutFrame_ = -1000;
    int lastDebugFrame_ = -1000;
    int lastDefenseFrame_ = -1000;
    int ggFrame_ = -1;
    bool ggSent_ = false;
    bool policyLoaded_ = false;
    int nextPendingBuildId_ = 1;
    PolicyModel policyModel_;
    std::map<int, PendingBuild> pendingBuilds_;
    std::set<int> defenseProbeIds_;
    std::set<int> visitedStartIndices_;
};
