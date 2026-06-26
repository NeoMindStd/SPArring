#include "NeoTerranF.h"

#include <algorithm>
#include <cctype>
#include <cmath>
#include <cstdlib>
#include <ctime>
#include <fstream>
#include <limits>
#include <sstream>

using namespace BWAPI;

namespace
{
    bool isValidTile(TilePosition tile)
    {
        return tile != TilePositions::Invalid && tile != TilePositions::None && tile.x >= 0 && tile.y >= 0;
    }

    bool isCombatUnit(UnitType type)
    {
        return !type.isWorker() &&
            !type.isBuilding() &&
            type != UnitTypes::Terran_Vulture_Spider_Mine &&
            type.canAttack();
    }

    bool isThreatType(UnitType type)
    {
        return type.canAttack() ||
            type == UnitTypes::Protoss_Zealot ||
            type == UnitTypes::Zerg_Zergling ||
            type == UnitTypes::Terran_Marine ||
            type.isWorker();
    }

    std::string trimLower(std::string value)
    {
        const auto first = value.find_first_not_of(" \t\r\n");
        if (first == std::string::npos)
        {
            return "";
        }

        const auto last = value.find_last_not_of(" \t\r\n");
        value = value.substr(first, last - first + 1);
        std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch)
        {
            return static_cast<char>(std::tolower(ch));
        });
        return value;
    }

    bool buildIdToOpening(const std::string& buildId, NeoTerranF::Opening& opening)
    {
        const std::string id = trimLower(buildId);
        if (id == "bio_academy")
        {
            opening = NeoTerranF::Opening::BioAcademy;
            return true;
        }
        if (id == "rax_expand")
        {
            opening = NeoTerranF::Opening::RaxExpand;
            return true;
        }
        if (id == "factory_expand")
        {
            opening = NeoTerranF::Opening::FactoryExpand;
            return true;
        }
        if (id == "two_factory_pressure")
        {
            opening = NeoTerranF::Opening::TwoFactoryPressure;
            return true;
        }
        if (id == "one_fact_star")
        {
            opening = NeoTerranF::Opening::OneFactStar;
            return true;
        }

        return false;
    }
}

void NeoTerranF::onStart()
{
    Broodwar->enableFlag(Flag::UserInput);
    Broodwar->setCommandOptimizationLevel(2);
    setInitialMapPositions();
    chooseOpening();
}

void NeoTerranF::onEnd(bool isWinner)
{
    if (!isWinner && !ggSent_)
    {
        Broodwar->sendText("gg");
        ggSent_ = true;
        ggFrame_ = Broodwar->getFrameCount();
    }
}

void NeoTerranF::onFrame()
{
    if (Broodwar->isReplay() || Broodwar->isPaused() || !Broodwar->self())
    {
        return;
    }

    if (Broodwar->self()->getRace() != Races::Terran)
    {
        return;
    }

    const int frame = Broodwar->getFrameCount();
    if (frame % std::max(1, Broodwar->getLatencyFrames()) != 0)
    {
        return;
    }

    manageWorkers();
    const bool emergency = handleEmergencyDefense();
    executeOpening();
    manageProduction();
    manageResearch();
    maybeSendGg();
    if (ggSent_)
    {
        if (ggFrame_ >= 0 && frame - ggFrame_ > 48)
        {
            Broodwar->leaveGame();
        }
        return;
    }

    if (!emergency)
    {
        manageCombat();
    }
}

void NeoTerranF::onSendText(std::string text)
{
    Broodwar->sendText("%s", text.c_str());
}

void NeoTerranF::onReceiveText(Player, std::string)
{
}

void NeoTerranF::onPlayerLeft(Player)
{
}

void NeoTerranF::onNukeDetect(Position)
{
}

