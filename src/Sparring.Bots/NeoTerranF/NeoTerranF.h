#pragma once

#include <BWAPI.h>

#include <set>
#include <string>
#include <vector>

class NeoTerranF : public BWAPI::AIModule
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
        BioAcademy,
        RaxExpand,
        FactoryExpand,
        TwoFactoryPressure,
        OneFactStar
    };

private:
    void setInitialMapPositions();
    void chooseOpening();
    void updateEnemyStart(BWAPI::Unit unit);
    void manageWorkers();
    bool handleEmergencyDefense();
    void releaseDefenseScvs();
    void executeOpening();
    void executeBioAcademy();
    void executeRaxExpand();
    void executeFactoryExpand();
    void executeTwoFactoryPressure();
    void executeOneFactStar();
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
    bool hasOrBuilding(BWAPI::UnitType type, int count = 1) const;
    bool hasCompleted(BWAPI::UnitType type, int count = 1) const;
    bool canAfford(BWAPI::UnitType type) const;
    bool canAfford(BWAPI::UpgradeType type) const;
    bool canAfford(BWAPI::TechType type) const;
    bool ensureSupplyDepot();
    bool ensureBuilding(BWAPI::UnitType type, int targetCount, BWAPI::TilePosition near, int maxRange = 48);
    bool ensureRefinery(int targetCount);
    bool ensureExpansion(int targetCount);
    bool ensureAddon(BWAPI::UnitType producer, BWAPI::UnitType addon, int maxAddons);
    bool trainFromIdle(BWAPI::UnitType producer, BWAPI::UnitType unitType, int maxCount);
    bool researchFromIdle(BWAPI::UnitType producer, BWAPI::TechType techType);
    bool upgradeFromIdle(BWAPI::UnitType producer, BWAPI::UpgradeType upgradeType);

    BWAPI::Unit pickWorker(BWAPI::Position near) const;
    BWAPI::Unit nearestMineral(BWAPI::Position near) const;
    BWAPI::Unit nearestRefineryNeedingWorkers(BWAPI::Position near) const;
    BWAPI::Unit nearestEnemyThreat(BWAPI::Position near, int radius) const;
    BWAPI::Unit nearestEnemyTarget() const;
    int combatUnitCountNear(BWAPI::Position near, int radius) const;
    int enemyThreatCountNear(BWAPI::Position near, int radius, bool workersOnly) const;
    int gasWorkersFor(BWAPI::Unit refinery) const;
    BWAPI::TilePosition findExpansionTile() const;
    BWAPI::Position attackTarget() const;
    double distance(BWAPI::Position a, BWAPI::Position b) const;
    std::string configuredBuildId() const;
    std::string openingName() const;

    Opening opening_ = Opening::FactoryExpand;
    BWAPI::TilePosition mainTile_ = BWAPI::TilePositions::Invalid;
    BWAPI::TilePosition naturalTile_ = BWAPI::TilePositions::Invalid;
    BWAPI::TilePosition enemyStartTile_ = BWAPI::TilePositions::Invalid;
    int lastBuildFrame_ = -1000;
    int lastAttackFrame_ = -1000;
    int lastDefenseFrame_ = -1000;
    int ggFrame_ = -1;
    bool ggSent_ = false;
    std::set<int> defenseScvIds_;
};
