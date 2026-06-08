package delingvery;

import bwapi.*;

import bwta.BWTA;
import bwta.BaseLocation;

public class OnUnitDestroy extends DefaultBWListener {
	
	public static void runOnUnitDestroy(Unit unit) {
		
		//This does not work! Builder label remains on map, order remains as "Move".
		if (unit == Variables.builder) {
			Variables.builder = null;
			Variables.expanding = 0;
		}
		
		if (unit.getPlayer() == Delingvery.self) {
    		
    		if (unit.getType() == UnitType.Zerg_Spawning_Pool) {
    			Variables.havePool = 0;
    		}
    		
    		if (unit.getType() == UnitType.Zerg_Extractor) {
    			Variables.haveExtractor = 0;
    		}
    		
    		if (unit.getType() == UnitType.Zerg_Drone) {
    			Variables.wantDrones = Variables.wantDrones + 1;
    		}
    		
    		if (unit.getType() == UnitType.Zerg_Overlord) {
    			Variables.wantOverlords = Variables.wantOverlords + 1;
    			//if we do not know where any enemy bases are, the nearest spawn location must be where the enemy spawned
    			//THIS IS WORKING!!! (Just make sure we're actually RUNNING IT!)
    			if (Variables.enemyBasesSet.size() == 0) {
    				Delingvery.game.sendText("Overlord lost. Nearest spawn location must be enemy base");
    				BaseLocation nearestSpawnLocation = null;
    				Delingvery.game.sendText("trying to add nearest spawn location to enemyBasesSet");
    				for (BaseLocation spawnLocation : BWTA.getStartLocations()) {
    					if (nearestSpawnLocation == null || unit.getDistance(spawnLocation) < unit.getDistance(nearestSpawnLocation)) {
    						nearestSpawnLocation = spawnLocation;
    					}
    				}
    				
    				Position expectedBase = nearestSpawnLocation.getPosition();
    				Variables.enemyBasesSet.add(expectedBase);
    			}
    		}
    		
    		if (unit.getType() == UnitType.Zerg_Zergling) {
    			Variables.myUnitsSet.remove(unit);
    		}
    	}
    	
    	//remove enemy units from set
    	if (unit.getPlayer() == Delingvery.game.enemy()) {
    		if (unit.getType().isResourceDepot()) {
    			Variables.enemyBasesSet.remove(unit.getPosition());
    			//This thing seems to cause errors?
    			//Delingvery.game.sendText("Trying to remove enemy base from set");
    			//reset nearestEnemyBase so it is recalculated when an enemy base is destroyed
    			Variables.nearestEnemyBase = null;
    			if (Variables.enemyBasesSet.size() == 0) {
    				Variables.lastEnemyBase = unit.getPosition();
    			}
    		}
    		
    		else if (Variables.enemyUnitsSet.contains(unit)) {
    			Variables.enemyUnitsSet.remove(unit);
			}
    	}	
	}
}