void NeoTerranF::onUnitDiscover(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoTerranF::onUnitEvade(Unit)
{
}

void NeoTerranF::onUnitShow(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoTerranF::onUnitHide(Unit)
{
}

void NeoTerranF::onUnitCreate(Unit)
{
}

void NeoTerranF::onUnitDestroy(Unit)
{
}

void NeoTerranF::onUnitMorph(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoTerranF::onUnitRenegade(Unit)
{
}

void NeoTerranF::onSaveGame(std::string)
{
}

void NeoTerranF::onUnitComplete(Unit)
{
}

void NeoTerranF::setInitialMapPositions()
{
    mainTile_ = Broodwar->self()->getStartLocation();
    naturalTile_ = TilePositions::Invalid;
}

void NeoTerranF::chooseOpening()
{
    Opening configured = Opening::FactoryExpand;
    if (buildIdToOpening(configuredBuildId(), configured))
    {
        opening_ = configured;
        return;
    }

    std::vector<Opening> candidates;
    Race enemyRace = Broodwar->enemy() ? Broodwar->enemy()->getRace() : Races::Unknown;
    if (enemyRace == Races::Zerg)
    {
        candidates.push_back(Opening::BioAcademy);
        candidates.push_back(Opening::RaxExpand);
    }
    else if (enemyRace == Races::Protoss)
    {
        candidates.push_back(Opening::FactoryExpand);
        candidates.push_back(Opening::TwoFactoryPressure);
    }
    else
    {
        candidates.push_back(Opening::FactoryExpand);
        candidates.push_back(Opening::TwoFactoryPressure);
        candidates.push_back(Opening::OneFactStar);
    }

    candidates.push_back(Opening::RaxExpand);
    const int seed = static_cast<int>(std::time(nullptr)) ^
        Broodwar->getFrameCount() ^
        (mainTile_.x * 73856093) ^
        (mainTile_.y * 19349663);
    std::srand(seed);
    opening_ = candidates[std::rand() % candidates.size()];
}

void NeoTerranF::updateEnemyStart(Unit unit)
{
    if (!unit || !unit->exists() || !unit->getPlayer() || !Broodwar->self()->isEnemy(unit->getPlayer()))
    {
        return;
    }

    if (unit->getType().isBuilding())
    {
        enemyStartTile_ = unit->getTilePosition();
    }
}

void NeoTerranF::manageWorkers()
{
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == UnitTypes::Terran_Command_Center &&
            unit->isCompleted() &&
            unit->isIdle() &&
            allCount(UnitTypes::Terran_SCV) < workerTarget() &&
            canAfford(UnitTypes::Terran_SCV))
        {
            unit->train(UnitTypes::Terran_SCV);
        }
    }

    for (auto scv : Broodwar->self()->getUnits())
    {
        if (!scv || !scv->exists() || scv->getType() != UnitTypes::Terran_SCV || !scv->isCompleted())
        {
            continue;
        }

        if (scv->isConstructing() || scv->isRepairing() || scv->isLoaded())
        {
            continue;
        }

        if (defenseScvIds_.find(scv->getID()) != defenseScvIds_.end() &&
            Broodwar->getFrameCount() - lastDefenseFrame_ < 96)
        {
            continue;
        }

        Unit gas = nearestRefineryNeedingWorkers(scv->getPosition());
        if (gas && !scv->isCarryingMinerals() && !scv->isCarryingGas())
        {
            scv->gather(gas);
            continue;
        }

        if (scv->isIdle())
        {
            if (scv->isCarryingMinerals() || scv->isCarryingGas())
            {
                scv->returnCargo();
                continue;
            }

            Unit mineral = nearestMineral(scv->getPosition());
            if (mineral)
            {
                scv->gather(mineral);
            }
        }
    }
}

bool NeoTerranF::handleEmergencyDefense()
{
    const Position mainPosition = Position(mainTile_);
    Unit threat = nearestEnemyThreat(mainPosition, 860);
    if (!threat && isValidTile(naturalTile_))
    {
        threat = nearestEnemyThreat(Position(naturalTile_), 650);
    }

    if (!threat)
    {
        releaseDefenseScvs();
        return false;
    }

    const bool workerHarass = threat->getType().isWorker();
    const int combatNearMain = combatUnitCountNear(mainPosition, 760);
    const int workerThreatCount = enemyThreatCountNear(mainPosition, 560, true);
    if (workerHarass && combatNearMain > 0)
    {
        releaseDefenseScvs();
    }

    if (workerHarass &&
        !threat->isAttacking() &&
        distance(threat->getPosition(), mainPosition) > 320)
    {
        releaseDefenseScvs();
        return false;
    }

    if (hasCompleted(UnitTypes::Terran_Barracks))
    {
        trainFromIdle(UnitTypes::Terran_Barracks, UnitTypes::Terran_Marine, 18);
    }

    if (hasCompleted(UnitTypes::Terran_Engineering_Bay))
    {
        ensureBuilding(UnitTypes::Terran_Missile_Turret, 2, isValidTile(naturalTile_) ? naturalTile_ : mainTile_, 20);
    }

    int desiredScvs = 0;
    if (workerHarass)
    {
        desiredScvs = combatNearMain > 0 ? 0 : std::min(2, std::max(1, workerThreatCount));
    }
    else if (supplyUsed() < 30 && combatNearMain < 2)
    {
        desiredScvs = 3;
    }
    else if (combatNearMain == 0)
    {
        desiredScvs = 1;
    }

    std::vector<Unit> currentDefenseScvs;
    for (auto scv : Broodwar->self()->getUnits())
    {
        if (scv &&
            scv->exists() &&
            scv->getType() == UnitTypes::Terran_SCV &&
            scv->isCompleted() &&
            !scv->isConstructing() &&
            defenseScvIds_.find(scv->getID()) != defenseScvIds_.end())
        {
            if (distance(scv->getPosition(), mainPosition) > 760)
            {
                defenseScvIds_.erase(scv->getID());
                Unit mineral = nearestMineral(scv->getPosition());
                if (mineral)
                {
                    scv->gather(mineral);
                }
                continue;
            }

            currentDefenseScvs.push_back(scv);
        }
    }

    for (auto scv : Broodwar->self()->getUnits())
    {
        if (static_cast<int>(currentDefenseScvs.size()) >= desiredScvs)
        {
            break;
        }

        if (!scv ||
            !scv->exists() ||
            scv->getType() != UnitTypes::Terran_SCV ||
            !scv->isCompleted() ||
            scv->isConstructing() ||
            defenseScvIds_.find(scv->getID()) != defenseScvIds_.end())
        {
            continue;
        }

        currentDefenseScvs.push_back(scv);
        defenseScvIds_.insert(scv->getID());
    }

    while (static_cast<int>(currentDefenseScvs.size()) > desiredScvs)
    {
        Unit scv = currentDefenseScvs.back();
        currentDefenseScvs.pop_back();
        defenseScvIds_.erase(scv->getID());
        Unit mineral = nearestMineral(scv->getPosition());
        if (mineral)
        {
            scv->gather(mineral);
        }
    }

    if (Broodwar->getFrameCount() - lastDefenseFrame_ >= 24)
    {
        lastDefenseFrame_ = Broodwar->getFrameCount();
        for (auto scv : currentDefenseScvs)
        {
            if (scv && scv->exists())
            {
                scv->attack(threat);
            }
        }
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit && unit->exists() && unit->isCompleted() && isCombatUnit(unit->getType()))
        {
            unit->attack(threat);
        }
    }

    return true;
}

