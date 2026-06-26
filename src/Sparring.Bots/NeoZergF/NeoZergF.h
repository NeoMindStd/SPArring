#pragma once

#include <BWAPI.h>

#include <set>
#include <string>
#include <vector>

class NeoZergF : public BWAPI::AIModule
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
        OverpoolSpeed,
        TwoHatchMuta,
        FiveThirtyMuta,
        ThreeHatchHydra,
        FiveHatchHydra,
        NinePoolSpeed,
        LurkerContain
    };

private:
    void setInitialMapPositions();
    void chooseOpening();
    void updateEnemyStart(BWAPI::Unit unit);
    void manageWorkers();
    bool handleEmergencyDefense();
    void releaseDefenseDrones();
    void executeOpening();
    void executeOverpoolSpeed();
    void executeTwoHatchMuta();
    void executeFiveThirtyMuta();
    void executeThreeHatchHydra();
    void executeFiveHatchHydra();
    void executeNinePoolSpeed();
    void executeLurkerContain();
    void manageProduction();
    void manageResearch();
    void manageCombat();
    void maybeSendGg();

    int supplyUsed() const;
    int supplyTotal() const;
    int allCount(BWAPI::UnitType type) const;
    int completedCount(BWAPI::UnitType type) const;
    int armyCount() const;
    int workerTarget() const;
    bool hasOrMorphing(BWAPI::UnitType type, int count = 1) const;
    bool hasCompleted(BWAPI::UnitType type, int count = 1) const;
    bool canAfford(BWAPI::UnitType type) const;
    bool canAfford(BWAPI::UpgradeType type) const;
    bool canAfford(BWAPI::TechType type) const;
    bool ensureOverlord();
    bool ensureBuilding(BWAPI::UnitType type, int targetCount, BWAPI::TilePosition near, int maxRange = 48);
    bool ensureExtractor(int targetCount);
    bool ensureExpansion(int targetCount);
    bool ensureLair();
    bool trainLarva(BWAPI::UnitType type, int maxCount);
    bool researchFromIdle(BWAPI::UnitType producer, BWAPI::TechType techType);
    bool upgradeFromIdle(BWAPI::UnitType producer, BWAPI::UpgradeType upgradeType);
    bool morphHydraliskToLurker(int maxCount);

    BWAPI::Unit pickWorker(BWAPI::Position near) const;
    BWAPI::Unit nearestMineral(BWAPI::Position near) const;
    BWAPI::Unit nearestExtractorNeedingWorkers(BWAPI::Position near) const;
    BWAPI::Unit nearestEnemyThreat(BWAPI::Position near, int radius) const;
    BWAPI::Unit nearestEnemyTarget() const;
    int combatUnitCountNear(BWAPI::Position near, int radius) const;
    int enemyThreatCountNear(BWAPI::Position near, int radius, bool workersOnly) const;
    int gasWorkersFor(BWAPI::Unit extractor) const;
    BWAPI::TilePosition findExpansionTile() const;
    BWAPI::Position attackTarget() const;
    double distance(BWAPI::Position a, BWAPI::Position b) const;
    std::string configuredBuildId() const;
    std::string openingName() const;

    Opening opening_ = Opening::TwoHatchMuta;
    BWAPI::TilePosition mainTile_ = BWAPI::TilePositions::Invalid;
    BWAPI::TilePosition naturalTile_ = BWAPI::TilePositions::Invalid;
    BWAPI::TilePosition enemyStartTile_ = BWAPI::TilePositions::Invalid;
    int lastBuildFrame_ = -1000;
    int lastAttackFrame_ = -1000;
    int lastScoutFrame_ = -1000;
    int ggFrame_ = -1;
    bool ggSent_ = false;
    std::set<int> defenseDroneIds_;
};
