package delingvery;
/*import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.HashMap;
import java.util.Map;
*/

import bwapi.*;

/*
import bwta.BWTA;
import bwta.BaseLocation;
*/

public class Delingvery extends DefaultBWListener {
	
	//these 3 were originally private and not static
    public static Mirror mirror = new Mirror();

    public static Game game;

    public static Player self;
    
    public void run() {
        mirror.getModule().setEventListener(this);
        mirror.startGame();
    }
    
    /*
    //PUBLIC VARIABLES
    
    //which overlord is this? (for scouting purposes)
    public static int overlords = 0;
    
    //is an overlord being built? Starts at 1 to account for the overlord we start with being "completed".
    public static int buildingOverlord = 1;
    
    //have we started training a unit this frame?
    //public static boolean trainingUnit = false;
    
    // 0 = no pool, 1 = ordered to build pool, 2 = pool has begun, 3 = pool is complete
    public static int havePool = 0;
    
    // 0 = no extractor, 1 = ordered to build extractor, 2 = extractor has begun, 3 = extractor is complete
    public static int haveExtractor = 0;
    
    // true = harvest more gas, false = move gas drones back to minerals
    public static boolean wantGas = false;
    
    // number of drones gathering gas
    public static int gasDrones = 0;
    
    // number of drones we want to build. Start at 6 because we want to have 9 (minus 1 for the pool)
    public static int wantDrones = 6;
    
    // number of overlords we want to build.
    public static int wantOverlords = 0;
    
    //0 = no expansion, 1 = moving to expansion, 2 = ordered to build expansion
    public static int expanding = 0;
    
    //0 = have not sent greeting, 1 = have sent greeting
    public static int greeting = 0;
    
    // declares “expansion” base location
    public static BaseLocation expansionTarget;
    
    // declares "builder" unit for expansions
    public static Unit builder;
    
    // do we want to attack? Base this on enemyUnitsSet compared to myUnitsSet
    public static boolean wantToAttack = false;
    
    // declares position to retreat to if wantToAttack is false
    public static Position retreatPosition = null;
    
    // create HashSet for my units
    public static HashSet<Unit> myUnitsSet = new HashSet<Unit>();
    
    // create HashSet for enemy units
    public static HashSet<Unit> enemyUnitsSet = new HashSet<Unit>();
    
    // create HashSet for enemy bases
    public static HashSet<Position> enemyBasesSet = new HashSet<Position>();
    
    // declares position of nearest enemy base
    public static Position nearestEnemyBase = null;
    
    // declares our start position
    public static Unit mainBase = null;
    
    // declares position of destroyed enemy base if no other bases are known
    public static Position lastEnemyBase = null;
    
    // declares base to try to scout
    public static BaseLocation scoutTargetBase = null;
    
    // declares Map Interface to show whether a unit is retreating or attacking
    public static Map<Unit, Boolean> isRetreating = new HashMap<Unit, Boolean>();
    
    */
    
    @Override
    public void onStart() {
    	OnStart.runOnStart();
    }
    
    /*
    @Override
    public void onStart() {
        game = mirror.getGame();
        self = game.self();
        
        //set speed to ladder speed (20). Lower is faster.
        game.setLocalSpeed(5);

        //Use BWTA to analyze map
        //This may take a few minutes if the map is processed first time!
        BWTA.readMap();
        BWTA.analyze();
    }
    */
    
    @Override 
    public void onFrame(){
    	OnFrame.runOnFrame(mirror, game, self);
    }
    