void NeoTerranF::releaseDefenseScvs()
{
    if (defenseScvIds_.empty())
    {
        return;
    }

    for (auto scv : Broodwar->self()->getUnits())
    {
        if (!scv ||
            !scv->exists() ||
            scv->getType() != UnitTypes::Terran_SCV ||
            defenseScvIds_.find(scv->getID()) == defenseScvIds_.end())
        {
            continue;
        }

        if (scv->isCarryingMinerals() || scv->isCarryingGas())
        {
            scv->returnCargo();
            continue;
        }

        Unit mineral = nearestMineral(scv->getPosition());
        if (mineral)
        {
            scv->gather(mineral);
        }
    }

    defenseScvIds_.clear();
}

void NeoTerranF::executeOpening()
{
    if (allCount(UnitTypes::Terran_Supply_Depot) > 0)
    {
        ensureSupplyDepot();
    }

    switch (opening_)
    {
    case Opening::BioAcademy:
        executeBioAcademy();
        break;
    case Opening::RaxExpand:
        executeRaxExpand();
        break;
    case Opening::FactoryExpand:
        executeFactoryExpand();
        break;
    case Opening::TwoFactoryPressure:
        executeTwoFactoryPressure();
        break;
    case Opening::OneFactStar:
        executeOneFactStar();
        break;
    }
}

