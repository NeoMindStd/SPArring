#include "NeoZergF.h"

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
            type != UnitTypes::Zerg_Egg &&
            type != UnitTypes::Zerg_Larva &&
            type != UnitTypes::Zerg_Overlord &&
            type != UnitTypes::Zerg_Broodling &&
            type.canAttack();
    }

    bool isThreatType(UnitType type)
    {
        return type.canAttack() ||
            type == UnitTypes::Terran_Bunker ||
            type == UnitTypes::Protoss_Zealot ||
            type == UnitTypes::Zerg_Zergling ||
            type == UnitTypes::Terran_Marine ||
            type.isWorker();
    }

    bool isTechHatchery(UnitType type)
    {
        return type == UnitTypes::Zerg_Hatchery ||
            type == UnitTypes::Zerg_Lair ||
            type == UnitTypes::Zerg_Hive;
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

    bool buildIdToOpening(const std::string& buildId, NeoZergF::Opening& opening)
    {
        const std::string id = trimLower(buildId);
        if (id == "overpool_speed")
        {
            opening = NeoZergF::Opening::OverpoolSpeed;
            return true;
        }
        if (id == "two_hatch_muta")
        {
            opening = NeoZergF::Opening::TwoHatchMuta;
            return true;
        }
        if (id == "five_thirty_muta")
        {
            opening = NeoZergF::Opening::FiveThirtyMuta;
            return true;
        }
        if (id == "three_hatch_hydra")
        {
            opening = NeoZergF::Opening::ThreeHatchHydra;
            return true;
        }
        if (id == "five_hatch_hydra")
        {
            opening = NeoZergF::Opening::FiveHatchHydra;
            return true;
        }
        if (id == "nine_pool_speed")
        {
            opening = NeoZergF::Opening::NinePoolSpeed;
            return true;
        }
        if (id == "lurker_contain")
        {
            opening = NeoZergF::Opening::LurkerContain;
            return true;
        }

        return false;
    }
}

void NeoZergF::onStart()
{
    Broodwar->enableFlag(Flag::UserInput);
    Broodwar->setCommandOptimizationLevel(2);
    setInitialMapPositions();
    chooseOpening();
}

void NeoZergF::onEnd(bool isWinner)
{
    if (!isWinner && !ggSent_)
    {
        Broodwar->sendText("gg");
        ggSent_ = true;
        ggFrame_ = Broodwar->getFrameCount();
    }
}

