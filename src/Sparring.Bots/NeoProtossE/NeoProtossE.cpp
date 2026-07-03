#include "NeoProtossE.h"

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

    bool buildIdToOpening(const std::string& buildId, NeoProtossE::Opening& opening)
    {
        const std::string id = trimLower(buildId);
        if (id == "1012")
        {
            opening = NeoProtossE::Opening::TwoGate1012;
            return true;
        }
        if (id == "fast_power_dragoon")
        {
            opening = NeoProtossE::Opening::FastPowerDragoon;
            return true;
        }
        if (id == "23_nexus")
        {
            opening = NeoProtossE::Opening::Nexus23;
            return true;
        }
        if (id == "29_arbiter")
        {
            opening = NeoProtossE::Opening::Arbiter29;
            return true;
        }
        if (id == "forge_double")
        {
            opening = NeoProtossE::Opening::ForgeDouble;
            return true;
        }
        if (id == "gate_double_corsair_dark")
        {
            opening = NeoProtossE::Opening::GateDoubleCorsairDark;
            return true;
        }
        if (id == "naked_double")
        {
            opening = NeoProtossE::Opening::NakedDouble;
            return true;
        }
        if (id == "real_fast_dark")
        {
            opening = NeoProtossE::Opening::RealFastDark;
            return true;
        }
        if (id == "forward_gate_dark")
        {
            opening = NeoProtossE::Opening::ForwardGateDark;
            return true;
        }

        return false;
    }
}

void NeoProtossE::onStart()
{
    Broodwar->enableFlag(Flag::UserInput);
    Broodwar->setCommandOptimizationLevel(2);
    setInitialMapPositions();
    chooseOpening();
    loadPolicyModel();
}

void NeoProtossE::onEnd(bool isWinner)
{
    if (!isWinner && !ggSent_)
    {
        Broodwar->sendText("gg");
        ggSent_ = true;
        ggFrame_ = Broodwar->getFrameCount();
    }
}

void NeoProtossE::onFrame()
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

    const PolicyState policyState = capturePolicyState();
    const PolicyAction policyAction = evaluatePolicy(policyState);
    manageWorkers(policyAction);
    const bool emergency = handleEmergencyDefense();
    executeOpening();
    executePolicyAction(policyAction);
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

    if (emergency)
    {
        return;
    }

    return;
}

void NeoProtossE::onSendText(std::string text)
{
    Broodwar->sendText("%s", text.c_str());
}

void NeoProtossE::onReceiveText(Player, std::string)
{
}

void NeoProtossE::onPlayerLeft(Player)
{
}

void NeoProtossE::onNukeDetect(Position)
{
}