void NeoTerranF::executeBioAcademy()
{
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Terran_Supply_Depot, 1, mainTile_);
    if (supplyUsed() >= 11) ensureBuilding(UnitTypes::Terran_Barracks, 1, mainTile_);
    if (supplyUsed() >= 13) ensureBuilding(UnitTypes::Terran_Barracks, 2, mainTile_);
    if (supplyUsed() >= 15) ensureRefinery(1);
    if (supplyUsed() >= 18) ensureBuilding(UnitTypes::Terran_Academy, 1, mainTile_);
    if (supplyUsed() >= 22) ensureBuilding(UnitTypes::Terran_Engineering_Bay, 1, mainTile_);
    if (supplyUsed() >= 26) ensureExpansion(2);
    if (supplyUsed() >= 34) ensureBuilding(UnitTypes::Terran_Barracks, 4, mainTile_);
}

void NeoTerranF::executeRaxExpand()
{
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Terran_Supply_Depot, 1, mainTile_);
    if (supplyUsed() >= 11) ensureBuilding(UnitTypes::Terran_Barracks, 1, mainTile_);
    if (supplyUsed() >= 14) ensureBuilding(UnitTypes::Terran_Bunker, 1, isValidTile(naturalTile_) ? naturalTile_ : mainTile_, 20);
    if (supplyUsed() >= 16) ensureExpansion(2);
    if (supplyUsed() >= 18) ensureRefinery(1);
    if (supplyUsed() >= 24) ensureBuilding(UnitTypes::Terran_Academy, 1, mainTile_);
    if (supplyUsed() >= 32) ensureBuilding(UnitTypes::Terran_Barracks, 3, mainTile_);
    if (supplyUsed() >= 44) ensureBuilding(UnitTypes::Terran_Factory, 1, mainTile_);
}

void NeoTerranF::executeFactoryExpand()
{
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Terran_Supply_Depot, 1, mainTile_);
    if (supplyUsed() >= 11) ensureBuilding(UnitTypes::Terran_Barracks, 1, mainTile_);
    if (supplyUsed() >= 12) ensureRefinery(1);
    if (supplyUsed() >= 19) ensureBuilding(UnitTypes::Terran_Factory, 1, mainTile_);
    if (supplyUsed() >= 24) ensureAddon(UnitTypes::Terran_Factory, UnitTypes::Terran_Machine_Shop, 1);
    if (supplyUsed() >= 26) ensureExpansion(2);
    if (supplyUsed() >= 34) ensureBuilding(UnitTypes::Terran_Factory, 2, mainTile_);
    if (supplyUsed() >= 44) ensureBuilding(UnitTypes::Terran_Engineering_Bay, 1, mainTile_);
    if (supplyUsed() >= 56) ensureBuilding(UnitTypes::Terran_Armory, 1, mainTile_);
}

void NeoTerranF::executeTwoFactoryPressure()
{
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Terran_Supply_Depot, 1, mainTile_);
    if (supplyUsed() >= 11) ensureBuilding(UnitTypes::Terran_Barracks, 1, mainTile_);
    if (supplyUsed() >= 12) ensureRefinery(1);
    if (supplyUsed() >= 19) ensureBuilding(UnitTypes::Terran_Factory, 1, mainTile_);
    if (supplyUsed() >= 23) ensureBuilding(UnitTypes::Terran_Factory, 2, mainTile_);
    if (supplyUsed() >= 26) ensureAddon(UnitTypes::Terran_Factory, UnitTypes::Terran_Machine_Shop, 1);
    if (supplyUsed() >= 36) ensureBuilding(UnitTypes::Terran_Academy, 1, mainTile_);
    if (supplyUsed() >= 44) ensureExpansion(2);
    if (supplyUsed() >= 58) ensureBuilding(UnitTypes::Terran_Factory, 3, mainTile_);
}

