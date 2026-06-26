#include "NeoProtossF.h"

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
            type != UnitTypes::Protoss_Observer &&
            type != UnitTypes::Protoss_Scarab &&
            type != UnitTypes::Protoss_Interceptor &&
            type.canAttack();
    }

    bool isThreatType(UnitType type)
    {
        return type.canAttack() ||
            type == UnitTypes::Terran_Bunker ||
            type == UnitTypes::Terran_Marine ||
            type == UnitTypes::Zerg_Zergling ||
            type == UnitTypes::Protoss_Zealot ||
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

    bool buildIdToOpening(const std::string& buildId, NeoProtossF::Opening& opening)
    {
        const std::string id = trimLower(buildId);
        if (id == "1012")
        {
            opening = NeoProtossF::Opening::TwoGate1012;
            return true;
        }
        if (id == "fast_power_dragoon")
        {
            opening = NeoProtossF::Opening::FastPowerDragoon;
            return true;
        }
        if (id == "23_nexus")
        {
            opening = NeoProtossF::Opening::Nexus23;
            return true;
        }
        if (id == "29_arbiter")
        {
            opening = NeoProtossF::Opening::Arbiter29;
            return true;
        }
        if (id == "forge_double")
        {
            opening = NeoProtossF::Opening::ForgeDouble;
            return true;
        }
        if (id == "gate_double_corsair_dark")
        {
            opening = NeoProtossF::Opening::GateDoubleCorsairDark;
            return true;
        }
        if (id == "naked_double")
        {
            opening = NeoProtossF::Opening::NakedDouble;
            return true;
        }
        if (id == "real_fast_dark")
        {
            opening = NeoProtossF::Opening::RealFastDark;
            return true;
        }
        if (id == "forward_gate_dark")
        {
            opening = NeoProtossF::Opening::ForwardGateDark;
            return true;
        }

        return false;
    }
}

void NeoProtossF::onStart()
{
    Broodwar->enableFlag(Flag::UserInput);
    Broodwar->setCommandOptimizationLevel(2);
    setInitialMapPositions();
    chooseOpening();
}

void NeoProtossF::onEnd(bool isWinner)
{
    if (!isWinner && !ggSent_)
    {
        Broodwar->sendText("gg");
        ggSent_ = true;
        ggFrame_ = Broodwar->getFrameCount();
    }
}

void NeoProtossF::onFrame()
{
    if (Broodwar->isReplay() || Broodwar->isPaused() || !Broodwar->self())
    {
        return;
    }

    if (Broodwar->self()->getRace() != Races::Protoss)
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
    return;
}

void NeoProtossF::onSendText(std::string text)
{
    Broodwar->sendText("%s", text.c_str());
}

void NeoProtossF::onReceiveText(Player, std::string)
{
}

void NeoProtossF::onPlayerLeft(Player)
{
}

void NeoProtossF::onNukeDetect(Position)
{
}