    /*
    @Override
    public void onFrame() {
     	
    	//Display text
        game.drawTextScreen(10, 10, "Playing as " + self.getName() + " - " + self.getRace());
        game.drawTextScreen(10, 20, "I want to build " + wantDrones + " drones");
        game.drawTextScreen(10, 30, "I want to build " + wantOverlords + " overlords");
        game.drawTextScreen(10, 40, "I am building " + buildingOverlord + " overlords");
        game.drawTextScreen(10, 50, "havePool = " + havePool);
        game.drawTextScreen(10, 60, "haveExtractor = " + haveExtractor);
        game.drawTextScreen(10, 70, "expanding = " +expanding);
        game.drawTextScreen(10, 80, "wantGas = " + wantGas);
        game.drawTextScreen(10, 90, gasDrones + " drones are gathering gas");
        game.drawTextScreen(10, 100, "mainBase at " + mainBase);
        game.drawTextScreen(10, 110, "nearestEnemyBase at " + nearestEnemyBase);
        game.drawTextScreen(10, 120, "lastEnemyBase at " + lastEnemyBase);
        game.drawTextScreen(10, 130, "wantToAttack is " + wantToAttack);
        game.drawTextScreen(10, 140, "we have " + myUnitsSet.size() + " units");
        game.drawTextScreen(10, 150, "enemy has at least " + enemyUnitsSet.size() + " units");
        game.drawTextScreen(10, 160, "we know of " + enemyBasesSet.size() + " enemy bases");
        game.drawTextScreen(10, 170, "builder is " + builder);
        if (builder != null) {
        	game.drawTextScreen(10, 180, "builder order is " + builder.getOrder());
        }
        
        
        if (builder != null) {
        	game.drawTextMap(builder.getPosition(), "builder");
        }
        
        if (expansionTarget != null) {
        	game.drawTextMap(expansionTarget.getPosition(), "expansionTarget");
        }
        
        if (mainBase != null) {
        	game.drawTextMap(mainBase.getPosition(), "mainBase");
        }
        
        if (nearestEnemyBase != null) {
        	game.drawTextMap(nearestEnemyBase, "nearestEnemyBase");
        }
        
        //temporarily removed
        if (lastEnemyBase != null) {
        	game.drawTextMap(lastEnemyBase, "lastEnemyBase");
        }
        
        if (greeting == 0) {
        	game.sendText("gl hf!");
        	greeting = 1;
        }
        
    	if (self.supplyUsed() >= 33 && (self.supplyUsed() + 8) >= (self.supplyTotal() + (buildingOverlord + wantOverlords)*16)) {
    		wantOverlords = wantOverlords + 1;
    	}
    	
    	if (self.supplyTotal() < 200 && (self.minerals() - wantOverlords*150) > 450) {
    		wantOverlords = wantOverlords + 1;
    	}
    	
    	//We want to attack if we have a larger army (x3 as we only use lings and enemy will have stronger units)
    	if (myUnitsSet.size() > (enemyUnitsSet.size()*3)) {
    		wantToAttack = true;
    	}
    	else {
    		wantToAttack = false;
    	}
        
    	//set nearestEnemyBase to be at the position of the nearest enemy resource depot to our base
    	
    	if (nearestEnemyBase == null && enemyBasesSet.size() > 0) {
    		for (Position enemyBase : enemyBasesSet) {
    			//Changing the order of the || to check for null FIRST fixed the fatal exception access violation.
    			if (nearestEnemyBase == null  || mainBase.getDistance(enemyBase) < mainBase.getDistance(nearestEnemyBase)) {
    				nearestEnemyBase = enemyBase;
    				game.sendText("nearest base found");
    			}
    		}
    	}
        
        //Iterate through myUnitsSet. Also needs to be at the start of OnFrame()... weird AF.
        //Just make sure the units go to a sensible location.
        for (Unit armyUnit : myUnitsSet) {
        	
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
    		
            if (nearestEnemy != null && armyUnit.isAttacking() == false) {
            	armyUnit.attack(nearestEnemy);
            	if (isRetreating.get(armyUnit) == true){
            		isRetreating.put(armyUnit, false);
            	}
			}
        	
            else if (nearestEnemyBuilding != null && armyUnit.isAttacking() == false) {
            	armyUnit.attack(nearestEnemyBuilding);
            	if (isRetreating.get(armyUnit) == true){
            		isRetreating.put(armyUnit, false);
            	}
            }
            
            else if (wantToAttack == true && nearestEnemyBase != null && armyUnit.isIdle()) {
            	armyUnit.attack(nearestEnemyBase);
            	if (isRetreating.get(armyUnit) == true) {
            		isRetreating.put(armyUnit, false);
            	}
            }
        	
            //HAS NOT BEEN TESTED!!! Can't get my AI to kill an enemy base after the enemy expands.
            else if (wantToAttack == true && nearestEnemyBase == null && lastEnemyBase != null && armyUnit.isIdle()) {
            	for (BaseLocation baseLocation : BWTA.getBaseLocations()) {
            		
        			TilePosition baseTile = baseLocation.getTilePosition();
        			
        			if (game.isVisible(baseTile) == false && baseLocation != expansionTarget && (lastEnemyBase.getDistance(baseLocation) < lastEnemyBase.getDistance(scoutTargetBase))) {       				
        				scoutTargetBase = baseLocation;
        			}
        		}
            	
				Position scoutPosition = scoutTargetBase.getPosition();
    			armyUnit.move(scoutPosition);
    			if (isRetreating.get(armyUnit) == true) {
    				isRetreating.put(armyUnit, false);
    			}
            }
            
            /*
            //This logic doesn't work very well. However, trying to find the nearest base causes the whole bot to halt.
        	else if (nearestEnemyBase == null && armyUnit.isIdle() == true) {
        		for (BaseLocation baseLocation : BWTA.getBaseLocations()) {
        			TilePosition baseTile = baseLocation.getTilePosition();
        			if (game.isVisible(baseTile) == false && baseLocation != expansionTarget) {       				
        				Position scoutPosition = baseLocation.getPosition();
            			armyUnit.attack(scoutPosition, true);
        			}
        		}	
        	}
            
            //can't use "armyUnit.getTargetPosition() != retreatPosition" as getTargetPosition() only applies to valid paths.
            //path will always be invalid, as the retreat position is underneath our hatcheries.
            else if (wantToAttack == false && isRetreating.get(armyUnit) == false) {
            	armyUnit.move(retreatPosition);
            	if (isRetreating.get(armyUnit) == false) {
    				isRetreating.put(armyUnit, true);
    			}
            }
        }
        
        //iterate through ENEMY units. Needs to be at start of OnFrame() or will not work. I have no idea why this is.
        /*for (Unit enemyUnit : game.enemy().getUnits()) {
     	   
     	   if (enemyUnit.getType().isResourceDepot() && nearestEnemyBase == null) {
     		   game.sendText("enemy base found!");
     		   nearestEnemyBase = enemyUnit.getPosition();
     	   }
        }
        
        //iterate through my units
        for (Unit myUnit : self.getUnits()) {
        	
        	//Assign builder outside of building manager
        	//Having this at the start of the iteration makes it more reliable.
        	/*if (myUnit.getType() == UnitType.Zerg_Drone && builder == null && havePool >= 2) {
        		game.sendText("trying to find builder");
        		if (expanding == 0 && self.minerals() >= 240 && myUnit.isGatheringGas() == false ) {
                	builder = myUnit;
                	game.sendText("trying to assign builder");
        		}
   			}
        	
        	//Stop taking gas if we don't need it
        	//Added in self.gas() > 100 as a failsafe, as it seems to sometimes not work.
        	if (myUnit.getType() == UnitType.Zerg_Drone && (wantGas == false || self.gas() > 100) && myUnit.isGatheringGas() == true) {
        		wantGas = false;
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
    				gasDrones = 0;
    			}
        	}
        	
        	//Drone defend!
        	if (myUnit.getType() == UnitType.Zerg_Drone && myUnit.isAttackFrame() == false && myUnit.getHitPoints() < 40) {
            	Unit nearestEnemy = null;
            	//Position enemyPosition = null;
            	
            	//Find the nearest enemy within 160 pixels
            	for (Unit enemy : game.enemy().getUnits()) {
            		if (myUnit.getDistance(enemy) < 160 && enemy.isFlying() != true && (enemy.isAttacking() == true)) {
                		if (nearestEnemy == null || myUnit.getDistance(enemy) < myUnit.getDistance(nearestEnemy)) {
                			nearestEnemy = enemy;
                		}
            		}
            	}
            	
            	//if there is an enemy and we are not attacking it, attack it
            	if (nearestEnemy != null && myUnit.isAttacking() == false) {
            		Position enemyPosition = nearestEnemy.getPosition();
            		myUnit.attack(enemyPosition);
    			}
                
            	//if there is no enemy and we are trying to attack, return to mining
            	else if (nearestEnemy == null && myUnit.isAttacking() == true) {
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
                	}
            	}
            }
        	
        	/*
        	//Combined drone logic
        	if (myUnit.getType() == UnitType.Zerg_Drone) {
        		//Assign builder if we want to expand
        		if (builder == null && havePool >= 2 && expanding == 0 && self.minerals() >= 240 && myUnit.isGatheringGas() == false) {
        			game.sendText("Trying to assign builder");
        			builder = myUnit;
        		}
        		
        		
        	}
            
            //overlord scout if idle
        	if (myUnit.getType() == UnitType.Zerg_Overlord && myUnit.isIdle() == true) {
            	List<Position> positions = new ArrayList<Position>();	
    			int i = 0;
    			for (BaseLocation spawnLocation : BWTA.getStartLocations()) {
    				positions.add(spawnLocation.getPosition());	
    				i = i + 1;
    			}
    		
    			myUnit.move(positions.get(overlords));
    			overlords = overlords + 1;
            }
            
            
        	
        	//TAKE EXPANSIONS WITH BUILDER
        	//move builder to expansionTarget
    		/*if (builder != null && expansionTarget != null && expanding == 0) {
    			builder.stop();
    			builder.move(expansionTarget.getPosition(), true);
    			expanding = 1;
    		}
    		
    		//failsafe for if builder fails to move to expansionTarget
    		if (expanding == 1 && (builder.isIdle() || builder.isGatheringMinerals()) && builder.getPosition() != expansionTarget.getPosition()) {
    			TilePosition buildTile = expansionTarget.getTilePosition();
    			builder.build(UnitType.Zerg_Hatchery, buildTile);
    		}
        	
        	//Different builder logic
        	if (builder != null && builder.getOrder() != Order.Move && builder.getOrder() != Order.PlaceBuilding) {
        		builder.move(expansionTarget.getPosition(), false);
        		expanding = 1;
        	}
    		
            //Build an expansion!
        	if (self.minerals() >= 300 && expanding == 1) {
        		TilePosition buildTile = expansionTarget.getTilePosition();
        		if (game.isVisible(buildTile) == true) {
        			builder.build(UnitType.Zerg_Hatchery, buildTile);
        		}
        	}
        	
        	//get Zergling Speed
        	//Why the hell has changing the expansion logic to game.canBuildHere broken this!?
            if (myUnit.getType() == UnitType.Zerg_Spawning_Pool && self.gas() >= 100) {
        		if (wantGas == true) {
        			game.sendText("we don't want gas");
        			wantGas = false;
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
            	
            	if (mainBase == null) {
            		mainBase = myUnit;
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
                	
            		if (wantOverlords >= 1 && self.minerals() >= 100) {
            			nearestLarva.morph(UnitType.Zerg_Overlord);
            			buildingOverlord = buildingOverlord + 1;
            			wantOverlords = wantOverlords - 1;
            			//trainingUnit = true;
            		}
            		
            		else if (wantDrones >= 1 && (self.supplyUsed() + 2) <= self.supplyTotal()) {
            			nearestLarva.morph(UnitType.Zerg_Drone);
            			wantDrones = wantDrones - 1;
            			//trainingUnit = true;
            		}
            		
            		else if (self.supplyUsed() + 2 <= self.supplyTotal()){
            			nearestLarva.morph(UnitType.Zerg_Zergling);
            			//trainingUnit = true;
            		}
            	}

            	//Find nearest drone
            	for (Unit getDrone : self.getUnits()) {
            		if (getDrone.getType() == UnitType.Zerg_Drone && getDrone.isGatheringGas() == false && getDrone != builder) {
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
            	if (builder == null && self.minerals() >= 240 && expanding == 0) {
            		game.sendText("trying to assign builder");
            		builder = nearestDrone;
            		nearestDrone = null;
            	}
            	
            	//Take gas if we need it
            	if (wantGas == true && gasDrones < 3 && nearestDrone != null) {
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
            		gasDrones = gasDrones + 1;
            	}
            	
            	//define build locations (find hatch co-ords, build next to it)
            	for (Unit Hatchery : self.getUnits()) {
            		if (Hatchery.getType() == UnitType.Zerg_Hatchery) {
            			int hatchX = (Hatchery.getTilePosition().getX());
            			int hatchY = (Hatchery.getTilePosition().getY());
            			
            			//check for pool
            			for (Unit poolSearch : self.getUnits()) {
            				if ((poolSearch.getType() == UnitType.Zerg_Spawning_Pool) && havePool < 2) {
            					havePool = 2;
            				}
            				//Build the pool. IT WORKS!!!
            				if (havePool == 0 && self.minerals() >= 200 && nearestDrone != null) {
            					if (game.canBuildHere(new TilePosition(hatchX,hatchY - 2), UnitType.Zerg_Spawning_Pool)) {
            						nearestDrone.build(UnitType.Zerg_Spawning_Pool, new TilePosition(hatchX,hatchY - 2));
            						havePool = 1;
            					}
            					else {
            						nearestDrone.build(UnitType.Zerg_Spawning_Pool, new TilePosition(hatchX,hatchY + 4));
            						havePool = 1;
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
            			if (haveExtractor == 0 && havePool == 2 && self.minerals() >= 110 && nearestDrone != null) {
            				int GeyserX = (nearestGeyser.getTilePosition().getX());
            				int GeyserY = (nearestGeyser.getTilePosition().getY());
                			nearestDrone.build(UnitType.Zerg_Extractor, new TilePosition(GeyserX,GeyserY));
                			haveExtractor = 1;
            			}
            		}
            	}
            	
            	//Defines expansionTarget (nearest base which is buildable)
            	/*Do not use (BWTA.getGroundDistance(myUnit.getTilePosition(), baseLocation.getTilePosition()))
            	because on maps like andromeda and destination, the nearest base is blocked
            	if (expansionTarget == null) {
            		for (BaseLocation baseLocation : BWTA.getBaseLocations()) {
            			if (expansionTarget == null || myUnit.getDistance(baseLocation) < myUnit.getDistance(expansionTarget)){
            				TilePosition buildTile = baseLocation.getTilePosition();
            				if (game.canBuildHere(buildTile, UnitType.Zerg_Hatchery) == true && baseLocation.isIsland() == false) {
            					expansionTarget = baseLocation;
            				}
            			}
            		}
            	}
            }
            
           
        }
    }
    */
    