void NeoTerranF::executeOneFactStar()
{
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Terran_Supply_Depot, 1, mainTile_);
    if (supplyUsed() >= 11) ensureBuilding(UnitTypes::Terran_Barracks, 1, mainTile_);
    if (supplyUsed() >= 12) ensureRefinery(1);
    if (supplyUsed() >= 19) ensureBuilding(UnitTypes::Terran_Factory, 1, mainTile_);
    if (supplyUsed() >= 24) ensureBuilding(UnitTypes::Terran_Starport, 1, mainTile_);
    if (supplyUsed() >= 28) ensureAddon(UnitTypes::Terran_Factory, UnitTypes::Terran_Machine_Shop, 1);
    if (supplyUsed() >= 34) ensureExpansion(2);
    if (supplyUsed() >= 42) ensureBuilding(UnitTypes::Terran_Engineering_Bay, 1, mainTile_);
    if (supplyUsed() >= 54) ensureBuilding(UnitTypes::Terran_Factory, 2, mainTile_);
}

void NeoTerranF::manageProduction()
{
    if (opening_ == Opening::BioAcademy || opening_ == Opening::RaxExpand)
    {
        trainFromIdle(UnitTypes::Terran_Barracks, UnitTypes::Terran_Marine, 36);
        if (hasCompleted(UnitTypes::Terran_Academy))
        {
            trainFromIdle(UnitTypes::Terran_Barracks, UnitTypes::Terran_Medic, 8);
        }
    }
    else
    {
        trainFromIdle(UnitTypes::Terran_Barracks, UnitTypes::Terran_Marine, 8);
    }

    if (hasCompleted(UnitTypes::Terran_Factory))
    {
        trainFromIdle(UnitTypes::Terran_Factory, UnitTypes::Terran_Vulture, 18);
        if (hasCompleted(UnitTypes::Terran_Machine_Shop))
        {
            trainFromIdle(UnitTypes::Terran_Factory, UnitTypes::Terran_Siege_Tank_Tank_Mode, 12);
        }
    }

    if (opening_ == Opening::OneFactStar && hasCompleted(UnitTypes::Terran_Starport))
    {
        trainFromIdle(UnitTypes::Terran_Starport, UnitTypes::Terran_Wraith, 6);
    }
}

void NeoTerranF::manageResearch()
{
    if (hasCompleted(UnitTypes::Terran_Academy))
    {
        researchFromIdle(UnitTypes::Terran_Academy, TechTypes::Stim_Packs);
        upgradeFromIdle(UnitTypes::Terran_Academy, UpgradeTypes::U_238_Shells);
    }

    if (hasCompleted(UnitTypes::Terran_Machine_Shop))
    {
        researchFromIdle(UnitTypes::Terran_Machine_Shop, TechTypes::Tank_Siege_Mode);
        upgradeFromIdle(UnitTypes::Terran_Machine_Shop, UpgradeTypes::Ion_Thrusters);
    }

    if (hasCompleted(UnitTypes::Terran_Engineering_Bay))
    {
        upgradeFromIdle(UnitTypes::Terran_Engineering_Bay, UpgradeTypes::Terran_Infantry_Weapons);
    }
}

void NeoTerranF::manageCombat()
{
    const int frame = Broodwar->getFrameCount();
    if (frame - lastAttackFrame_ < 96)
    {
        return;
    }

    const int marines = completedCount(UnitTypes::Terran_Marine);
    const int vultures = completedCount(UnitTypes::Terran_Vulture);
    const int tanks = completedCount(UnitTypes::Terran_Siege_Tank_Tank_Mode) +
        completedCount(UnitTypes::Terran_Siege_Tank_Siege_Mode);
    bool shouldAttack = false;

    if (opening_ == Opening::BioAcademy)
    {
        shouldAttack = marines >= 10 || frame > 24 * 360;
    }
    else if (opening_ == Opening::RaxExpand)
    {
        shouldAttack = marines >= 14 || armyCount() >= 18 || frame > 24 * 430;
    }
    else if (opening_ == Opening::TwoFactoryPressure)
    {
        shouldAttack = vultures >= 5 || tanks >= 2 || frame > 24 * 340;
    }
    else if (opening_ == Opening::OneFactStar)
    {
        shouldAttack = completedCount(UnitTypes::Terran_Wraith) >= 3 || armyCount() >= 10 || frame > 24 * 400;
    }
    else
    {
        shouldAttack = (vultures >= 4 && tanks >= 1) || armyCount() >= 14 || frame > 24 * 420;
    }

    if (!shouldAttack)
    {
        return;
    }

    Position target = attackTarget();
    if (!target)
    {
        return;
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit || !unit->exists() || !unit->isCompleted() || !isCombatUnit(unit->getType()))
        {
            continue;
        }

        if (unit->isLockedDown() || unit->isLoaded() || unit->isStasised())
        {
            continue;
        }

        unit->attack(target);
    }

    lastAttackFrame_ = frame;
}