void NeoProtossF::onUnitDiscover(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoProtossF::onUnitEvade(Unit)
{
}

void NeoProtossF::onUnitShow(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoProtossF::onUnitHide(Unit)
{
}

void NeoProtossF::onUnitCreate(Unit)
{
}

void NeoProtossF::onUnitDestroy(Unit)
{
}

void NeoProtossF::onUnitMorph(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoProtossF::onUnitRenegade(Unit)
{
}

void NeoProtossF::onSaveGame(std::string)
{
}

void NeoProtossF::onUnitComplete(Unit)
{
}

void NeoProtossF::setInitialMapPositions()
{
    mainTile_ = Broodwar->self()->getStartLocation();
    naturalTile_ = TilePositions::Invalid;
}

void NeoProtossF::chooseOpening()
{
    Opening configured = Opening::FastPowerDragoon;
    if (buildIdToOpening(configuredBuildId(), configured))
    {
        opening_ = configured;
        return;
    }

    std::vector<Opening> candidates;
    candidates.push_back(Opening::TwoGate1012);
    candidates.push_back(Opening::FastPowerDragoon);

    Race enemyRace = Broodwar->enemy() ? Broodwar->enemy()->getRace() : Races::Unknown;
    if (enemyRace == Races::Terran)
    {
        candidates.push_back(Opening::Nexus23);
        candidates.push_back(Opening::Arbiter29);
        candidates.push_back(Opening::NakedDouble);
        candidates.push_back(Opening::RealFastDark);
        candidates.push_back(Opening::ForwardGateDark);
    }
    else if (enemyRace == Races::Zerg)
    {
        candidates.push_back(Opening::ForgeDouble);
        candidates.push_back(Opening::GateDoubleCorsairDark);
    }
    else
    {
        candidates.push_back(Opening::NakedDouble);
        candidates.push_back(Opening::RealFastDark);
        candidates.push_back(Opening::ForwardGateDark);
    }

    const int seed = static_cast<int>(std::time(nullptr)) ^
        Broodwar->getFrameCount() ^
        (mainTile_.x * 73856093) ^
        (mainTile_.y * 19349663);
    std::srand(seed);
    opening_ = candidates[std::rand() % candidates.size()];
}

void NeoProtossF::updateEnemyStart(Unit unit)
{
    if (!unit || !unit->exists() || !unit->getPlayer() || !Broodwar->self()->isEnemy(unit->getPlayer()))
    {
        return;
    }

    if (!unit->getType().isBuilding())
    {
        return;
    }

    enemyStartTile_ = unit->getTilePosition();
}

void NeoProtossF::manageWorkers()
{
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit || !unit->exists() || !unit->isCompleted())
        {
            continue;
        }

        if (unit->getType() == UnitTypes::Protoss_Nexus &&
            unit->isIdle() &&
            allCount(UnitTypes::Protoss_Probe) < workerTarget() &&
            canAfford(UnitTypes::Protoss_Probe))
        {
            unit->train(UnitTypes::Protoss_Probe);
        }
    }

    for (auto probe : Broodwar->self()->getUnits())
    {
        if (!probe || !probe->exists() || probe->getType() != UnitTypes::Protoss_Probe || !probe->isCompleted())
        {
            continue;
        }

        if (probe->isConstructing() || probe->isStuck() || probe->isLoaded())
        {
            continue;
        }

        if (defenseProbeIds_.find(probe->getID()) != defenseProbeIds_.end() &&
            Broodwar->getFrameCount() - lastDefenseFrame_ < 96)
        {
            continue;
        }

        Unit gas = nearestRefineryNeedingWorkers(probe->getPosition());
        if (gas && !probe->isCarryingMinerals() && !probe->isCarryingGas())
        {
            probe->gather(gas);
            continue;
        }

        if (probe->isIdle())
        {
            if (probe->isCarryingMinerals() || probe->isCarryingGas())
            {
                probe->returnCargo();
                continue;
            }

            Unit mineral = nearestMineral(probe->getPosition());
            if (mineral)
            {
                probe->gather(mineral);
            }
        }
    }
}

void NeoProtossF::manageScout()
{
    if (supplyUsed() < 12 || Broodwar->getFrameCount() - lastScoutFrame_ < 48)
    {
        return;
    }

    lastScoutFrame_ = Broodwar->getFrameCount();
    if (enemyStartTile_ != TilePositions::Invalid)
    {
        return;
    }

    if (!scout_ || !scout_->exists() || scout_->getType() != UnitTypes::Protoss_Probe)
    {
        scout_ = pickWorker(Position(mainTile_));
    }

    if (!scout_)
    {
        return;
    }

    std::vector<TilePosition> starts;
    for (auto start : Broodwar->getStartLocations())
    {
        if (start != mainTile_)
        {
            starts.push_back(start);
        }
    }

    if (starts.empty())
    {
        return;
    }

    int index = static_cast<int>(visitedStartIndices_.size() % starts.size());
    TilePosition target = starts[index];
    if (scout_->getDistance(Position(target)) < 160)
    {
        visitedStartIndices_.insert(index);
        index = static_cast<int>(visitedStartIndices_.size() % starts.size());
        target = starts[index];
    }

    scout_->move(Position(target));
}

bool NeoProtossF::handleEmergencyDefense()
{
    const Position mainPosition = Position(mainTile_);
    Unit threat = nearestEnemyThreat(mainPosition, 900);
    if (!threat && isValidTile(naturalTile_))
    {
        threat = nearestEnemyThreat(Position(naturalTile_), 650);
    }

    if (!threat)
    {
        releaseDefenseProbes();
        return false;
    }

    const bool workerHarass = threat->getType().isWorker();
    const int combatNearMain = combatUnitCountNear(mainPosition, 760);
    const int workerThreatCount = enemyThreatCountNear(mainPosition, 560, true);
    if (workerHarass && combatNearMain > 0)
    {
        releaseDefenseProbes();
    }

    if (workerHarass &&
        !threat->isAttacking() &&
        distance(threat->getPosition(), mainPosition) > 320)
    {
        releaseDefenseProbes();
        return false;
    }

    if (hasOrBuilding(UnitTypes::Protoss_Pylon) && hasOrBuilding(UnitTypes::Protoss_Gateway))
    {
        trainGatewayUnit(UnitTypes::Protoss_Zealot, 10);
    }

    if (hasCompleted(UnitTypes::Protoss_Forge) && hasCompleted(UnitTypes::Protoss_Pylon))
    {
        ensureBuilding(UnitTypes::Protoss_Photon_Cannon, 2, isValidTile(naturalTile_) ? naturalTile_ : mainTile_, 24);
    }

    int desiredProbes = 0;
    if (workerHarass)
    {
        desiredProbes = combatNearMain > 0 ? 0 : std::min(2, std::max(1, workerThreatCount));
    }
    else if (supplyUsed() < 28 && combatNearMain < 2)
    {
        desiredProbes = 3;
    }
    else if (combatNearMain == 0)
    {
        desiredProbes = 1;
    }

    std::vector<Unit> currentDefenseProbes;
    for (auto probe : Broodwar->self()->getUnits())
    {
        if (!probe ||
            !probe->exists() ||
            probe->getType() != UnitTypes::Protoss_Probe ||
            !probe->isCompleted() ||
            probe->isConstructing())
        {
            continue;
        }

        if (defenseProbeIds_.find(probe->getID()) != defenseProbeIds_.end())
        {
            if (distance(probe->getPosition(), mainPosition) > 760)
            {
                defenseProbeIds_.erase(probe->getID());
                Unit mineral = nearestMineral(probe->getPosition());
                if (mineral)
                {
                    probe->gather(mineral);
                }
                continue;
            }

            currentDefenseProbes.push_back(probe);
        }
    }

    for (auto probe : Broodwar->self()->getUnits())
    {
        if (static_cast<int>(currentDefenseProbes.size()) >= desiredProbes)
        {
            break;
        }

        if (!probe ||
            !probe->exists() ||
            probe->getType() != UnitTypes::Protoss_Probe ||
            !probe->isCompleted() ||
            probe->isConstructing() ||
            defenseProbeIds_.find(probe->getID()) != defenseProbeIds_.end())
        {
            continue;
        }

        currentDefenseProbes.push_back(probe);
        defenseProbeIds_.insert(probe->getID());
    }

    while (static_cast<int>(currentDefenseProbes.size()) > desiredProbes)
    {
        Unit probe = currentDefenseProbes.back();
        currentDefenseProbes.pop_back();
        defenseProbeIds_.erase(probe->getID());
        Unit mineral = nearestMineral(probe->getPosition());
        if (mineral)
        {
            probe->gather(mineral);
        }
    }

    if (Broodwar->getFrameCount() - lastDefenseFrame_ >= 24)
    {
        lastDefenseFrame_ = Broodwar->getFrameCount();
        for (auto probe : currentDefenseProbes)
        {
            if (probe && probe->exists())
            {
                probe->attack(threat);
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

void NeoProtossF::releaseDefenseProbes()
{
    if (defenseProbeIds_.empty())
    {
        return;
    }

    for (auto probe : Broodwar->self()->getUnits())
    {
        if (!probe ||
            !probe->exists() ||
            probe->getType() != UnitTypes::Protoss_Probe ||
            defenseProbeIds_.find(probe->getID()) == defenseProbeIds_.end())
        {
            continue;
        }

        if (probe->isCarryingMinerals() || probe->isCarryingGas())
        {
            probe->returnCargo();
            continue;
        }

        Unit mineral = nearestMineral(probe->getPosition());
        if (mineral)
        {
            probe->gather(mineral);
        }
    }

    defenseProbeIds_.clear();
}

void NeoProtossF::executeOpening()
{
    if (allCount(UnitTypes::Protoss_Pylon) > 0)
    {
        ensurePylon();
    }

    switch (opening_)
    {
    case Opening::TwoGate1012:
        executeTwoGate1012();
        break;
    case Opening::FastPowerDragoon:
        executeFastPowerDragoon();
        break;
    case Opening::Nexus23:
        executeNexus23();
        break;
    case Opening::Arbiter29:
        executeArbiter29();
        break;
    case Opening::ForgeDouble:
        executeForgeDouble();
        break;
    case Opening::GateDoubleCorsairDark:
        executeGateDoubleCorsairDark();
        break;
    case Opening::NakedDouble:
        executeNakedDouble();
        break;
    case Opening::RealFastDark:
        executeRealFastDark();
        break;
    case Opening::ForwardGateDark:
        executeForwardGateDark();
        break;
    }
}

void NeoProtossF::executeTwoGate1012()
{
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 10) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 12) ensureBuilding(UnitTypes::Protoss_Gateway, 2, mainTile_);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 21) ensureBuilding(UnitTypes::Protoss_Pylon, 3, mainTile_);
    if (supplyUsed() >= 22) ensureAssimilator(1);
    if (supplyUsed() >= 24) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 30) ensureBuilding(UnitTypes::Protoss_Gateway, 3, mainTile_);
}

void NeoProtossF::executeFastPowerDragoon()
{
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 10) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 11) ensureAssimilator(1);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 17) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 26) ensureBuilding(UnitTypes::Protoss_Gateway, 2, mainTile_);
    if (supplyUsed() >= 27) ensureBuilding(UnitTypes::Protoss_Forge, 1, mainTile_);
    if (supplyUsed() >= 39) ensureExpansion(2);
}