void NeoZergF::onFrame()
{
    if (Broodwar->isReplay() || Broodwar->isPaused() || !Broodwar->self())
    {
        return;
    }

    if (Broodwar->self()->getRace() != Races::Zerg)
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

void NeoZergF::onSendText(std::string text)
{
    Broodwar->sendText("%s", text.c_str());
}

void NeoZergF::onReceiveText(Player, std::string)
{
}

void NeoZergF::onPlayerLeft(Player)
{
}

void NeoZergF::onNukeDetect(Position)
{
}

void NeoZergF::onUnitDiscover(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoZergF::onUnitEvade(Unit)
{
}

void NeoZergF::onUnitShow(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoZergF::onUnitHide(Unit)
{
}

void NeoZergF::onUnitCreate(Unit)
{
}

void NeoZergF::onUnitDestroy(Unit)
{
}

void NeoZergF::onUnitMorph(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoZergF::onUnitRenegade(Unit)
{
}

void NeoZergF::onSaveGame(std::string)
{
}

void NeoZergF::onUnitComplete(Unit)
{
}

void NeoZergF::setInitialMapPositions()
{
    mainTile_ = Broodwar->self()->getStartLocation();
    naturalTile_ = TilePositions::Invalid;
}

void NeoZergF::chooseOpening()
{
    Opening configured = Opening::TwoHatchMuta;
    if (buildIdToOpening(configuredBuildId(), configured))
    {
        opening_ = configured;
        return;
    }

    std::vector<Opening> candidates;
    Race enemyRace = Broodwar->enemy() ? Broodwar->enemy()->getRace() : Races::Unknown;
    if (enemyRace == Races::Protoss)
    {
        candidates.push_back(Opening::ThreeHatchHydra);
        candidates.push_back(Opening::FiveHatchHydra);
        candidates.push_back(Opening::NinePoolSpeed);
    }
    else if (enemyRace == Races::Terran)
    {
        candidates.push_back(Opening::TwoHatchMuta);
        candidates.push_back(Opening::FiveThirtyMuta);
        candidates.push_back(Opening::LurkerContain);
    }
    else
    {
        candidates.push_back(Opening::OverpoolSpeed);
        candidates.push_back(Opening::NinePoolSpeed);
        candidates.push_back(Opening::TwoHatchMuta);
    }

    candidates.push_back(Opening::OverpoolSpeed);
    const int seed = static_cast<int>(std::time(nullptr)) ^
        Broodwar->getFrameCount() ^
        (mainTile_.x * 73856093) ^
        (mainTile_.y * 19349663);
    std::srand(seed);
    opening_ = candidates[std::rand() % candidates.size()];
}

void NeoZergF::updateEnemyStart(Unit unit)
{
    if (!unit || !unit->exists() || !unit->getPlayer() || unit->getPlayer() == Broodwar->self())
    {
        return;
    }

    if (unit->getPlayer()->isEnemy(Broodwar->self()) && unit->getType().isBuilding())
    {
        enemyStartTile_ = unit->getTilePosition();
    }
}

void NeoZergF::manageWorkers()
{
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Zerg_Drone ||
            !unit->isCompleted() ||
            unit->isMorphing() ||
            unit->isConstructing())
        {
            continue;
        }

        if (defenseDroneIds_.find(unit->getID()) != defenseDroneIds_.end())
        {
            continue;
        }

        Unit gasTarget = unit->getTarget();
        if (!gasTarget ||
            !gasTarget->exists() ||
            gasTarget->getType() != UnitTypes::Zerg_Extractor)
        {
            gasTarget = unit->getOrderTarget();
        }

        if (unit->isIdle() ||
            (unit->isGatheringGas() &&
                gasWorkersFor(gasTarget) > gasWorkerTarget(gasTarget)))
        {
            Unit gas = nearestExtractorNeedingWorkers(unit->getPosition());
            if (gas)
            {
                unit->gather(gas);
                continue;
            }

            Unit mineral = nearestMineral(unit->getPosition());
            if (mineral)
            {
                unit->gather(mineral);
            }
        }
    }

    if (Broodwar->getFrameCount() - lastScoutFrame_ > 24 * 20)
    {
        for (auto unit : Broodwar->self()->getUnits())
        {
            if (unit &&
                unit->exists() &&
                unit->getType() == UnitTypes::Zerg_Overlord &&
                unit->isCompleted() &&
                (unit->isIdle() || unit->getOrder() == Orders::PlayerGuard))
            {
                unit->move(attackTarget());
                lastScoutFrame_ = Broodwar->getFrameCount();
                break;
            }
        }
    }
}

bool NeoZergF::handleEmergencyDefense()
{
    Position mainPosition(mainTile_);
    Unit threat = nearestEnemyThreat(mainPosition, 620);
    if (!threat)
    {
        releaseDefenseDrones();
        return false;
    }

    const int workerThreatCount = enemyThreatCountNear(mainPosition, 620, true);
    const int combatNearMain = combatUnitCountNear(mainPosition, 760);
    int desiredDrones = 0;
    if (workerThreatCount > 0 && combatNearMain < 2)
    {
        desiredDrones = std::min(3, std::max(1, workerThreatCount));
    }
    else if (combatNearMain == 0 && supplyUsed() < 22)
    {
        desiredDrones = 2;
    }

    std::vector<Unit> currentDefense;
    for (auto drone : Broodwar->self()->getUnits())
    {
        if (drone &&
            drone->exists() &&
            drone->getType() == UnitTypes::Zerg_Drone &&
            drone->isCompleted() &&
            defenseDroneIds_.find(drone->getID()) != defenseDroneIds_.end())
        {
            if (distance(drone->getPosition(), mainPosition) > 760)
            {
                defenseDroneIds_.erase(drone->getID());
                Unit mineral = nearestMineral(drone->getPosition());
                if (mineral)
                {
                    drone->gather(mineral);
                }
            }
            else
            {
                currentDefense.push_back(drone);
            }
        }
    }

    while (static_cast<int>(currentDefense.size()) < desiredDrones)
    {
        Unit drone = pickWorker(mainPosition);
        if (!drone)
        {
            break;
        }

        defenseDroneIds_.insert(drone->getID());
        currentDefense.push_back(drone);
    }

    for (auto drone : currentDefense)
    {
        if (drone && threat && !drone->isAttacking())
        {
            drone->attack(threat);
        }
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit || !unit->exists() || !unit->isCompleted() || !isCombatUnit(unit->getType()))
        {
            continue;
        }

        if (distance(unit->getPosition(), threat->getPosition()) < 820)
        {
            unit->attack(threat);
        }
    }

    return true;
}

void NeoZergF::releaseDefenseDrones()
{
    if (defenseDroneIds_.empty())
    {
        return;
    }

    for (auto drone : Broodwar->self()->getUnits())
    {
        if (!drone ||
            !drone->exists() ||
            defenseDroneIds_.find(drone->getID()) == defenseDroneIds_.end())
        {
            continue;
        }

        Unit mineral = nearestMineral(drone->getPosition());
        if (mineral)
        {
            drone->gather(mineral);
        }
    }

    defenseDroneIds_.clear();
}

void NeoZergF::executeOpening()
{
    if (allCount(UnitTypes::Zerg_Overlord) > 0)
    {
        ensureOverlord();
    }

    switch (opening_)
    {
    case Opening::OverpoolSpeed:
        executeOverpoolSpeed();
        break;
    case Opening::TwoHatchMuta:
        executeTwoHatchMuta();
        break;
    case Opening::FiveThirtyMuta:
        executeFiveThirtyMuta();
        break;
    case Opening::ThreeHatchHydra:
        executeThreeHatchHydra();
        break;
    case Opening::FiveHatchHydra:
        executeFiveHatchHydra();
        break;
    case Opening::NinePoolSpeed:
        executeNinePoolSpeed();
        break;
    case Opening::LurkerContain:
        executeLurkerContain();
        break;
    }
}

void NeoZergF::executeOverpoolSpeed()
{
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Zerg_Spawning_Pool, 1, mainTile_);
    if (supplyUsed() >= 10) ensureExtractor(1);
    if (supplyUsed() >= 15) ensureExpansion(2);
    if (supplyUsed() >= 24) ensureBuilding(UnitTypes::Zerg_Hydralisk_Den, 1, mainTile_);
}