void NeoProtossE::onUnitDiscover(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoProtossE::onUnitEvade(Unit)
{
}

void NeoProtossE::onUnitShow(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoProtossE::onUnitHide(Unit)
{
}

void NeoProtossE::onUnitCreate(Unit)
{
}

void NeoProtossE::onUnitDestroy(Unit)
{
}

void NeoProtossE::onUnitMorph(Unit unit)
{
    updateEnemyStart(unit);
}

void NeoProtossE::onUnitRenegade(Unit)
{
}

void NeoProtossE::onSaveGame(std::string)
{
}

void NeoProtossE::onUnitComplete(Unit)
{
}

void NeoProtossE::setInitialMapPositions()
{
    mainTile_ = Broodwar->self()->getStartLocation();
    naturalTile_ = TilePositions::Invalid;
}

void NeoProtossE::chooseOpening()
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
    if (enemyRace == Races::Zerg)
    {
        candidates.push_back(Opening::ForgeDouble);
        candidates.push_back(Opening::GateDoubleCorsairDark);
    }
    else if (enemyRace == Races::Terran)
    {
        candidates.push_back(Opening::Nexus23);
    }

    const int seed = static_cast<int>(std::time(nullptr)) ^
        Broodwar->getFrameCount() ^
        (mainTile_.x * 73856093) ^
        (mainTile_.y * 19349663);
    std::srand(seed);
    opening_ = candidates[std::rand() % candidates.size()];
}

void NeoProtossE::updateEnemyStart(Unit unit)
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

void NeoProtossE::manageWorkers(const PolicyAction& action)
{
    int plannedProbes = allCount(UnitTypes::Protoss_Probe);
    const int queuedProbes = queuedProbeCount();
    plannedProbes += queuedProbes;

    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit || !unit->exists() || !unit->isCompleted())
        {
            continue;
        }

        if (unit->getType() == UnitTypes::Protoss_Nexus &&
            unit->getTrainingQueue().size() < 2 &&
            plannedProbes < action.WorkerTarget &&
            canAfford(UnitTypes::Protoss_Probe))
        {
            if (unit->train(UnitTypes::Protoss_Probe))
            {
                ++plannedProbes;
            }
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

        if (probe->isGatheringGas())
        {
            Unit gasTarget = probe->getTarget();
            if (!gasTarget ||
                !gasTarget->exists() ||
                gasTarget->getType() != UnitTypes::Protoss_Assimilator)
            {
                gasTarget = probe->getOrderTarget();
            }

            if (gasTarget &&
                gasTarget->exists() &&
                gasTarget->getType() == UnitTypes::Protoss_Assimilator &&
                gasWorkersFor(gasTarget) > gasWorkerTarget(gasTarget, action))
            {
                Unit mineral = nearestMineral(probe->getPosition());
                if (mineral)
                {
                    probe->gather(mineral);
                }
            }

            continue;
        }

        Unit gas = nearestRefineryNeedingWorkers(probe->getPosition(), action);
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

void NeoProtossE::manageScout()
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

bool NeoProtossE::handleEmergencyDefense()
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
    const bool closeWorkerHarass = workerHarass &&
        distance(threat->getPosition(), mainPosition) <= 240;
    const bool realWorkerHarass =
        workerHarass &&
        combatNearMain == 0 &&
        (threat->isAttacking() ||
            closeWorkerHarass ||
            workerThreatCount >= 2);
    if (workerHarass && combatNearMain > 0)
    {
        releaseDefenseProbes();
    }

    if (workerHarass && !realWorkerHarass)
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
            probe->isCarryingMinerals() ||
            probe->isCarryingGas() ||
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

void NeoProtossE::releaseDefenseProbes()
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

void NeoProtossE::executeOpening()
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

void NeoProtossE::executeTwoGate1012()
{
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 10) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 12) ensureBuilding(UnitTypes::Protoss_Gateway, 2, mainTile_);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 21) ensureBuilding(UnitTypes::Protoss_Pylon, 3, mainTile_);
    if (supplyUsed() >= 22) ensureAssimilator(1);
    if (supplyUsed() >= 24) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 30) ensureBuilding(UnitTypes::Protoss_Gateway, 3, mainTile_);
    if (supplyUsed() >= 42) ensureBuilding(UnitTypes::Protoss_Gateway, 4, mainTile_);
}

void NeoProtossE::executeFastPowerDragoon()
{
    if (supplyUsed() >= 8) ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    if (supplyUsed() >= 10) ensureBuilding(UnitTypes::Protoss_Gateway, 1, mainTile_);
    if (supplyUsed() >= 11) ensureAssimilator(1);
    if (supplyUsed() >= 15) ensureBuilding(UnitTypes::Protoss_Pylon, 2, mainTile_);
    if (supplyUsed() >= 17) ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    if (supplyUsed() >= 26) ensureBuilding(UnitTypes::Protoss_Gateway, 2, mainTile_);
    if (supplyUsed() >= 27) ensureBuilding(UnitTypes::Protoss_Forge, 1, mainTile_);
    if (supplyUsed() >= 39) ensureExpansion(2);
    if (supplyUsed() >= 46) ensureBuilding(UnitTypes::Protoss_Gateway, 4, mainTile_);
}

void NeoProtossE::executeNexus23()
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

void NeoProtossE::executeArbiter29()
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

void NeoProtossE::executeForgeDouble()
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
    if (supplyUsed() >= 38) ensureBuilding(UnitTypes::Protoss_Gateway, 5, mainTile_);
    if (supplyUsed() >= 50) ensureBuilding(UnitTypes::Protoss_Templar_Archives, 1, mainTile_);
}

void NeoProtossE::executeGateDoubleCorsairDark()
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
    if (supplyUsed() >= 42) ensureBuilding(UnitTypes::Protoss_Gateway, 4, mainTile_);
    if (supplyUsed() >= 48) ensureBuilding(UnitTypes::Protoss_Templar_Archives, 1, mainTile_);
}

void NeoProtossE::executeNakedDouble()
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