void NeoProtossF::executeNexus23()
{
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 10) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 12) ensureAssimilator(1);
    if (supplyUsed() >= 13) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 23) ensureExpansion(2);
    if (supplyUsed() >= 25) ensureBuilding(UnitTypes::Protoss_Robotics_Facility, 1, mainTile_);
    if (supplyUsed() >= 30) ensureBuilding(UnitTypes::Protoss_Gateway, 2, mainTile_);
    if (supplyUsed() >= 34) ensureBuilding(UnitTypes::Protoss_Citadel_of_Adun, 1, mainTile_);
    if (supplyUsed() >= 40) ensureBuilding(UnitTypes::Protoss_Gateway, 4, mainTile_);
    if (supplyUsed() >= 55) ensureBuilding(UnitTypes::Protoss_Templar_Archives, 1, mainTile_);
    if (supplyUsed() >= 70) ensureBuilding(UnitTypes::Protoss_Gateway, 8, mainTile_);
}

void NeoProtossF::executeArbiter29()
{
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 10) ensureAssimilator(1);
    if (supplyUsed() >= 11) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 14) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 16) ensureBuilding(UnitTypes::Protoss_Citadel_of_Adun, 1, mainTile_);
    if (supplyUsed() >= 22) ensureBuilding(UnitTypes::Protoss_Stargate, 1, mainTile_);
    if (supplyUsed() >= 22) ensureBuilding(UnitTypes::Protoss_Templar_Archives, 1, mainTile_);
    if (supplyUsed() >= 24) ensureExpansion(2);
    if (supplyUsed() >= 29) ensureBuilding(UnitTypes::Protoss_Arbiter_Tribunal, 1, mainTile_);
    if (supplyUsed() >= 40) ensureBuilding(UnitTypes::Protoss_Gateway, 4, mainTile_);
    if (supplyUsed() >= 52) ensureBuilding(UnitTypes::Protoss_Robotics_Facility, 1, mainTile_);
    if (supplyUsed() >= 60) ensureBuilding(UnitTypes::Protoss_Gateway, 7, mainTile_);
}

