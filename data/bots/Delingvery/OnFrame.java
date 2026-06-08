package delingvery;

//import java.util.ArrayList;
//import java.util.List;

import bwapi.*;

import bwta.BWTA;
import bwta.BaseLocation;

import delingvery.Variables;

public class OnFrame extends DefaultBWListener {
	
	public static void runOnFrame(Mirror mirror, Game game, Player self) {
		
		//Display text
        game.drawTextScreen(10, 10, "Playing as " + self.getName() + " - " + self.getRace());
        game.drawTextScreen(10, 20, "I want to build " + Variables.wantDrones + " drones");
        game.drawTextScreen(10, 30, "I want to build " + Variables.wantOverlords + " overlords");
        game.drawTextScreen(10, 40, "I am building " + Variables.buildingOverlord + " overlords");
        game.drawTextScreen(10, 50, "havePool = " + Variables.havePool);
        game.drawTextScreen(10, 60, "haveExtractor = " + Variables.haveExtractor);
        game.drawTextScreen(10, 70, "expanding = " +Variables.expanding);
        game.drawTextScreen(10, 80, "wantGas = " + Variables.wantGas);
        game.drawTextScreen(10, 90, Variables.gasDrones + " drones are gathering gas");
        game.drawTextScreen(10, 100, "mainBase at " + Variables.mainBase);
        game.drawTextScreen(10, 110, "nearestEnemyBase at " + Variables.nearestEnemyBase);
        game.drawTextScreen(10, 120, "lastEnemyBase at " + Variables.lastEnemyBase);
        game.drawTextScreen(10, 130, "wantToAttack is " + Variables.wantToAttack);
        game.drawTextScreen(10, 140, "we have " + Variables.myUnitsSet.size() + " units");
        game.drawTextScreen(10, 150, "enemy has at least " + Variables.enemyUnitsSet.size() + " units");
        game.drawTextScreen(10, 160, "we know of " + Variables.enemyBasesSet.size() + " enemy bases");
        game.drawTextScreen(10, 170, "builder is " + Variables.builder);
        game.drawTextScreen(10, 180, "dronesThreatened = " + Variables.dronesThreatened);
        game.drawTextScreen(10, 190, "fightingDrones = " + Variables.fightingDrones);
        
        if (Variables.builder != null) {
        	game.drawTextScreen(10, 200, "builder order is " + Variables.builder.getOrder());
        	game.drawTextScreen(10, 210, "builder type is " + Variables.builder.getType());
        }
        else {
        	game.drawTextScreen(10, 200, "builder order is null");
        	game.drawTextScreen(10, 210, "builder type is null");
        }        
        
        if (Variables.builder != null) {
        	game.drawTextMap(Variables.builder.getPosition(), "builder");
        }
        
        if (Variables.expansionTarget != null) {
        	game.drawTextMap(Variables.expansionTarget.getPosition(), "expansionTarget");
        }
        
        if (Variables.mainBase != null) {
        	game.drawTextMap(Variables.mainBase.getPosition(), "mainBase");
        }
        
        if (Variables.nearestEnemyBase != null) {
        	game.drawTextMap(Variables.nearestEnemyBase, "nearestEnemyBase");
        }
        
        //lastEnemyBase was temporarily removed, has been returned, but needs proper testing.
        if (Variables.lastEnemyBase != null) {
        	game.drawTextMap(Variables.lastEnemyBase, "lastEnemyBase");
        }
        
        if (Variables.greeting == 0) {
        	game.sendText("gl hf!");
        	Variables.greeting = 1;
        }
        
    	if (self.supplyUsed() >= 33 && (self.supplyUsed() + 8) >= (self.supplyTotal() + (Variables.buildingOverlord + Variables.wantOverlords)*16)) {
    		Variables.wantOverlords = Variables.wantOverlords + 1;
    	}
    	
    	if (self.supplyTotal() < 200 && (self.minerals() - Variables.wantOverlords*150) > 450) {
    		Variables.wantOverlords = Variables.wantOverlords + 1;
    	}
    	
    	//We want to attack if we have a larger army (x2.5 as we only use lings and enemy will have stronger units)
    	if (Variables.myUnitsSet.size() > (Variables.enemyUnitsSet.size()*2.5)) {
    		Variables.wantToAttack = true;
    	}
    	else {
    		Variables.wantToAttack = false;
    	}
        
    	//set nearestEnemyBase to be at the position of the nearest enemy resource depot to our base
    	
    	if (Variables.nearestEnemyBase == null && Variables.enemyBasesSet.size() > 0) {
    		for (Position enemyBase : Variables.enemyBasesSet) {
    			//Changing the order of the || to check for null FIRST fixed the fatal exception access violation.
    			if (Variables.nearestEnemyBase == null  || Variables.mainBase.getDistance(enemyBase) < Variables.mainBase.getDistance(Variables.nearestEnemyBase)) {
    				Variables.nearestEnemyBase = enemyBase;
    				game.sendText("nearest base found");
    			}
    		}
    	}
        
        //Iterate through myUnitsSet. Also needs to be at the start of OnFrame()... weird AF.
        //Just make sure the units go to a sensible location.
        for (Unit armyUnit : Variables.myUnitsSet) {
        	
        	//Position target = new Position(500, 500);
    		//unit.move(target);
    		
    		Unit nearestEnemy = null;
    		Unit nearestEnemyBuilding = null;
    		
            for (Unit enemy : game.enemy().getUnits()) {
            	if (armyUnit.getDistance(enemy) < 480 && enemy.isVisible() && enemy.isFlying() != true) {
            		if (enemy.getType().isBuilding() == false && (enemy.getType() != UnitType.Zerg_Larva && enemy.getType() != UnitType.Zerg_Egg) || (enemy.getType() == UnitType.Zerg_Sunken_Colony) || (enemy.getType() == UnitType.Terran_Bunker) || (enemy.getType() == UnitType.Protoss_Photon_Cannon)) {
            			if (nearestEnemy == null || armyUnit.getDistance(enemy) < armyUnit.getDistance(nearestEnemy)) {
            				nearestEnemy = enemy;
            			}
            		}
            		
            		else {
            			if (nearestEnemyBuilding == null || armyUnit.getDistance(enemy) < armyUnit.getDistance(nearestEnemyBuilding)) {
            				nearestEnemyBuilding = enemy;
            			}
            		}
        		}
            }
    		
            //What is this retreating clusterfuck!? It needs rewriting to make sure "isRetreating" applies to the right
            //units and that they actually attack or retreat when they are supposed to.
            if (nearestEnemy != null && armyUnit.isAttacking() == false) {
            	armyUnit.attack(nearestEnemy);
            	//I tried armyUnit.attack(nearestEnemy.getPosition()); but lings tend to get stuck more);
            	
            	//We should not do this. It (might?) make our units constantly try to retreat
                /*
            	if (Variables.isRetreating.get(armyUnit) == true){
            		Variables.isRetreating.put(armyUnit, false);
            	}
            	*/
			}
 	
            else if (nearestEnemyBuilding != null && armyUnit.isAttacking() == false) {
            	armyUnit.attack(nearestEnemyBuilding);
            	if (Variables.isRetreating.get(armyUnit) == true){
            		Variables.isRetreating.put(armyUnit, false);
            	}
            }
            
            else if (Variables.wantToAttack == true && Variables.nearestEnemyBase != null && armyUnit.isIdle()) {
            	armyUnit.attack(Variables.nearestEnemyBase);
            	if (Variables.isRetreating.get(armyUnit) == true) {
            		Variables.isRetreating.put(armyUnit, false);
            	}
            }
        	
            //HAS NOT BEEN TESTED!!! Can't get my AI to kill an enemy base after the enemy expands.
            //It seems to work, from what I've seen
            else if (Variables.wantToAttack == true && Variables.nearestEnemyBase == null && Variables.lastEnemyBase != null && armyUnit.isIdle()) {
            	for (BaseLocation baseLocation : BWTA.getBaseLocations()) {
            		
        			TilePosition baseTile = baseLocation.getTilePosition();
        			
        			if (game.isVisible(baseTile) == false && baseLocation != Variables.expansionTarget && (Variables.lastEnemyBase.getDistance(baseLocation) < Variables.lastEnemyBase.getDistance(Variables.scoutTargetBase))) {       				
        				Variables.scoutTargetBase = baseLocation;
        			}
        		}
            	
				Position scoutPosition = Variables.scoutTargetBase.getPosition();
    			armyUnit.move(scoutPosition);
    			if (Variables.isRetreating.get(armyUnit) == true) {
    				Variables.isRetreating.put(armyUnit, false);
    			}
            }
            
            //can't use "armyUnit.getTargetPosition() != retreatPosition" as getTargetPosition() only applies to valid paths.
            //path will always be invalid, as the retreat position is underneath our hatcheries.
            else if (Variables.wantToAttack == false && Variables.isRetreating.get(armyUnit) == false) {
            	armyUnit.move(Variables.retreatPosition);
            	if (Variables.isRetreating.get(armyUnit) == false) {
            		Variables.isRetreating.put(armyUnit, true);
    			}
            }
        }
        
        if (Variables.builder != null) {
        	if (Variables.builder.getType() != UnitType.Zerg_Drone) {
        		Variables.builder = null;
        		Delingvery.game.sendText("Builder has been reset");
        	}
        }
        
        //iterate through my units
        for (Unit myUnit : self.getUnits()) {
        	
        	//Stop taking gas if we don't need it
        	//Added in self.gas() > 100 as a failsafe, as it seems to sometimes not work.
        	if (myUnit.getType() == UnitType.Zerg_Drone && (Variables.wantGas == false || self.gas() > 100) && myUnit.isGatheringGas() == true) {
        		Variables.wantGas = false;
        		Unit closestMineral = null;

    			//find the closest mineral
    			for (Unit neutralUnit : game.neutral().getUnits()) {
    				if (neutralUnit.getType().isMineralField()) {
    					if (closestMineral == null || myUnit.getDistance(neutralUnit) < myUnit.getDistance(closestMineral)) {
    						closestMineral = neutralUnit;
    					}
    				}
    			}
            
    			//if a mineral patch was found, send the worker to gather it
    			if (closestMineral != null) {
    				myUnit.gather(closestMineral, false);
    				Variables.gasDrones = 0;
    			}
        	}
        	
        	//Drone defend!
        	//reset dronesThreatened every frame;
        	Variables.dronesThreatened = false;
        	
        	if (myUnit.getType() == UnitType.Zerg_Drone) {
            	Unit nearestEnemy = null;
            	//Position enemyPosition = null;
            	
            	//Find the nearest enemy within 160 pixels
            	// OMFG I CAN JUST USE getUnitsInRadius() !!!
            	for (Unit enemy : myUnit.getUnitsInRadius(160)) {
            		if (enemy.isFlying() != true && enemy.getPlayer() == game.enemy()) {
            			Variables.dronesThreatened = true;
                		if (nearestEnemy == null || myUnit.getDistance(enemy) < myUnit.getDistance(nearestEnemy)) {
                			nearestEnemy = enemy;           			
                		}
            		}
            	}
            	
            	//if there is an enemy near us and drones are not already fighting, set fightingDrones to zero.
            	if (Variables.dronesThreatened == true && Variables.fightingDrones == -1) {
            		Variables.fightingDrones = 0;
            	}
            	
            	//if there is an enemy and we are not attacking it, attack it
            	if (nearestEnemy != null && myUnit.isAttacking() == false && Variables.fightingDrones != -1 && Variables.fightingDrones < Variables.enemyUnitsSet.size()*2) {
            		Position enemyPosition = nearestEnemy.getPosition();
            		myUnit.attack(enemyPosition);
            		Variables.fightingDrones = Variables.fightingDrones + 1;
    			}
                
            	//if there is no enemy, return to mining
            	if (Variables.dronesThreatened == false && Variables.fightingDrones != -1) {
                	Unit closestMineral = null;

                	//find the closest mineral
                	for (Unit neutralUnit : game.neutral().getUnits()) {
                		if (neutralUnit.getType().isMineralField()) {
                			if (closestMineral == null || myUnit.getDistance(neutralUnit) < myUnit.getDistance(closestMineral)) {
                				closestMineral = neutralUnit;
                			}
                		}
                	}
                
                	//if a mineral patch was found, send the worker to gather it
                	if (closestMineral != null) {
                		myUnit.gather(closestMineral, false);
                		Variables.fightingDrones = Variables.fightingDrones - 1;
                	}
                	
                	//if no drones are fighting (and drones are not threatened) set fightingDrones to -1
                	if (Variables.fightingDrones == 0) {
                		Variables.fightingDrones = -1;
                	}
            	}
            }
            
            //overlord scout if idle
        	if (myUnit.getType() == UnitType.Zerg_Overlord && myUnit.isIdle() == true && Variables.overlords < Variables.startPositions.size()) {	
    		
    			myUnit.move(Variables.startPositions.get(Variables.overlords));
    			Variables.overlords = Variables.overlords + 1;
            }
            
                 	
        	//TAKE EXPANSIONS WITH BUILDER     	
        	//Different builder logic
        	if (Variables.builder != null && Variables.builder.getOrder() != Order.Move && Variables.builder.getOrder() != Order.PlaceBuilding) {
        		Variables.builder.move(Variables.expansionTarget.getPosition(), false);
        		Variables.expanding = 1;
        	}
    		
            //Build an expansion!
        	if (self.minerals() >= 300 && Variables.expanding == 1) {
        		TilePosition buildTile = Variables.expansionTarget.getTilePosition();
        		if (game.isVisible(buildTile) == true) {
        			Variables.builder.build(UnitType.Zerg_Hatchery, buildTile);
        		}
        	}
        	
        	//get Zergling Speed
        	//Why the hell has changing the expansion logic to game.canBuildHere broken this!?
            if (myUnit.getType() == UnitType.Zerg_Spawning_Pool && self.gas() >= 100) {
        		if (Variables.wantGas == true) {
        			game.sendText("we don't want gas");
        			Variables.wantGas = false;
        		}

        		if (self.minerals() >= 100) {
        			myUnit.upgrade(UpgradeType.Metabolic_Boost);
        		}
        	}
        	
            //BUILDING MANAGER
            if (myUnit.getType() == UnitType.Zerg_Hatchery) {
            	
            	Unit nearestDrone = null;
            	Unit nearestLarva = null;
            	Unit nearestGeyser = null;
            	boolean droneFound = false;
            	
            	if (Variables.mainBase == null) {
            		Variables.mainBase = myUnit;
            	}
            	
            	//Find nearest larva
            	for (Unit getLarva : self.getUnits()) {
            		if (getLarva.getType() == UnitType.Zerg_Larva) {
            			 if (nearestLarva == null || myUnit.getDistance(getLarva) < myUnit.getDistance(nearestLarva)) {
                             nearestLarva = getLarva;
            			}	
            		}
            	}
            	
            	//THIS WORKS!!! I just need to use the hardcoded "Find nearest larva" rather than .getlarva().get(0)         	
            	
            	if (self.minerals() >=50) {
                	
            		if (Variables.wantOverlords >= 1 && self.minerals() >= 100) {
            			nearestLarva.morph(UnitType.Zerg_Overlord);
            			Variables.buildingOverlord = Variables.buildingOverlord + 1;
            			Variables.wantOverlords = Variables.wantOverlords - 1;
            			//trainingUnit = true;
            		}
            		
            		else if (Variables.wantDrones >= 1 && (self.supplyUsed() + 2) <= self.supplyTotal()) {
            			nearestLarva.morph(UnitType.Zerg_Drone);
            			Variables.wantDrones = Variables.wantDrones - 1;
            			//trainingUnit = true;
            		}
            		
            		else if (self.supplyUsed() + 2 <= self.supplyTotal()){
            			nearestLarva.morph(UnitType.Zerg_Zergling);
            			//trainingUnit = true;
            		}
            	}

            	//Find nearest drone
            	for (Unit getDrone : self.getUnits()) {
            		if (getDrone.getType() == UnitType.Zerg_Drone && getDrone.isGatheringGas() == false && getDrone != Variables.builder) {
            			 if (nearestDrone == null || myUnit.getDistance(getDrone) < myUnit.getDistance(nearestDrone)) {
                             nearestDrone = getDrone;
                             droneFound = true;
            			}	
            		}
            	}
            	
            	//Debug to show whether a drone can be found
            	if (droneFound == false) {
            		game.sendText("drone not found!");
            	}
            	
            	//EXPERIMENTAL builder assignment
            	if (Variables.builder == null && self.minerals() >= 240 && Variables.expanding == 0) {
            		game.sendText("trying to assign builder");
            		Variables.builder = nearestDrone;
            		nearestDrone = null;
            	}
            	
            	//Take gas if we need it
            	if (Variables.wantGas == true && Variables.gasDrones < 3 && nearestDrone != null) {
            		Unit nearestExtractor = null;
            		
            		//find extractor
    				for (Unit getExtractor : self.getUnits()) {
    					if (getExtractor.getType() == UnitType.Zerg_Extractor) {
    						if (nearestExtractor == null || myUnit.getDistance(getExtractor) < myUnit.getDistance(nearestExtractor)){
    							nearestExtractor = getExtractor;
    						}
    					}
    				}
            		nearestDrone.gather(nearestExtractor);
            		Variables.gasDrones = Variables.gasDrones + 1;
            	}
            	
            	//define build locations (find hatch co-ords, build next to it)
            	for (Unit Hatchery : self.getUnits()) {
            		if (Hatchery.getType() == UnitType.Zerg_Hatchery) {
            			int hatchX = (Hatchery.getTilePosition().getX());
            			int hatchY = (Hatchery.getTilePosition().getY());
            			
            			//check for pool
            			for (Unit poolSearch : self.getUnits()) {
            				if ((poolSearch.getType() == UnitType.Zerg_Spawning_Pool) && Variables.havePool < 2) {
            					Variables.havePool = 2;
            				}
            				//Build the pool. IT WORKS!!!
            				if (Variables.havePool == 0 && self.minerals() >= 200 && nearestDrone != null) {
            					if (game.canBuildHere(new TilePosition(hatchX,hatchY - 2), UnitType.Zerg_Spawning_Pool)) {
            						nearestDrone.build(UnitType.Zerg_Spawning_Pool, new TilePosition(hatchX,hatchY - 2));
            						Variables.havePool = 1;
            					}
            					else {
            						nearestDrone.build(UnitType.Zerg_Spawning_Pool, new TilePosition(hatchX,hatchY + 4));
            						Variables.havePool = 1;
            					}
            				}
            			}
            			
            			//find nearest geyser
        				for (Unit getGeyser : game.neutral().getUnits()) {
        					if (getGeyser.getType() == UnitType.Resource_Vespene_Geyser) {
        						if (nearestGeyser == null || myUnit.getDistance(getGeyser) < myUnit.getDistance(nearestGeyser)){
        							nearestGeyser = getGeyser;
        						}
        					}
        				}
        				
        				//Build the extractor
            			if (Variables.haveExtractor == 0 && Variables.havePool == 2 && self.minerals() >= 110 && nearestDrone != null) {
            				int GeyserX = (nearestGeyser.getTilePosition().getX());
            				int GeyserY = (nearestGeyser.getTilePosition().getY());
                			nearestDrone.build(UnitType.Zerg_Extractor, new TilePosition(GeyserX,GeyserY));
                			Variables.haveExtractor = 1;
            			}
            		}
            	}
            	
            	//Defines expansionTarget (nearest base which is buildable)
            	/*Do not use (BWTA.getGroundDistance(myUnit.getTilePosition(), baseLocation.getTilePosition()))
            	because on maps like andromeda and destination, the nearest base is blocked*/
            	if (Variables.expansionTarget == null) {
            		for (BaseLocation baseLocation : BWTA.getBaseLocations()) {
            			if (Variables.expansionTarget == null || myUnit.getDistance(baseLocation) < myUnit.getDistance(Variables.expansionTarget)){
            				TilePosition buildTile = baseLocation.getTilePosition();
            				if (game.canBuildHere(buildTile, UnitType.Zerg_Hatchery) == true && baseLocation.isIsland() == false) {
            					Variables.expansionTarget = baseLocation;
            				}
            			}
            		}
            	}
            	
            }
        }
    }	
}