void NeoZergF::executeTwoHatchMuta()
{
    if (supplyUsed() >= 12 && allCount(UnitTypes::Zerg_Hatchery) < 2)
    {
        if (ensureExpansion(2) || supplyUsed() < 14)
        {
            return;
        }
    }
    if (supplyUsed() >= 12) ensureExtractor(1);
    if (supplyUsed() >= 12) ensureBuilding(UnitTypes::Zerg_Spawning_Pool, 1, mainTile_);
    if (supplyUsed() >= 20) ensureLair();
    if (hasCompleted(UnitTypes::Zerg_Lair)) ensureBuilding(UnitTypes::Zerg_Spire, 1, mainTile_);
    if (supplyUsed() >= 38) ensureBuilding(UnitTypes::Zerg_Hydralisk_Den, 1, mainTile_);
}

void NeoZergF::executeFiveThirtyMuta()
{
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Zerg_Spawning_Pool, 1, mainTile_);
    if (supplyUsed() >= 10) ensureExtractor(1);
    if (supplyUsed() >= 14) ensureExpansion(2);
    if (supplyUsed() >= 19) ensureLair();
    if (hasCompleted(UnitTypes::Zerg_Lair)) ensureBuilding(UnitTypes::Zerg_Spire, 1, mainTile_);
    if (supplyUsed() >= 34) ensureExpansion(3);
}

void NeoZergF::executeThreeHatchHydra()
{
    if (supplyUsed() >= 12 && allCount(UnitTypes::Zerg_Hatchery) < 2)
    {
        if (ensureExpansion(2) || supplyUsed() < 14)
        {
            return;
        }
    }
    if (supplyUsed() >= 12) ensureBuilding(UnitTypes::Zerg_Spawning_Pool, 1, mainTile_);
    if (supplyUsed() >= 16) ensureExpansion(3);
    if (supplyUsed() >= 18) ensureExtractor(1);
    if (supplyUsed() >= 22) ensureBuilding(UnitTypes::Zerg_Hydralisk_Den, 1, mainTile_);
    if (supplyUsed() >= 44) ensureBuilding(UnitTypes::Zerg_Evolution_Chamber, 1, mainTile_);
}

void NeoZergF::executeFiveHatchHydra()
{
    if (supplyUsed() >= 12 && allCount(UnitTypes::Zerg_Hatchery) < 2)
    {
        if (ensureExpansion(2) || supplyUsed() < 14)
        {
            return;
        }
    }
    if (supplyUsed() >= 12) ensureBuilding(UnitTypes::Zerg_Spawning_Pool, 1, mainTile_);
    if (supplyUsed() >= 18) ensureExpansion(3);
    if (supplyUsed() >= 20) ensureExtractor(1);
    if (supplyUsed() >= 24) ensureBuilding(UnitTypes::Zerg_Hydralisk_Den, 1, mainTile_);
    if (supplyUsed() >= 38) ensureExpansion(4);
    if (supplyUsed() >= 52) ensureExpansion(5);
    if (supplyUsed() >= 50) ensureBuilding(UnitTypes::Zerg_Evolution_Chamber, 1, mainTile_);
}