    @Override
    public void onUnitMorph(Unit unit) {
    	OnUnitMorph.runOnUnitMorph(unit);
    }
    
    /*
    @Override
    //reset expansion control mechanisms when a new hatchery starts
    public void onUnitMorph(Unit unit) {
    	if (unit.getType() == UnitType.Zerg_Hatchery) {
    		Variables.expanding = 2;
    		Variables.expansionTarget = null;
    		Variables.builder = null;
    		Variables.wantDrones = Variables.wantDrones + 3;
    		//in case we go below 17 supply (often happens against terran if our first overlord goes the right way)
    		Variables.wantOverlords = Variables.wantOverlords + 1;
    	}
    	
    	//build an overlord after the pool is started
    	if (unit.getType() == UnitType.Zerg_Spawning_Pool) { 
    		Variables.wantOverlords = Variables.wantOverlords + 1;
    	}
    }
    */
    
    @Override
    public void onUnitComplete(Unit unit) {
    	OnUnitComplete.runOnUnitComplete(unit);
    }
    
    /*
    @Override
    //tell units what to do when they are completed (construction FINISHES)
    public void onUnitComplete(Unit unit) {
    	if (unit.getPlayer() == self) {
    		
    		//When a hatchery is completed
    		if (unit.getType() == UnitType.Zerg_Hatchery) {
    			Variables.expanding = 0;
    			Variables.retreatPosition = unit.getPosition();
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
    			for (Unit neutralUnit : game.neutral().getUnits()) {
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
    			
    			/*
    			List<Position> positions = new ArrayList<Position>();	
    			
    			int i = 0;
    			for (BaseLocation spawnLocation : BWTA.getStartLocations()) {
    				positions.add(spawnLocation.getPosition());	
    				i = i + 1;
    			}
    			//
    		
    			unit.move(Variables.startPositions.get(Variables.overlords));
    			Variables.overlords = Variables.overlords + 1;
        	}
    		
    		if (unit.getType() == UnitType.Zerg_Zergling) {
    			
    			/*Position target;
    			target = new Position(500, 500);
    			unit.move(target);
    			
    			if (nearestEnemyBase != null) {
    				unit.attack(nearestEnemyBase);
    			}//
    			
    			//Add Zerglings to Hashset
    			if (!Variables.myUnitsSet.contains(unit)) {
    				Variables.myUnitsSet.add(unit);
    			}
    			
    			//Set isRetreating to false
    			Variables.isRetreating.put(unit, false);
    		}
        }
    }
    */
    