void NeoTerranF::maybeSendGg()
{
    if (ggSent_)
    {
        return;
    }

    const int frame = Broodwar->getFrameCount();
    const bool lostAllCommandCenters = frame > 24 * 240 && completedCount(UnitTypes::Terran_Command_Center) == 0;
    const bool cannotFightOrProduce =
        frame > 24 * 300 &&
        armyCount() == 0 &&
        completedCount(UnitTypes::Terran_Barracks) == 0 &&
        completedCount(UnitTypes::Terran_Factory) == 0 &&
        completedCount(UnitTypes::Terran_Command_Center) <= 1 &&
        allCount(UnitTypes::Terran_SCV) <= 4;

    if (!lostAllCommandCenters && !cannotFightOrProduce)
    {
        return;
    }

    Broodwar->sendText("gg");
    ggSent_ = true;
    ggFrame_ = frame;
}

int NeoTerranF::supplyUsed() const
{
    return Broodwar->self()->supplyUsed() / 2;
}

int NeoTerranF::supplyTotal() const
{
    return Broodwar->self()->supplyTotal() / 2;
}

int NeoTerranF::allCount(UnitType type) const
{
    return Broodwar->self()->allUnitCount(type);
}

int NeoTerranF::completedCount(UnitType type) const
{
    return Broodwar->self()->completedUnitCount(type);
}

int NeoTerranF::armyCount() const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit && unit->exists() && unit->isCompleted() && isCombatUnit(unit->getType()))
        {
            ++count;
        }
    }

    return count;
}

int NeoTerranF::workerTarget() const
{
    const int commandCenters = std::max(1, allCount(UnitTypes::Terran_Command_Center));
    return std::min(65, 22 + commandCenters * 16);
}

bool NeoTerranF::hasOrBuilding(UnitType type, int count) const
{
    return allCount(type) >= count;
}

bool NeoTerranF::hasCompleted(UnitType type, int count) const
{
    return completedCount(type) >= count;
}

bool NeoTerranF::canAfford(UnitType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoTerranF::canAfford(UpgradeType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoTerranF::canAfford(TechType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoTerranF::ensureSupplyDepot()
{
    if (allCount(UnitTypes::Terran_Supply_Depot) == 0)
    {
        return ensureBuilding(UnitTypes::Terran_Supply_Depot, 1, mainTile_);
    }

    if (supplyTotal() - supplyUsed() <= 4 &&
        allCount(UnitTypes::Terran_Supply_Depot) < 16 &&
        Broodwar->self()->incompleteUnitCount(UnitTypes::Terran_Supply_Depot) == 0)
    {
        return ensureBuilding(UnitTypes::Terran_Supply_Depot, allCount(UnitTypes::Terran_Supply_Depot) + 1, mainTile_);
    }

    return false;
}

bool NeoTerranF::ensureBuilding(UnitType type, int targetCount, TilePosition near, int maxRange)
{
    if (allCount(type) >= targetCount || !canAfford(type))
    {
        return false;
    }

    const int frame = Broodwar->getFrameCount();
    if (frame - lastBuildFrame_ < 10)
    {
        return false;
    }

    if (!isValidTile(near))
    {
        near = mainTile_;
    }

    Unit worker = pickWorker(Position(near));
    if (!worker)
    {
        return false;
    }

    TilePosition buildTile = Broodwar->getBuildLocation(type, near, maxRange, false);
    if (!isValidTile(buildTile))
    {
        buildTile = Broodwar->getBuildLocation(type, mainTile_, 64, false);
    }

    if (!isValidTile(buildTile))
    {
        return false;
    }

    if (worker->build(type, buildTile))
    {
        lastBuildFrame_ = frame;
        return true;
    }

    return false;
}

bool NeoTerranF::ensureRefinery(int targetCount)
{
    if (allCount(UnitTypes::Terran_Refinery) >= targetCount || !canAfford(UnitTypes::Terran_Refinery))
    {
        return false;
    }

    Unit worker = pickWorker(Position(mainTile_));
    if (!worker)
    {
        return false;
    }

    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto geyser : Broodwar->getStaticGeysers())
    {
        if (!geyser || !geyser->exists())
        {
            continue;
        }

        double d = distance(geyser->getPosition(), worker->getPosition());
        if (d < bestDistance && d < 700)
        {
            bestDistance = d;
            best = geyser;
        }
    }

    if (!best)
    {
        return false;
    }

    if (worker->build(UnitTypes::Terran_Refinery, best->getTilePosition()))
    {
        lastBuildFrame_ = Broodwar->getFrameCount();
        return true;
    }

    return false;
}

bool NeoTerranF::ensureExpansion(int targetCount)
{
    if (allCount(UnitTypes::Terran_Command_Center) >= targetCount || !canAfford(UnitTypes::Terran_Command_Center))
    {
        return false;
    }

    TilePosition tile = targetCount <= 2 ? naturalTile_ : findExpansionTile();
    if (!isValidTile(tile) && targetCount <= 2)
    {
        naturalTile_ = findExpansionTile();
        tile = naturalTile_;
    }
    if (!isValidTile(tile))
    {
        return false;
    }

    return ensureBuilding(UnitTypes::Terran_Command_Center, targetCount, tile, 16);
}

bool NeoTerranF::ensureAddon(UnitType producer, UnitType addon, int maxAddons)
{
    if (allCount(addon) >= maxAddons || !canAfford(addon))
    {
        return false;
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != producer ||
            !unit->isCompleted() ||
            unit->isTraining() ||
            unit->getAddon())
        {
            continue;
        }

        if (unit->buildAddon(addon))
        {
            return true;
        }
    }

    return false;
}

bool NeoTerranF::trainFromIdle(UnitType producer, UnitType unitType, int maxCount)
{
    if (allCount(unitType) >= maxCount || !canAfford(unitType))
    {
        return false;
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == producer &&
            unit->isCompleted() &&
            unit->isIdle() &&
            unit->train(unitType))
        {
            return true;
        }
    }

    return false;
}

