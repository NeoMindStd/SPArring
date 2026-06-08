package delingvery;

import bwta.BWTA;
import bwta.BaseLocation;

import bwapi.*;

public class OnStart extends DefaultBWListener {
	public static void runOnStart() {

        Delingvery.game = Delingvery.mirror.getGame();
		Delingvery.self = Delingvery.game.self();
        
        //set speed to ladder speed (20). Lower is faster.
        Delingvery.game.setLocalSpeed(5);

        //Use BWTA to analyze map
        //This may take a few minutes if the map is processed first time!
        BWTA.readMap();
        BWTA.analyze();
        
        int i = 0;
		for (BaseLocation spawnLocation : BWTA.getStartLocations()) {
			Variables.startPositions.add(spawnLocation.getPosition());	
			i = i + 1;
		}
		
	}
}