void NeoProtossE::executeRealFastDark()
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

void NeoProtossE::executeForwardGateDark()
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

void NeoProtossE::manageResearch()
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

void NeoProtossE::drawDebug() const
{
    if (Broodwar->getFrameCount() - lastDebugFrame_ < 12)
    {
        return;
    }

    Broodwar->drawTextScreen(8, 8, "NeoProtossE %s", openingName().c_str());
}

void NeoProtossE::maybeSendGg()
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

int NeoProtossE::supplyUsed() const
{
    return Broodwar->self()->supplyUsed() / 2;
}

int NeoProtossE::supplyTotal() const
{
    return Broodwar->self()->supplyTotal() / 2;
}

int NeoProtossE::allCount(UnitType type) const
{
    return Broodwar->self()->allUnitCount(type);
}

int NeoProtossE::completedCount(UnitType type) const
{
    return Broodwar->self()->completedUnitCount(type);
}

int NeoProtossE::armyCount() const
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

bool NeoProtossE::hasOrBuilding(UnitType type, int count) const
{
    return allCount(type) >= count;
}

bool NeoProtossE::hasCompleted(UnitType type, int count) const
{
    return completedCount(type) >= count;
}

bool NeoProtossE::canAfford(UnitType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoProtossE::canAfford(UpgradeType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

bool NeoProtossE::canAfford(TechType type) const
{
    return Broodwar->self()->minerals() >= type.mineralPrice() &&
        Broodwar->self()->gas() >= type.gasPrice();
}

void NeoProtossE::loadPolicyModel()
{
    if (policyLoaded_)
    {
        return;
    }

    policyLoaded_ = true;
    policyModel_.Outputs.clear();

    const char* candidates[] =
    {
        "bwapi-data\\AI\\Sparring\\Bots\\NeoProtossE\\neo-policy.tsv",
        "bwapi-data\\AI\\neo-policy.tsv"
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
            if (line.empty() || line[0] == '#')
            {
                continue;
            }

            std::istringstream stream(line);
            std::string output;
            stream >> output;
            std::vector<double> weights;
            double value = 0.0;
            while (stream >> value)
            {
                weights.push_back(value);
            }

            if (!output.empty() && !weights.empty())
            {
                policyModel_.Outputs[output] = weights;
            }
        }

        return;
    }
}

NeoProtossE::PolicyState NeoProtossE::capturePolicyState() const
{
    PolicyState state;
    state.Frame = Broodwar->getFrameCount();
    state.Supply = supplyUsed();
    state.Workers = allCount(UnitTypes::Protoss_Probe);
    state.Army = armyCount();
    state.Gateways = allCount(UnitTypes::Protoss_Gateway);
    state.Nexuses = allCount(UnitTypes::Protoss_Nexus);
    state.Minerals = Broodwar->self()->minerals();
    state.Gas = Broodwar->self()->gas();
    state.EnemyThreatsNearMain = enemyThreatCountNear(Position(mainTile_), 720, false);
    state.OpeningComplete = state.Supply >= 28 || state.Frame > 24 * 320;
    return state;
}

std::vector<double> NeoProtossE::policyFeatures(const PolicyState& state) const
{
    return
    {
        1.0,
        static_cast<double>(state.Frame) / 10000.0,
        static_cast<double>(state.Minerals) / 500.0,
        static_cast<double>(state.Gas) / 300.0,
        static_cast<double>(state.Supply) / 100.0,
        static_cast<double>(state.Workers) / 60.0,
        static_cast<double>(state.Army) / 30.0,
        static_cast<double>(state.Gateways) / 8.0,
        static_cast<double>(state.Nexuses) / 4.0,
        static_cast<double>(state.EnemyThreatsNearMain) / 8.0,
        state.OpeningComplete ? 1.0 : 0.0
    };
}

double NeoProtossE::scorePolicyOutput(const std::string& output, const std::vector<double>& features, const std::vector<double>& fallback) const
{
    const std::vector<double>* weights = &fallback;
    const auto found = policyModel_.Outputs.find(output);
    if (found != policyModel_.Outputs.end())
    {
        weights = &found->second;
    }

    double score = 0.0;
    const size_t count = std::min(weights->size(), features.size());
    for (size_t i = 0; i < count; ++i)
    {
        score += (*weights)[i] * features[i];
    }

    return score;
}

NeoProtossE::PolicyAction NeoProtossE::evaluatePolicy(const PolicyState& state) const
{
    const std::vector<double> features = policyFeatures(state);
    const std::string intents[] = { "intent_macro", "intent_defend", "intent_produce", "intent_expand", "intent_attack" };
    const std::vector<double> intentFallbacks[] =
    {
        { 0.0, 0.0, 0.5, 0.0, 0.2, 0.8, -0.2, 0.0, 0.0, -0.4, 0.0 },
        { -0.7, 0.0, 0.0, 0.0, 0.0, 0.0, -0.1, 0.0, 0.0, 4.2, 0.0 },
        { 0.3, 0.0, 0.6, 0.4, 0.3, 0.0, -0.25, 0.9, 0.0, 0.0, 0.5 },
        { -1.3, 0.35, 1.5, 0.0, 1.1, 0.4, 0.25, 0.0, -1.5, -2.0, 1.1 },
        { -1.0, 0.5, 0.0, 0.0, 0.6, 0.0, 2.0, 0.25, 0.0, -1.4, 1.3 }
    };

    int bestIndex = 0;
    double bestScore = scorePolicyOutput(intents[0], features, intentFallbacks[0]);
    for (int i = 1; i < 5; ++i)
    {
        const double score = scorePolicyOutput(intents[i], features, intentFallbacks[i]);
        if (score > bestScore)
        {
            bestScore = score;
            bestIndex = i;
        }
    }

    const auto countOutput = [&](const std::string& output, const std::vector<double>& fallback, int minimum, int maximum)
    {
        const double raw = scorePolicyOutput(output, features, fallback);
        const int rounded = static_cast<int>(std::floor(raw + 0.5));
        return std::max(minimum, std::min(maximum, rounded));
    };

    PolicyAction action;
    action.Intent =
        bestIndex == 1 ? PolicyIntent::Defend :
        bestIndex == 2 ? PolicyIntent::Produce :
        bestIndex == 3 ? PolicyIntent::Expand :
        bestIndex == 4 ? PolicyIntent::Attack :
        PolicyIntent::Macro;
    action.WorkerTarget = countOutput("worker_target", { 24.0, 5.5, 0.0, 0.0, 11.0, 24.0, -2.5, 0.0, 13.0, 0.0, 7.0 }, 20, 70);
    action.GasWorkersPerAssimilator = countOutput("gas_workers", { 3.0, 0.0, 0.0, -1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.7 }, 1, 3);
    action.NexusTarget = countOutput("nexus_target", { 1.0, 0.45, 0.9, 0.0, 1.3, 0.0, 0.15, 0.0, -0.9, -1.5, 1.1 }, 1, 4);
    action.GatewayTarget = countOutput("gateway_target", { 1.0, 0.0, 1.3, 0.0, 4.6, 0.0, 0.5, -0.3, 2.4, 0.0, 1.4 }, 1, 8);
    action.ZealotCap = countOutput("zealot_cap", { 2.0, 1.1, 0.0, 0.0, 9.0, 0.0, 0.7, 0.0, 0.0, 1.0, 2.5 }, 2, 34);
    action.DragoonCap = countOutput("dragoon_cap", { 0.0, 1.2, 0.0, 0.4, 16.0, 0.0, 1.1, 0.0, 0.0, 0.0, 9.0 }, 0, 48);
    action.DarkTemplarCap = countOutput("dark_templar_cap", { -2.0, 0.9, 0.0, 0.8, 7.0, 0.0, 0.0, 0.0, 0.0, -1.0, 3.5 }, 0, 10);
    action.ObserverCap = countOutput("observer_cap", { -1.0, 0.2, 0.0, 0.3, 2.2, 0.0, 0.0, 0.0, 0.0, 0.4, 1.0 }, 0, 3);
    action.StargateUnit = opening_ == Opening::Arbiter29 ? UnitTypes::Protoss_Arbiter : UnitTypes::Protoss_Corsair;
    action.StargateUnitCap = countOutput("stargate_unit_cap", { -1.0, 0.45, 0.0, 0.5, 2.2, 0.0, 0.2, 0.0, 0.0, -0.4, 1.2 }, 0, 5);
    action.AttackPressure = scorePolicyOutput("attack_pressure", features, { -0.6, 0.5, 0.0, 0.0, 0.6, 0.0, 1.9, 0.25, 0.0, -1.3, 1.3 });
    action.WorkerDefenders = countOutput("defense_workers", { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, -0.5, 0.0, 0.0, 2.0, 0.0 }, 0, 4);
    action.TakeExpansion = state.OpeningComplete && action.NexusTarget > state.Nexuses;
    action.Attack = state.OpeningComplete && action.AttackPressure > 0.65;

    if (action.Intent == PolicyIntent::Defend)
    {
        action.ZealotCap = std::max(action.ZealotCap, allCount(UnitTypes::Protoss_Zealot) + action.WorkerDefenders);
        action.Attack = false;
    }

    return action;
}

void NeoProtossE::executePolicyAction(const PolicyAction& action)
{
    if (action.TakeExpansion)
    {
        ensureExpansion(action.NexusTarget);
    }

    if (hasOrBuilding(UnitTypes::Protoss_Gateway))
    {
        trainGatewayUnit(UnitTypes::Protoss_Zealot, action.ZealotCap);
        if (hasCompleted(UnitTypes::Protoss_Cybernetics_Core))
        {
            trainGatewayUnit(UnitTypes::Protoss_Dragoon, action.DragoonCap);
        }
        if (hasCompleted(UnitTypes::Protoss_Templar_Archives))
        {
            trainGatewayUnit(UnitTypes::Protoss_Dark_Templar, action.DarkTemplarCap);
        }
    }

    if (hasOrBuilding(UnitTypes::Protoss_Gateway) && action.DragoonCap > 0 && !hasOrBuilding(UnitTypes::Protoss_Cybernetics_Core))
    {
        ensureBuilding(UnitTypes::Protoss_Cybernetics_Core, 1, mainTile_);
    }

    if (action.GatewayTarget > allCount(UnitTypes::Protoss_Gateway))
    {
        ensureBuilding(UnitTypes::Protoss_Gateway, action.GatewayTarget, mainTile_);
    }

    if (action.ObserverCap > 0 && hasCompleted(UnitTypes::Protoss_Robotics_Facility))
    {
        ensureBuilding(UnitTypes::Protoss_Observatory, 1, mainTile_);
    }

    if (action.ObserverCap > 0 && hasCompleted(UnitTypes::Protoss_Observatory))
    {
        trainFromIdle(UnitTypes::Protoss_Robotics_Facility, UnitTypes::Protoss_Observer, action.ObserverCap);
    }

    if (action.StargateUnitCap > 0 && hasCompleted(UnitTypes::Protoss_Stargate))
    {
        trainFromIdle(UnitTypes::Protoss_Stargate, action.StargateUnit, action.StargateUnitCap);
    }

    if (!action.Attack)
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

    lastAttackFrame_ = Broodwar->getFrameCount();
}

int NeoProtossE::queuedProbeCount() const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Protoss_Nexus)
        {
            continue;
        }

        for (auto queued : unit->getTrainingQueue())
        {
            if (queued == UnitTypes::Protoss_Probe)
            {
                ++count;
            }
        }
    }

    return count;
}

