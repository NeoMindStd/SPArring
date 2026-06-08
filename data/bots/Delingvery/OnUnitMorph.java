package delingvery;

import bwapi.*;

public class OnUnitMorph extends DefaultBWListener {
	
	public static void runOnUnitMorph(Unit unit) {
		
		//Does not work. Has been moved to OnFrame if Variables.builder.getType != UnitType.Zerg_Drone;
		/*
		if (unit == Variables.builder) {
			Variables.builder = null;
			Delingvery.game.sendText("Builder has been reset. Its type is " +unit.getType());
		}
		*/
		
		if (unit.getPlayer() == Delingvery.self) {
			if (unit.getType() == UnitType.Zerg_Hatchery) {
	    		Variables.expanding = 2;
	    		Variables.expansionTarget = null;
	    		Variables.wantDrones = Variables.wantDrones + 3;
	    		//Moved from OnUnitComplete so our expansion doesn't get sniped by rushes before completion.
	    		Variables.retreatPosition = unit.getPosition();
	    		//in case we go below 17 supply (often happens against terran if our first overlord goes the right way)
	    		Variables.wantOverlords = Variables.wantOverlords + 1;
	    	}
	    	
	    	//build an overlord after the pool is started
	    	if (unit.getType() == UnitType.Zerg_Spawning_Pool) { 
	    		Variables.wantOverlords = Variables.wantOverlords + 1;
	    	}
		}
	}
}