void NeoZergF::executeNinePoolSpeed()
{
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Zerg_Spawning_Pool, 1, mainTile_);
    if (supplyUsed() >= 10) ensureExtractor(1);
    if (supplyUsed() >= 14) ensureExpansion(2);
    if (supplyUsed() >= 24) ensureBuilding(UnitTypes::Zerg_Hydralisk_Den, 1, mainTile_);
}

void NeoZergF::executeLurkerContain()
{
    if (supplyUsed() >= 12 && allCount(UnitTypes::Zerg_Hatchery) < 2)
    {
        if (ensureExpansion(2) || supplyUsed() < 14)
        {
            return;
        }
    }
    if (supplyUsed() >= 12) ensureBuilding(UnitTypes::Zerg_Spawning_Pool, 1, mainTile_);
    if (supplyUsed() >= 12) ensureExtractor(1);
    if (supplyUsed() >= 20) ensureLair();
    if (supplyUsed() >= 22) ensureBuilding(UnitTypes::Zerg_Hydralisk_Den, 1, mainTile_);
    if (supplyUsed() >= 40) ensureExpansion(3);
}

void NeoZergF::manageProduction()
{
    const bool mutaOpening = opening_ == Opening::TwoHatchMuta || opening_ == Opening::FiveThirtyMuta;
    const bool hydraOpening = opening_ == Opening::ThreeHatchHydra ||
        opening_ == Opening::FiveHatchHydra ||
        opening_ == Opening::LurkerContain;
    const bool lingOpening = opening_ == Opening::OverpoolSpeed || opening_ == Opening::NinePoolSpeed;
    const bool hatchFirstOpening = opening_ == Opening::TwoHatchMuta ||
        opening_ == Opening::ThreeHatchHydra ||
        opening_ == Opening::FiveHatchHydra ||
        opening_ == Opening::LurkerContain;
    const bool reservingForSecondHatchery = hatchFirstOpening &&
        allCount(UnitTypes::Zerg_Hatchery) < 2 &&
        supplyUsed() >= 12 &&
        supplyUsed() < 15;
    const bool reservingForEarlyExtractor = (mutaOpening || opening_ == Opening::LurkerContain) &&
        allCount(UnitTypes::Zerg_Extractor) < 1 &&
        supplyUsed() >= 12 &&
        supplyUsed() < 22;

    const int desiredDrones = workerTarget();
    if (!reservingForSecondHatchery &&
        !reservingForEarlyExtractor &&
        completedCount(UnitTypes::Zerg_Drone) < desiredDrones &&
        supplyTotal() - supplyUsed() > 0)
    {
        if (trainLarva(UnitTypes::Zerg_Drone, desiredDrones))
        {
            return;
        }
    }

    if (ensureOverlord())
    {
        return;
    }

    if (mutaOpening && hasCompleted(UnitTypes::Zerg_Spire))
    {
        trainLarva(UnitTypes::Zerg_Mutalisk, 12);
    }

    if (hydraOpening && hasCompleted(UnitTypes::Zerg_Hydralisk_Den))
    {
        trainLarva(UnitTypes::Zerg_Hydralisk, opening_ == Opening::FiveHatchHydra ? 42 : 24);
    }

    const int zerglingCap = lingOpening ? 36 : (mutaOpening ? 6 : 12);
    if (zerglingCap > 0 && (lingOpening || completedCount(UnitTypes::Zerg_Zergling) < zerglingCap))
    {
        if (hasCompleted(UnitTypes::Zerg_Spawning_Pool))
        {
            trainLarva(UnitTypes::Zerg_Zergling, zerglingCap);
        }
    }

    if (mutaOpening && hasCompleted(UnitTypes::Zerg_Hydralisk_Den) && completedCount(UnitTypes::Zerg_Mutalisk) >= 6)
    {
        trainLarva(UnitTypes::Zerg_Hydralisk, 16);
    }

    if (opening_ == Opening::LurkerContain)
    {
        morphHydraliskToLurker(6);
    }
}