void NeoProtossF::executeForgeDouble()
{
    TilePosition wall = isValidTile(naturalTile_) ? naturalTile_ : mainTile_;
    if (!isValidTile(naturalTile_) && supplyUsed() >= 11)
    {
        naturalTile_ = findExpansionTile();
        wall = isValidTile(naturalTile_) ? naturalTile_ : mainTile_;
    }
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, wall, 20);
    if (supplyUsed() >= 11) ensureBuilding(UnitTypes::Protoss_Forge, 1, wall, 20);
    if (supplyUsed() >= 13) ensureBuilding(UnitTypes::Protoss_Photon_Cannon, 1, wall, 18);
    if (supplyUsed() >= 13) ensureExpansion(2);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Gateway, 1, wall, 24);
    if (supplyUsed() >= 16) ensureAssimilator(1);
    if (supplyUsed() >= 18) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 24) ensureBuilding(UnitTypes::Protoss_Citadel_of_Adun, 1, mainTile_);
    if (supplyUsed() >= 30) ensureAssimilator(2);
    if (supplyUsed() >= 40) ensureBuilding(UnitTypes::Protoss_Gateway, 4, mainTile_);
    if (supplyUsed() >= 50) ensureBuilding(UnitTypes::Protoss_Templar_Archives, 1, mainTile_);
}