int NeoProtossE::pendingBuildCount(UnitType type) const
{
    int count = 0;
    const int frame = Broodwar->getFrameCount();
    for (const auto& entry : pendingBuilds_)
    {
        const PendingBuild& build = entry.second;
        if (build.Type == type &&
            frame - build.Frame <= 24 * 20 &&
            allCount(type) <= build.ObservedCount)
        {
            ++count;
        }
    }

    return count;
}

void NeoProtossE::rememberPendingBuild(UnitType type, int observedCount)
{
    PendingBuild build;
    build.Type = type;
    build.ObservedCount = observedCount;
    build.Frame = Broodwar->getFrameCount();
    pendingBuilds_[nextPendingBuildId_++] = build;
}

bool NeoProtossE::ensurePylon()
{
    if (allCount(UnitTypes::Protoss_Pylon) == 0)
    {
        return ensureBuilding(UnitTypes::Protoss_Pylon, 1, mainTile_);
    }

    if (supplyTotal() - supplyUsed() <= 6 &&
        allCount(UnitTypes::Protoss_Pylon) < 12 &&
        Broodwar->self()->incompleteUnitCount(UnitTypes::Protoss_Pylon) == 0)
    {
        return ensureBuilding(UnitTypes::Protoss_Pylon, allCount(UnitTypes::Protoss_Pylon) + 1, mainTile_);
    }

    return false;
}