void NeoZergF::manageResearch()
{
    if (hasCompleted(UnitTypes::Zerg_Spawning_Pool))
    {
        upgradeFromIdle(UnitTypes::Zerg_Spawning_Pool, UpgradeTypes::Metabolic_Boost);
    }

    if (hasCompleted(UnitTypes::Zerg_Hydralisk_Den))
    {
        upgradeFromIdle(UnitTypes::Zerg_Hydralisk_Den, UpgradeTypes::Grooved_Spines);
        upgradeFromIdle(UnitTypes::Zerg_Hydralisk_Den, UpgradeTypes::Muscular_Augments);
        if (opening_ == Opening::LurkerContain && hasCompleted(UnitTypes::Zerg_Lair))
        {
            researchFromIdle(UnitTypes::Zerg_Hydralisk_Den, TechTypes::Lurker_Aspect);
        }
    }
}

void NeoZergF::manageCombat()
{
    const int frame = Broodwar->getFrameCount();
    if (frame - lastAttackFrame_ < 96)
    {
        return;
    }

    lastAttackFrame_ = frame;
    const int zerglings = completedCount(UnitTypes::Zerg_Zergling);
    const int hydras = completedCount(UnitTypes::Zerg_Hydralisk);
    const int mutas = completedCount(UnitTypes::Zerg_Mutalisk);
    const int lurkers = completedCount(UnitTypes::Zerg_Lurker);

    bool shouldAttack = false;
    if (opening_ == Opening::TwoHatchMuta || opening_ == Opening::FiveThirtyMuta)
    {
        shouldAttack = mutas >= 6 || armyCount() >= 22 || frame > 24 * 430;
    }
    else if (opening_ == Opening::ThreeHatchHydra || opening_ == Opening::FiveHatchHydra)
    {
        shouldAttack = hydras >= 10 || armyCount() >= 24 || frame > 24 * 390;
    }
    else if (opening_ == Opening::LurkerContain)
    {
        shouldAttack = lurkers >= 2 || hydras >= 12 || frame > 24 * 420;
    }
    else
    {
        shouldAttack = zerglings >= 10 || armyCount() >= 18 || frame > 24 * 330;
    }

    Position target = shouldAttack ? attackTarget() : Position(mainTile_);
    if (!target.isValid())
    {
        target = Position(Broodwar->self()->getStartLocation());
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit || !unit->exists() || !unit->isCompleted() || !isCombatUnit(unit->getType()))
        {
            continue;
        }

        if (unit->getType() == UnitTypes::Zerg_Lurker && !unit->isBurrowed() && shouldAttack)
        {
            if (distance(unit->getPosition(), target) < 420)
            {
                unit->burrow();
                continue;
            }
        }

        if (shouldAttack)
        {
            Unit enemy = nearestEnemyTarget();
            if (enemy && distance(unit->getPosition(), enemy->getPosition()) < 820)
            {
                unit->attack(enemy);
            }
            else
            {
                unit->attack(target);
            }
        }
        else if (distance(unit->getPosition(), Position(mainTile_)) > 760)
        {
            unit->move(Position(mainTile_));
        }
    }
}

void NeoZergF::maybeSendGg()
{
    if (ggSent_)
    {
        return;
    }

    if (completedCount(UnitTypes::Zerg_Hatchery) +
        completedCount(UnitTypes::Zerg_Lair) +
        completedCount(UnitTypes::Zerg_Hive) == 0)
    {
        Broodwar->sendText("gg");
        ggSent_ = true;
        ggFrame_ = Broodwar->getFrameCount();
        return;
    }

    const int myArmy = armyCount();
    Player enemy = Broodwar->enemy();
    const int enemyVisibleArmy = enemy
        ? static_cast<int>(std::count_if(
            enemy->getUnits().begin(),
            enemy->getUnits().end(),
            [](Unit unit)
            {
                return unit && unit->exists() && isCombatUnit(unit->getType());
            }))
        : 0;

    if (Broodwar->getFrameCount() > 24 * 480 &&
        myArmy <= 2 &&
        enemyVisibleArmy >= 12)
    {
        Broodwar->sendText("gg");
        ggSent_ = true;
        ggFrame_ = Broodwar->getFrameCount();
    }
}

int NeoZergF::supplyUsed() const
{
    return Broodwar->self()->supplyUsed() / 2;
}

int NeoZergF::supplyTotal() const
{
    return Broodwar->self()->supplyTotal() / 2;
}