void NeoProtossF::executeGateDoubleCorsairDark()
{
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 10) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 19) ensureExpansion(2);
    if (supplyUsed() >= 20) ensureBuilding(UnitTypes::Protoss_Pylon, 3, mainTile_);
    if (supplyUsed() >= 25) ensureBuilding(UnitTypes::Protoss_Forge, 1, mainTile_);
    if (supplyUsed() >= 26) ensureAssimilator(1);
    if (supplyUsed() >= 28) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 34) ensureBuilding(UnitTypes::Protoss_Stargate, 1, mainTile_);
    if (supplyUsed() >= 36) ensureBuilding(UnitTypes::Protoss_Citadel_of_Adun, 1, mainTile_);
    if (supplyUsed() >= 42) ensureBuilding(UnitTypes::Protoss_Gateway, 3, mainTile_);
    if (supplyUsed() >= 48) ensureBuilding(UnitTypes::Protoss_Templar_Archives, 1, mainTile_);
}

void NeoProtossF::executeNakedDouble()
{
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 13) ensureExpansion(2);
    if (supplyUsed() >= 14) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 15) ensureAssimilator(1);
    if (supplyUsed() >= 17) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 28) ensureBuilding(UnitTypes::Protoss_Robotics_Facility, 1, mainTile_);
    if (supplyUsed() >= 34) ensureBuilding(UnitTypes::Protoss_Gateway, 3, mainTile_);
    if (supplyUsed() >= 52) ensureExpansion(3);
}

void NeoProtossF::executeRealFastDark()
{
    if (supplyUsed() >= 7) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 10) ensureAssimilator(1);
    if (supplyUsed() >= 10) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 14) ensureBuilding(UnitTypes::Protoss_Citadel_of_Adun, 1, mainTile_);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Gateway, 2, mainTile_);
    if (supplyUsed() >= 16) ensureBuilding(UnitTypes::Protoss_Templar_Archives, 1, mainTile_);
    if (supplyUsed() >= 17) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 28) ensureExpansion(2);
}

void NeoProtossF::executeForwardGateDark()
{
    TilePosition forward = findForwardTile();
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 9) ensureBuilding(UnitTypes::Protoss_Gateway, 1, forward, 18);
    if (supplyUsed() >= 11) ensureAssimilator(1);
    if (supplyUsed() >= 12) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 14) ensureBuilding(UnitTypes::Protoss_Citadel_of_Adun, 1, mainTile_);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, forward, 18);
    if (supplyUsed() >= 16) ensureBuilding(UnitTypes::Protoss_Templar_Archives, 1, forward, 22);
    if (supplyUsed() >= 17) ensureBuilding(UnitTypes::Protoss_Gateway, 3, forward, 24);
}