bool NeoProtossE::ensureBuilding(UnitType type, int targetCount, TilePosition near, int maxRange)
{
    const int observedCount = allCount(type);
    if (observedCount + pendingBuildCount(type) >= targetCount || !canAfford(type))
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
        rememberPendingBuild(type, observedCount);
        return true;
    }

    return false;
}

bool NeoProtossE::ensureAssimilator(int targetCount)
{
    const int observedCount = allCount(UnitTypes::Protoss_Assimilator);
    if (observedCount + pendingBuildCount(UnitTypes::Protoss_Assimilator) >= targetCount ||
        !canAfford(UnitTypes::Protoss_Assimilator))
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
        rememberPendingBuild(UnitTypes::Protoss_Assimilator, observedCount);
        return true;
    }

    return false;
}

bool NeoProtossE::ensureExpansion(int targetCount)
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

bool NeoProtossE::trainFromIdle(UnitType producer, UnitType unitType, int maxCount)
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

bool NeoProtossE::trainGatewayUnit(UnitType unitType, int maxCount)
{
    return trainFromIdle(UnitTypes::Protoss_Gateway, unitType, maxCount);
}

bool NeoProtossE::upgradeFromIdle(UnitType producer, UpgradeType upgradeType)
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

bool NeoProtossE::researchFromIdle(UnitType producer, TechType techType)
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