int NeoZergF::allCount(UnitType type) const
{
    if (type == UnitTypes::Zerg_Hatchery)
    {
        int count = Broodwar->self()->allUnitCount(UnitTypes::Zerg_Hatchery) +
            Broodwar->self()->allUnitCount(UnitTypes::Zerg_Lair) +
            Broodwar->self()->allUnitCount(UnitTypes::Zerg_Hive);
        for (auto unit : Broodwar->self()->getUnits())
        {
            if (unit &&
                unit->exists() &&
                !isTechHatchery(unit->getType()) &&
                (unit->getBuildType() == UnitTypes::Zerg_Hatchery ||
                    unit->getBuildType() == UnitTypes::Zerg_Lair ||
                    unit->getBuildType() == UnitTypes::Zerg_Hive))
            {
                count++;
            }
        }

        return count;
    }

    int count = Broodwar->self()->allUnitCount(type);
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() != type &&
            unit->getBuildType() == type)
        {
            count++;
        }
    }

    return count;
}

int NeoZergF::completedCount(UnitType type) const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit || !unit->exists() || !unit->isCompleted())
        {
            continue;
        }

        if (type == UnitTypes::Zerg_Hatchery)
        {
            if (isTechHatchery(unit->getType()))
            {
                count++;
            }
        }
        else if (unit->getType() == type)
        {
            count++;
        }
    }

    return count;
}

int NeoZergF::armyCount() const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit && unit->exists() && unit->isCompleted() && isCombatUnit(unit->getType()))
        {
            count++;
        }
    }

    return count;
}

int NeoZergF::workerTarget() const
{
    const int hatcheries = std::max(1, allCount(UnitTypes::Zerg_Hatchery));
    int target = 11 + hatcheries * 8;
    if (opening_ == Opening::NinePoolSpeed || opening_ == Opening::OverpoolSpeed)
    {
        target = std::min(target, supplyUsed() < 22 ? 12 : 26);
    }
    else if (opening_ == Opening::FiveHatchHydra)
    {
        target += 8;
    }

    return std::min(target, 58);
}

bool NeoZergF::hasOrMorphing(UnitType type, int count) const
{
    return allCount(type) >= count;
}

bool NeoZergF::hasCompleted(UnitType type, int count) const
{
    return completedCount(type) >= count;
}

bool NeoZergF::canAfford(UnitType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice() &&
        Broodwar->self()->supplyTotal() - Broodwar->self()->supplyUsed() >= type.supplyRequired();
}

bool NeoZergF::canAfford(UpgradeType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoZergF::canAfford(TechType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoZergF::ensureOverlord()
{
    if (supplyTotal() - supplyUsed() > 0 ||
        allCount(UnitTypes::Zerg_Overlord) >= 18 ||
        allCount(UnitTypes::Zerg_Overlord) > completedCount(UnitTypes::Zerg_Overlord))
    {
        return false;
    }

    return trainLarva(UnitTypes::Zerg_Overlord, allCount(UnitTypes::Zerg_Overlord) + 1);
}

bool NeoZergF::ensureBuilding(UnitType type, int targetCount, TilePosition near, int maxRange)
{
    if (hasOrMorphing(type, targetCount) || !canAfford(type))
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

bool NeoZergF::ensureExtractor(int targetCount)
{
    if (allCount(UnitTypes::Zerg_Extractor) >= targetCount || !canAfford(UnitTypes::Zerg_Extractor))
    {
        return false;
    }

    Unit bestGeyser = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    Position mainPosition(mainTile_);
    for (auto geyser : Broodwar->getStaticGeysers())
    {
        if (!geyser || !geyser->exists())
        {
            continue;
        }

        bool alreadyTaken = false;
        for (auto unit : Broodwar->getUnitsOnTile(geyser->getTilePosition()))
        {
            if (unit && unit->exists() && unit->getType() == UnitTypes::Zerg_Extractor)
            {
                alreadyTaken = true;
                break;
            }
        }

        if (alreadyTaken)
        {
            continue;
        }

        const double d = distance(geyser->getPosition(), mainPosition);
        if (d < bestDistance)
        {
            bestDistance = d;
            bestGeyser = geyser;
        }
    }

    if (!bestGeyser)
    {
        return false;
    }

    Unit worker = pickWorker(bestGeyser->getPosition());
    if (!worker)
    {
        return false;
    }

    if (worker->build(UnitTypes::Zerg_Extractor, bestGeyser->getTilePosition()))
    {
        lastBuildFrame_ = Broodwar->getFrameCount();
        return true;
    }

    return false;
}

bool NeoZergF::ensureExpansion(int targetCount)
{
    if (allCount(UnitTypes::Zerg_Hatchery) >= targetCount || !canAfford(UnitTypes::Zerg_Hatchery))
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
        return targetCount > 2 && ensureBuilding(UnitTypes::Zerg_Hatchery, targetCount, mainTile_, 64);
    }

    const int range = targetCount <= 2 ? 16 : 48;
    if (ensureBuilding(UnitTypes::Zerg_Hatchery, targetCount, tile, range))
    {
        return true;
    }

    return targetCount > 2 && ensureBuilding(UnitTypes::Zerg_Hatchery, targetCount, mainTile_, 64);
}

bool NeoZergF::ensureLair()
{
    if (allCount(UnitTypes::Zerg_Lair) + allCount(UnitTypes::Zerg_Hive) > 0 ||
        !hasCompleted(UnitTypes::Zerg_Spawning_Pool) ||
        !canAfford(UnitTypes::Zerg_Lair))
    {
        return false;
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == UnitTypes::Zerg_Hatchery &&
            unit->isCompleted() &&
            !unit->isMorphing())
        {
            return unit->morph(UnitTypes::Zerg_Lair);
        }
    }

    return false;
}

bool NeoZergF::trainLarva(UnitType type, int maxCount)
{
    if (allCount(type) >= maxCount || !canAfford(type))
    {
        return false;
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == UnitTypes::Zerg_Larva &&
            unit->train(type))
        {
            return true;
        }
    }

    return false;
}

