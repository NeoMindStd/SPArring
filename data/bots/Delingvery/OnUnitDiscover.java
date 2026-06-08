package delingvery;

import bwapi.*;

public class OnUnitDiscover extends DefaultBWListener {
	
	public static void runOnUnitDiscover(Unit unit) {
		
    	if (unit.getPlayer() == Delingvery.game.enemy()) {
    		//Add enemy units to Hashset
    		/*
    		if ((unit.getType().isBuilding() == true && unit.getType().canAttack() == true) || (unit.getType().isBuilding() == false && unit.getType().isWorker() == false)) {
    			if (!Variables.enemyUnitsSet.contains(unit)) {
    				Variables.enemyUnitsSet.add(unit);
    			}
    		}
    		*/
    		
    		
    		if (unit.getType().canAttack() && !unit.getType().isWorker()) {
    			if (!Variables.enemyUnitsSet.contains(unit)) {
    				Variables.enemyUnitsSet.add(unit);
    			}
    		}
    		
    		/*
    		if (unit.getWeaponType() != WeaponType.None && !unit.getType().isWorker()) {
    			if (!Variables.enemyUnitsSet.contains(unit)) {
    				Variables.enemyUnitsSet.add(unit);
    			}
    		}
    		*/
    		
    	
    		//Add enemy bases to Hashset
    		if (unit.getType().isResourceDepot()) {
    			Delingvery.game.sendText("base discovered");
    			Position basePosition = unit.getPosition();
				if (!Variables.enemyBasesSet.contains(basePosition)) {
					Variables.enemyBasesSet.add(basePosition);
    				Delingvery.game.sendText("base added to set");
    				//reset nearestEnemyBase (recalculate) and lastEnemyBase (do not need) when a new enemy base is found
    				Variables.nearestEnemyBase = null;
    				Variables.lastEnemyBase = null;
    			}
    		}
    	}
	}
}