void NeoProtossF::manageProduction()
{
    if (opening_ == Opening::Nexus23 && allCount(UnitTypes::Protoss_Nexus) < 2)
    {
        if (hasCompleted(UnitTypes::Protoss_Cybernetics_Core))
        {
            trainGatewayUnit(UnitTypes::Protoss_Dragoon, 2);
        }
        else
        {
            trainGatewayUnit(UnitTypes::Protoss_Zealot, 1);
        }

        return;
    }

    const bool darkPlan = opening_ == Opening::Arbiter29 ||
        opening_ == Opening::GateDoubleCorsairDark ||
        opening_ == Opening::RealFastDark ||
        opening_ == Opening::ForwardGateDark;
    const bool zealotPlan = opening_ == Opening::TwoGate1012 ||
        opening_ == Opening::ForgeDouble ||
        opening_ == Opening::GateDoubleCorsairDark ||
        opening_ == Opening::ForwardGateDark;

    if (darkPlan && hasCompleted(UnitTypes::Protoss_Templar_Archives))
    {
        trainGatewayUnit(UnitTypes::Protoss_Dark_Templar, 8);
    }

    if (!darkPlan && hasCompleted(UnitTypes::Protoss_Cybernetics_Core))
    {
        trainGatewayUnit(UnitTypes::Protoss_Dragoon, opening_ == Opening::TwoGate1012 ? 6 : 40);
    }

    if (zealotPlan || !hasCompleted(UnitTypes::Protoss_Cybernetics_Core))
    {
        trainGatewayUnit(UnitTypes::Protoss_Zealot, opening_ == Opening::TwoGate1012 ? 10 : 30);
    }

    if (opening_ == Opening::Arbiter29 && hasCompleted(UnitTypes::Protoss_Arbiter_Tribunal))
    {
        trainFromIdle(UnitTypes::Protoss_Stargate, UnitTypes::Protoss_Arbiter, 2);
    }
    else if (opening_ == Opening::GateDoubleCorsairDark)
    {
        trainFromIdle(UnitTypes::Protoss_Stargate, UnitTypes::Protoss_Corsair, 4);
    }

    if (hasCompleted(UnitTypes::Protoss_Robotics_Facility))
    {
        ensureBuilding(UnitTypes::Protoss_Observatory, 1, mainTile_);
    }

    if (hasCompleted(UnitTypes::Protoss_Observatory))
    {
        trainFromIdle(UnitTypes::Protoss_Robotics_Facility, UnitTypes::Protoss_Observer, 2);
    }
}

void NeoProtossF::manageResearch()
{
    if (hasCompleted(UnitTypes::Protoss_Cybernetics_Core))
    {
        upgradeFromIdle(UnitTypes::Protoss_Cybernetics_Core, UpgradeTypes::Singularity_Charge);
    }

    if (hasCompleted(UnitTypes::Protoss_Forge))
    {
        upgradeFromIdle(UnitTypes::Protoss_Forge, UpgradeTypes::Protoss_Ground_Weapons);
    }

    if (hasCompleted(UnitTypes::Protoss_Citadel_of_Adun))
    {
        upgradeFromIdle(UnitTypes::Protoss_Citadel_of_Adun, UpgradeTypes::Leg_Enhancements);
    }

    if (hasCompleted(UnitTypes::Protoss_Templar_Archives))
    {
        researchFromIdle(UnitTypes::Protoss_Templar_Archives, TechTypes::Psionic_Storm);
    }

    if (hasCompleted(UnitTypes::Protoss_Arbiter_Tribunal))
    {
        researchFromIdle(UnitTypes::Protoss_Arbiter_Tribunal, TechTypes::Stasis_Field);
    }
}