bool NeoZergF::researchFromIdle(UnitType producer, TechType techType)
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
            !unit->isResearching() &&
            unit->research(techType))
        {
            return true;
        }
    }

    return false;
}

bool NeoZergF::upgradeFromIdle(UnitType producer, UpgradeType upgradeType)
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
            !unit->isUpgrading() &&
            unit->upgrade(upgradeType))
        {
            return true;
        }
    }

    return false;
}

bool NeoZergF::morphHydraliskToLurker(int maxCount)
{
    if (!Broodwar->self()->hasResearched(TechTypes::Lurker_Aspect) ||
        allCount(UnitTypes::Zerg_Lurker) >= maxCount ||
        !canAfford(UnitTypes::Zerg_Lurker))
    {
        return false;
    }

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == UnitTypes::Zerg_Hydralisk &&
            unit->isCompleted() &&
            unit->morph(UnitTypes::Zerg_Lurker))
        {
            return true;
        }
    }

    return false;
}

Unit NeoZergF::pickWorker(Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Zerg_Drone ||
            !unit->isCompleted() ||
            unit->isMorphing() ||
            unit->isConstructing() ||
            defenseDroneIds_.find(unit->getID()) != defenseDroneIds_.end())
        {
            continue;
        }

        const double d = distance(unit->getPosition(), near);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = unit;
        }
    }

    return best;
}

Unit NeoZergF::nearestMineral(Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto mineral : Broodwar->getMinerals())
    {
        if (!mineral || !mineral->exists() || mineral->getResources() <= 0)
        {
            continue;
        }

        const double d = distance(mineral->getPosition(), near);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = mineral;
        }
    }

    return best;
}

Unit NeoZergF::nearestExtractorNeedingWorkers(Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Zerg_Extractor ||
            !unit->isCompleted() ||
            gasWorkersFor(unit) >= gasWorkerTarget(unit))
        {
            continue;
        }

        const double d = distance(unit->getPosition(), near);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = unit;
        }
    }

    return best;
}

Unit NeoZergF::nearestEnemyThreat(Position near, int radius) const
{
    Player enemy = Broodwar->enemy();
    if (!enemy)
    {
        return nullptr;
    }

    Unit best = nullptr;
    double bestDistance = radius;
    for (auto unit : enemy->getUnits())
    {
        if (!unit || !unit->exists() || !isThreatType(unit->getType()))
        {
            continue;
        }

        const double d = distance(unit->getPosition(), near);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = unit;
        }
    }

    return best;
}

Unit NeoZergF::nearestEnemyTarget() const
{
    Player enemy = Broodwar->enemy();
    if (!enemy)
    {
        return nullptr;
    }

    Unit best = nullptr;
    double bestScore = std::numeric_limits<double>::max();
    Position origin = Position(mainTile_);
    for (auto unit : enemy->getUnits())
    {
        if (!unit || !unit->exists() || !unit->isVisible())
        {
            continue;
        }

        double score = distance(unit->getPosition(), origin);
        if (unit->getType().isBuilding())
        {
            score -= 300;
        }

        if (score < bestScore)
        {
            bestScore = score;
            best = unit;
        }
    }

    return best;
}