    @Override
    public void onUnitDiscover(Unit unit) {
    	OnUnitDiscover.runOnUnitDiscover(unit);
    }
    
    /*
    @Override
    public void onUnitDiscover(Unit unit) {
    	
    	//Add enemy units to Hashset
    	if (unit.getPlayer() == game.enemy()) {
    		if ((unit.getType().isBuilding() == true && unit.getType().canAttack() == true) || (unit.getType().isBuilding() == false && unit.getType().isWorker() == false)) {
    			if (!Variables.enemyUnitsSet.contains(unit)) {
    				Variables.enemyUnitsSet.add(unit);
    			}
    		}
    	
    		//Add enemy bases to Hashset
    		if (unit.getType().isResourceDepot()) {
    			game.sendText("base discovered");
    			Position basePosition = unit.getPosition();
				if (!Variables.enemyBasesSet.contains(basePosition)) {
					Variables.enemyBasesSet.add(basePosition);
    				game.sendText("base added to set");
    				//reset nearestEnemyBase (recalculate) and lastEnemyBase (do not need) when a new enemy base is found
    				Variables.nearestEnemyBase = null;
    				Variables.lastEnemyBase = null;
    			}
    		}
    	}
    }
    */
    
    @Override 
    public void onUnitDestroy(Unit unit){
    	OnUnitDestroy.runOnUnitDestroy(unit);
    }
    
    /*
    //What to do when we lose a unit.
    public void onUnitDestroy(Unit unit) {
    	if (unit.getPlayer() == self) {
    		
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
    			//THIS IS WORKING!!!
    			if (Variables.enemyBasesSet.size() == 0) {
    				BaseLocation nearestSpawnLocation = null;
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
    		
    		//This does not work! Builder label remains on map, order remains as "Move".
    		if (unit == Variables.builder) {
    			Variables.builder = null;
    			Variables.expanding = 0;
    		}
    	}
    	
    	//remove enemy units from set
    	if (unit.getPlayer() == game.enemy()) {
    		if (unit.getType().isResourceDepot()) {
    			//We want to make wantToAttack true, but this will just be undone next frame if enemy army is bigger
    			//wantToAttack = true;
    			Variables.enemyBasesSet.remove(unit.getPosition());
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
    */
    
    public static void main(String[] args) {
        new Delingvery().run();
    }
}