Unit NeoProtossE::pickWorker(Position near) const
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
            unit->isStasised() ||
            defenseProbeIds_.find(unit->getID()) != defenseProbeIds_.end() ||
            (scout_ && scout_->exists() && scout_->getID() == unit->getID()) ||
            unit->isGatheringGas() ||
            unit->isCarryingMinerals() ||
            unit->isCarryingGas())
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

Unit NeoProtossE::nearestOwned(UnitType type, Position near) const
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

Unit NeoProtossE::nearestEnemyThreat(Position near, int radius) const
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

int NeoProtossE::combatUnitCountNear(Position near, int radius) const
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

int NeoProtossE::enemyThreatCountNear(Position near, int radius, bool workersOnly) const
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

Unit NeoProtossE::nearestEnemyTarget() const
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

Unit NeoProtossE::nearestMineral(Position near) const
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

Unit NeoProtossE::nearestRefineryNeedingWorkers(Position near, const PolicyAction& action) const
{
    Unit best = nullptr;
    double bestDistance = std::numeric_limits<double>::max();
    for (auto unit : Broodwar->self()->getUnits())
    {
        if (!unit ||
            !unit->exists() ||
            unit->getType() != UnitTypes::Protoss_Assimilator ||
            !unit->isCompleted() ||
            gasWorkersFor(unit) >= gasWorkerTarget(unit, action))
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

TilePosition NeoProtossE::findExpansionTile() const
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

TilePosition NeoProtossE::findForwardTile() const
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

Position NeoProtossE::attackTarget() const
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

int NeoProtossE::gasWorkerTarget(Unit refinery, const PolicyAction& action) const
{
    if (!refinery || !refinery->exists() || !refinery->isCompleted())
    {
        return 0;
    }

    return std::max(0, std::min(3, action.GasWorkersPerAssimilator));
}

int NeoProtossE::gasWorkersFor(Unit refinery) const
{
    int count = 0;
    for (auto unit : Broodwar->self()->getUnits())
    {
        Unit target = unit ? unit->getTarget() : nullptr;
        if (unit &&
            unit->exists() &&
            unit->getType() == UnitTypes::Protoss_Probe &&
            ((target &&
                target->getType() == UnitTypes::Protoss_Assimilator &&
                unit->getTarget() == refinery) ||
                unit->getOrderTarget() == refinery))
        {
            ++count;
        }
    }

    return count;
}

double NeoProtossE::distance(Position a, Position b) const
{
    if (!a || !b)
    {
        return std::numeric_limits<double>::max();
    }

    const double dx = static_cast<double>(a.x - b.x);
    const double dy = static_cast<double>(a.y - b.y);
    return std::sqrt(dx * dx + dy * dy);
}

std::string NeoProtossE::configuredBuildId() const
{
    const char* candidates[] =
    {
        "bwapi-data\\AI\\Sparring\\Bots\\NeoProtossE\\sparring-bot.ini",
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

std::string NeoProtossE::openingName() const
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