int NeoZergF::combatUnitCountNear(Position near, int radius) const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->isCompleted() &&
            isCombatUnit(unit->getType()) &&
            distance(unit->getPosition(), near) <= radius)
        {
            count++;
        }
    }

    return count;
}

int NeoZergF::enemyThreatCountNear(Position near, int radius, bool workersOnly) const
{
    Player enemy = Broodwar->enemy();
    if (!enemy)
    {
        return 0;
    }

    int count = 0;
    for (auto unit : enemy->getUnits())
    {
        if (!unit || !unit->exists())
        {
            continue;
        }

        if (workersOnly && !unit->getType().isWorker())
        {
            continue;
        }

        if (!workersOnly && !isThreatType(unit->getType()))
        {
            continue;
        }

        if (distance(unit->getPosition(), near) <= radius)
        {
            count++;
        }
    }

    return count;
}

int NeoZergF::gasWorkersFor(Unit extractor) const
{
    if (!extractor)
    {
        return 0;
    }

    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        Unit target = unit ? unit->getTarget() : nullptr;
        if (unit &&
            unit->exists() &&
            unit->getType() == UnitTypes::Zerg_Drone &&
            (((target &&
                target->getType() == UnitTypes::Zerg_Extractor &&
                unit->getTarget() == extractor) ||
                unit->getOrderTarget() == extractor)))
        {
            count++;
        }
    }

    return count;
}

int NeoZergF::gasWorkerTarget(Unit extractor) const
{
    if (!extractor || !extractor->exists() || !extractor->isCompleted())
    {
        return 0;
    }

    const int gas = Broodwar->self()->gas();
    const int minerals = Broodwar->self()->minerals();
    const int supply = supplyUsed();

    if (gas >= 300 && minerals < 200)
    {
        return 1;
    }

    if (supply < 40 && gas >= 180 && minerals < 150)
    {
        return 1;
    }

    if (supply < 32 && gas >= 110 && minerals < 120)
    {
        return 2;
    }

    return 3;
}

TilePosition NeoZergF::findExpansionTile() const
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

        const double d = distance(mineral->getPosition(), main);
        if (d < 700 || d > bestDistance)
        {
            continue;
        }

        TilePosition candidate = Broodwar->getBuildLocation(UnitTypes::Zerg_Hatchery, mineral->getTilePosition(), 14, false);
        if (isValidTile(candidate))
        {
            bestDistance = d;
            best = candidate;
        }
    }

    return best;
}

Position NeoZergF::attackTarget() const
{
    Unit visibleTarget = nearestEnemyTarget();
    if (visibleTarget)
    {
        return visibleTarget->getPosition();
    }

    if (isValidTile(enemyStartTile_))
    {
        return Position(enemyStartTile_);
    }

    Position main = Position(mainTile_);
    TilePosition best = TilePositions::Invalid;
    double bestDistance = -1;
    for (auto start : Broodwar->getStartLocations())
    {
        if (!isValidTile(start) || start == mainTile_)
        {
            continue;
        }

        const double d = distance(Position(start), main);
        if (d > bestDistance)
        {
            bestDistance = d;
            best = start;
        }
    }

    return isValidTile(best) ? Position(best) : main;
}

double NeoZergF::distance(Position a, Position b) const
{
    if (!a.isValid() || !b.isValid())
    {
        return std::numeric_limits<double>::max() / 4;
    }

    const double dx = static_cast<double>(a.x - b.x);
    const double dy = static_cast<double>(a.y - b.y);
    return std::sqrt(dx * dx + dy * dy);
}

std::string NeoZergF::configuredBuildId() const
{
    const char* candidates[] =
    {
        "bwapi-data\\AI\\Sparring\\Bots\\NeoZergF\\sparring-bot.ini",
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
            const auto equals = line.find('=');
            if (equals == std::string::npos)
            {
                continue;
            }

            if (trimLower(line.substr(0, equals)) == "build")
            {
                return trimLower(line.substr(equals + 1));
            }
        }
    }

    return "";
}

std::string NeoZergF::openingName() const
{
    switch (opening_)
    {
    case Opening::OverpoolSpeed:
        return "overpool speed";
    case Opening::TwoHatchMuta:
        return "two hatch muta";
    case Opening::FiveThirtyMuta:
        return "five thirty muta";
    case Opening::ThreeHatchHydra:
        return "three hatch hydra";
    case Opening::FiveHatchHydra:
        return "five hatch hydra";
    case Opening::NinePoolSpeed:
        return "nine pool speed";
    case Opening::LurkerContain:
        return "lurker contain";
    }

    return "unknown";
}
