package delingvery;

import java.util.HashSet;
import java.util.List;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;

import bwapi.*;
//import bwta.BWTA;
import bwta.BaseLocation;

public class Variables {
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
    
    // declares list of locations for overlords to scout
    public static List<Position> startPositions = new ArrayList<Position>();
    
    // declares number of drones chasing enemies in our base. 0 = no drones fighting, but threatened, -1 = unthreatened.
    public static int fightingDrones = -1;
    
    // declares whether enemies have been found near our drones this frame. Used to combine fightingDrones for every drone.
    public static boolean dronesThreatened = false;

}