void NeoProtossF::manageCombat()
{
    const int frame = Broodwar->getFrameCount();
    if (frame - lastAttackFrame_ < 96)
    {
        return;
    }

    const int zealots = completedCount(UnitTypes::Protoss_Zealot);
    const int dragoons = completedCount(UnitTypes::Protoss_Dragoon);
    const int darks = completedCount(UnitTypes::Protoss_Dark_Templar);
    const int arbiters = completedCount(UnitTypes::Protoss_Arbiter);
    bool shouldAttack = false;

    if (opening_ == Opening::TwoGate1012)
    {
        shouldAttack = zealots >= 3 || frame > 24 * 280;
    }
    else if (opening_ == Opening::FastPowerDragoon)
    {
        shouldAttack = (zealots >= 2 && dragoons >= 5) || frame > 24 * 360;
    }
    else if (opening_ == Opening::RealFastDark || opening_ == Opening::ForwardGateDark)
    {
        shouldAttack = darks >= 2 || frame > 24 * 330;
    }
    else if (opening_ == Opening::Arbiter29)
    {
        shouldAttack = darks >= 2 || arbiters >= 1 || armyCount() >= 12;
    }
    else
    {
        shouldAttack = armyCount() >= 10 || supplyUsed() >= 120;
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

        if (unit->isStasised() || unit->isLockedDown() || unit->isLoaded())
        {
            continue;
        }

        unit->attack(target);
    }

    lastAttackFrame_ = frame;
}

void NeoProtossF::drawDebug() const
{
    if (Broodwar->getFrameCount() - lastDebugFrame_ < 12)
    {
        return;
    }

    Broodwar->drawTextScreen(8, 8, "NeoProtossF %s", openingName().c_str());
}

void NeoProtossF::maybeSendGg()
{
    if (ggSent_)
    {
        return;
    }

    const int frame = Broodwar->getFrameCount();
    const bool lostAllNexus = frame > 24 * 240 && completedCount(UnitTypes::Protoss_Nexus) == 0;
    const bool cannotFightOrProduce =
        frame > 24 * 300 &&
        armyCount() == 0 &&
        completedCount(UnitTypes::Protoss_Gateway) == 0 &&
        completedCount(UnitTypes::Protoss_Nexus) <= 1 &&
        allCount(UnitTypes::Protoss_Probe) <= 4;

    if (!lostAllNexus && !cannotFightOrProduce)
    {
        return;
    }

    Broodwar->sendText("gg");
    ggSent_ = true;
    ggFrame_ = frame;
}

int NeoProtossF::supplyUsed() const
{
    return Broodwar->self()->supplyUsed() / 2;
}

int NeoProtossF::supplyTotal() const
{
    return Broodwar->self()->supplyTotal() / 2;
}

int NeoProtossF::allCount(UnitType type) const
{
    return Broodwar->self()->allUnitCount(type);
}

int NeoProtossF::completedCount(UnitType type) const
{
    return Broodwar->self()->completedUnitCount(type);
}

int NeoProtossF::armyCount() const
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

int NeoProtossF::workerTarget() const
{
    const int nexuses = std::max(1, allCount(UnitTypes::Protoss_Nexus));
    return std::min(65, 22 + nexuses * 16);
}

bool NeoProtossF::hasOrBuilding(UnitType type, int count) const
{
    return allCount(type) >= count;
}

bool NeoProtossF::hasCompleted(UnitType type, int count) const
{
    return completedCount(type) >= count;
}