bool NeoTerranF::researchFromIdle(UnitType producer, TechType techType)
{
    if (Broodwar->self()->hasResearched(techType) ||
        Broodwar->self()->isResearching(techType) ||
        !canAfford(techType))
    {
        return false;
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == producer &&
            unit->isCompleted() &&
            unit->isIdle() &&
            unit->research(techType))
        {
            return true;
        }
    }

    return false;
}

bool NeoTerranF::upgradeFromIdle(UnitType producer, UpgradeType upgradeType)
{
    if (Broodwar->self()->getUpgradeLevel(upgradeType) > 0 ||
        Broodwar->self()->isUpgrading(upgradeType) ||
        !canAfford(upgradeType))
    {
        return false;
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == producer &&
            unit->isCompleted() &&
            unit->isIdle() &&
            unit->upgrade(upgradeType))
        {
            return true;
        }
    }

    return false;
}

Unit NeoTerranF::pickWorker(Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Terran_SCV ||
            !unit->isCompleted() ||
            unit->isConstructing() ||
            unit->isRepairing() ||
            unit->isLoaded() ||
            unit->isStasised())
        {
            continue;
        }

        double d = distance(unit->getPosition(), near);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = unit;
        }
    }

    return best;
}

Unit NeoTerranF::nearestMineral(Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto mineral : Broodwar->getMinerals())
    {
        if (!mineral || !mineral->exists())
        {
            continue;
        }

        double d = distance(mineral->getPosition(), near);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = mineral;
        }
    }

    return best;
}

Unit NeoTerranF::nearestRefineryNeedingWorkers(Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Terran_Refinery ||
            !unit->isCompleted() ||
            gasWorkersFor(unit) >= 3)
        {
            continue;
        }

        double d = distance(unit->getPosition(), near);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = unit;
        }
    }

    return best;
}

Unit NeoTerranF::nearestEnemyThreat(Position near, int radius) const
{
    Unit best = nullptr;
    double bestDistance = radius;
    for (auto unit : Broodwar->enemy() ? Broodwar->enemy()->getUnits() : Unitset())
    {
        if (!unit || !unit->exists() || !unit->isVisible() || !isThreatType(unit->getType()))
        {
            continue;
        }

        double d = distance(unit->getPosition(), near);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = unit;
        }
    }

    return best;
}

