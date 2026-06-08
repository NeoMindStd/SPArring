package delingvery;

import bwapi.*;

public class OnUnitComplete extends DefaultBWListener {
	
	public static void runOnUnitComplete(Unit unit) {
		if (unit.getPlayer() == Delingvery.self) {
    		
    		//When a hatchery is completed
    		if (unit.getType() == UnitType.Zerg_Hatchery) {
    			Variables.expanding = 0;
    		}
    		
    		//when the pool is created
    		if (unit.getType() == UnitType.Zerg_Spawning_Pool) {
    			Variables.havePool = 3;
    		}
    		
    		//when an extractor is created
    		if (unit.getType() == UnitType.Zerg_Extractor) {
    			Variables.haveExtractor = 3;
    			Variables.wantGas = true;
    		}
    		
    		//if it's a worker, send it to the closest mineral patch
    		if (unit.getType().isWorker()) {
    			Unit closestMineral = null;

    			//find the closest mineral
    			for (Unit neutralUnit : Delingvery.game.neutral().getUnits()) {
    				if (neutralUnit.getType().isMineralField()) {
    					if (closestMineral == null || unit.getDistance(neutralUnit) < unit.getDistance(closestMineral)) {
    						closestMineral = neutralUnit;
    					}
    				}
    			}
            
    			//if a mineral patch was found, send the worker to gather it
    			if (closestMineral != null) {
    				unit.gather(closestMineral, false);
    			}
    		}
        
    		//find possible spawn locations (IT WORKS!!!)
    		//Would be nice to add a check that prevents it scouting its own base
        
    		if (unit.getType() == UnitType.Zerg_Overlord) {
    			Variables.buildingOverlord = Variables.buildingOverlord - 1;
    		
    			unit.move(Variables.startPositions.get(Variables.overlords));
    			Variables.overlords = Variables.overlords + 1;
    			
    			//Add overlords to army Hashset if we have free supply in the late game (for detection)
    			if (Delingvery.self.supplyTotal() >= 100 && (Delingvery.self.supplyTotal() >= Delingvery.self.supplyUsed() + 30)){
    				if (!Variables.myUnitsSet.contains(unit)) {
        				Variables.myUnitsSet.add(unit);
        			}
    				Variables.wantOverlords = Variables.wantOverlords + 1;
    				Delingvery.game.sendText("Sending overlord with army");
    			}
        	}
    		
    		if (unit.getType() == UnitType.Zerg_Zergling) {
    			
    			//Add Zerglings to Hashset
    			if (!Variables.myUnitsSet.contains(unit)) {
    				Variables.myUnitsSet.add(unit);
    			}
    			
    			//Set isRetreating to false
    			Variables.isRetreating.put(unit, false);
    		}
        }
	}
}