bool NeoProtossF::canAfford(UnitType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoProtossF::canAfford(UpgradeType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoProtossF::canAfford(TechType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoProtossF::ensurePylon()
{
    if (allCount(UnitTypes::Protoss_Pylon) == 0)
    {
        return ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    }

    if (supplyTotal() - supplyUsed() <= 4 &&
        allCount(UnitTypes::Protoss_Pylon) < 12 &&
        Broodwar->self()->incompleteUnitCount(UnitTypes::Protoss_Pylon) == 0)
    {
        return ensureBuilding(UnitTypes::Protoss_Pylon, allCount(UnitTypes::Protoss_Pylon) + 1, mainTile_);
    }

    return false;
}

bool NeoProtossF::ensureBuilding(UnitType type, int targetCount, TilePosition near, int maxRange)
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

bool NeoProtossF::ensureAssimilator(int targetCount)
{
    if (allCount(UnitTypes::Protoss_Assimilator) >= targetCount || !canAfford(UnitTypes::Protoss_Assimilator))
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

    if (worker->build(UnitTypes::Protoss_Assimilator, best->getTilePosition()))
    {
        lastBuildFrame_ = Broodwar->getFrameCount();
        return true;
    }

    return false;
}

bool NeoProtossF::ensureExpansion(int targetCount)
{
    if (allCount(UnitTypes::Protoss_Nexus) >= targetCount || !canAfford(UnitTypes::Protoss_Nexus))
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

    return ensureBuilding(UnitTypes::Protoss_Nexus, targetCount, tile, 16);
}

bool NeoProtossF::trainFromIdle(UnitType producer, UnitType unitType, int maxCount)
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

bool NeoProtossF::trainGatewayUnit(UnitType unitType, int maxCount)
{
    return trainFromIdle(UnitTypes::Protoss_Gateway, unitType, maxCount);
}

bool NeoProtossF::upgradeFromIdle(UnitType producer, UpgradeType upgradeType)
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

bool NeoProtossF::researchFromIdle(UnitType producer, TechType techType)
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

Unit NeoProtossF::pickWorker(Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Protoss_Probe ||
            !unit->isCompleted() ||
            unit->isConstructing() ||
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

Unit NeoProtossF::nearestOwned(UnitType type, Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit || !unit->exists() || unit->getType() != type)
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

Unit NeoProtossF::nearestEnemyThreat(Position near, int radius) const
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

int NeoProtossF::combatUnitCountNear(Position near, int radius) const
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

int NeoProtossF::enemyThreatCountNear(Position near, int radius, bool workersOnly) const
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

Unit NeoProtossF::nearestEnemyTarget() const
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

Unit NeoProtossF::nearestMineral(Position near) const
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

Unit NeoProtossF::nearestRefineryNeedingWorkers(Position near) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Protoss_Assimilator ||
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

TilePosition NeoProtossF::findExpansionTile() const
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

        TilePosition candidate = Broodwar->getBuildLocation(UnitTypes::Protoss_Nexus, mineral->getTilePosition(), 14, false);
        if (isValidTile(candidate))
        {
            bestDistance = d;
            best = candidate;
        }
    }

    return best;
}

TilePosition NeoProtossF::findForwardTile() const
{
    if (isValidTile(enemyStartTile_))
    {
        Position own = Position(mainTile_);
        Position enemy = Position(enemyStartTile_);
        Position mid((own.x + enemy.x) / 2, (own.y + enemy.y) / 2);
        return TilePosition(mid);
    }

    return TilePosition(Broodwar->mapWidth() / 2, Broodwar->mapHeight() / 2);
}

Position NeoProtossF::attackTarget() const
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

int NeoProtossF::gasWorkersFor(Unit refinery) const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (unit &&
            unit->exists() &&
            unit->getType() == UnitTypes::Protoss_Probe &&
            unit->getOrderTarget() == refinery)
        {
            ++count;
        }
    }

    return count;
}

double NeoProtossF::distance(Position a, Position b) const
{
    if (!a || !b)
    {
        return std::numeric_limits<double>::max();
    }

    const double dx = static_cast<double>(a.x - b.x);
    const double dy = static_cast<double>(a.y - b.y);
    return std::sqrt(dx * dx + dy * dy);
}

std::string NeoProtossF::configuredBuildId() const
{
    const char* candidates[] =
    {
        "bwapi-data\\AI\\Sparring\\Bots\\NeoProtossF\\sparring-bot.ini",
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

std::string NeoProtossF::openingName() const
{
    switch (opening_)
    {
    case Opening::TwoGate1012:
        return "1012 two gate";
    case Opening::FastPowerDragoon:
        return "fast power dragoon";
    case Opening::Nexus23:
        return "23 nexus";
    case Opening::Arbiter29:
        return "29 arbiter";
    case Opening::ForgeDouble:
        return "forge double";
    case Opening::GateDoubleCorsairDark:
        return "gate double corsair dark";
    case Opening::NakedDouble:
        return "naked double";
    case Opening::RealFastDark:
        return "fast dark";
    case Opening::ForwardGateDark:
        return "forward gate dark";
    }

    return "unknown";
}