Unit NeoTerranF::nearestEnemyTarget() const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    Position anchor = isValidTile(enemyStartTile_) ? Position(enemyStartTile_) : Position(mainTile_);
    for (auto unit : Broodwar->enemy() ? Broodwar->enemy()->getUnits() : Unitset())
    {
        if (!unit || !unit->exists() || !unit->isVisible())
        {
            continue;
        }

        double d = distance(unit->getPosition(), anchor);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = unit;
        }
    }

    return best;
}

int NeoTerranF::combatUnitCountNear(Position near, int radius) const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            !unit->isCompleted() ||
            !isCombatUnit(unit->getType()) ||
            distance(unit->getPosition(), near) > radius)
        {
            continue;
        }

        ++count;
    }

    return count;
}

int NeoTerranF::enemyThreatCountNear(Position near, int radius, bool workersOnly) const
{
    int count = 0;
    for (auto unit : Broodwar->enemy() ? Broodwar->enemy()->getUnits() : Unitset())
    {
        if (!unit || !unit->exists() || !unit->isVisible() || !isThreatType(unit->getType()))
        {
            continue;
        }

        if (workersOnly && !unit->getType().isWorker())
        {
            continue;
        }

        if (distance(unit->getPosition(), near) <= radius)
        {
            ++count;
        }
    }

    return count;
}

int NeoTerranF::gasWorkersFor(Unit refinery) const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == UnitTypes::Terran_SCV &&
            unit->getOrderTarget() == refinery)
        {
            ++count;
        }
    }

    return count;
}

TilePosition NeoTerranF::findExpansionTile() const
{
    TilePosition best = TilePositions::Invalid;
    double bestDistance = std::numeric_limits<double>::max();
    Position main = Position(mainTile_);

    for (auto mineral : Broodwar->getStaticMinerals())
    {
        if (!mineral || !mineral->exists())
        {
            continue;
        }

        double d = distance(mineral->getPosition(), main);
        if (d < 700 || d > bestDistance)
        {
            continue;
        }

        TilePosition candidate = Broodwar->getBuildLocation(UnitTypes::Terran_Command_Center, mineral->getTilePosition(), 14, false);
        if (isValidTile(candidate))
        {
            bestDistance = d;
            best = candidate;
        }
    }

    return best;
}

Position NeoTerranF::attackTarget() const
{
    Unit target = nearestEnemyTarget();
    if (target)
    {
        return target->getPosition();
    }

    if (isValidTile(enemyStartTile_))
    {
        return Position(enemyStartTile_);
    }

    for (auto start : Broodwar->getStartLocations())
    {
        if (start != mainTile_)
        {
            return Position(start);
        }
    }

    return Position(Broodwar->mapWidth() * 16, Broodwar->mapHeight() * 16);
}

double NeoTerranF::distance(Position a, Position b) const
{
    if (!a || !b)
    {
        return std::numeric_limits<double>::max();
    }

    const double dx = static_cast<double>(a.x - b.x);
    const double dy = static_cast<double>(a.y - b.y);
    return std::sqrt(dx * dx + dy * dy);
}

std::string NeoTerranF::configuredBuildId() const
{
    const char* candidates[] =
    {
        "bwapi-data\\AI\\Sparring\\Bots\\NeoTerranF\\sparring-bot.ini",
        "bwapi-data\\AI\\sparring-bot.ini"
    };

    for (const char* path : candidates)
    {
        std::ifstream file(path);
        if (!file)
        {
            continue;
        }

        std::string line;
        while (std::getline(file, line))
        {
            const std::string normalized = trimLower(line);
            const std::string prefix = "build=";
            if (normalized.compare(0, prefix.size(), prefix) == 0)
            {
                return normalized.substr(prefix.size());
            }
        }
    }

    return "";
}

std::string NeoTerranF::openingName() const
{
    switch (opening_)
    {
    case Opening::BioAcademy:
        return "bio academy";
    case Opening::RaxExpand:
        return "barracks expand";
    case Opening::FactoryExpand:
        return "factory expand";
    case Opening::TwoFactoryPressure:
        return "two factory pressure";
    case Opening::OneFactStar:
        return "one fact star";
    }

    return "unknown";
}
