/*----------------------------------Start Config-----------------------------------------------------------------------------------------*/
string triggerBlock = "ScannerTrigger"; 
string scannerCamera = "ScannerCamera";
string triggerAction = "TriggerNow";
float scanRange = 2000; //How far the camera ray will scan in meters. Camera charges at approx 2000m per second.

List<MyDetectedEntityType> triggerTargets = new List<MyDetectedEntityType>(){
  MyDetectedEntityType.None,
  MyDetectedEntityType.Unknown,
  MyDetectedEntityType.SmallGrid,
  MyDetectedEntityType.LargeGrid,
  MyDetectedEntityType.CharacterHuman,
  MyDetectedEntityType.CharacterOther,
  MyDetectedEntityType.FloatingObject,
  MyDetectedEntityType.Asteroid,
  MyDetectedEntityType.Planet,
  MyDetectedEntityType.Meteor,
  MyDetectedEntityType.Missile
}

List<MyRelationsBetweenPlayerAndBlock> ownershipTargets = new List<MyRelationsBetweenPlayerAndBlock>() {
  MyRelationsBetweenPlayerAndBlock.NoOwnership,
  MyRelationsBetweenPlayerAndBlock.Owner,
  MyRelationsBetweenPlayerAndBlock.FactionShare,
  MyRelationsBetweenPlayerAndBlock.Neutral,
  MyRelationsBetweenPlayerAndBlock.Enemies,
  MyRelationsBetweenPlayerAndBlock.Friends
}
/*----------------------------------End Config-------------------------------------------------------------------------------------------*/

MyDetectedEntityInfo scannerTarget;

IMyCameraBlock camera;
IMyTerminalBlock trigger;

public Program() {
  //Runtime.UpdateFrequency = UpdateFrequency.Update1;

  trigger = GridTerminalSystem.GetBlockWithName(triggerBlock) as IMyTerminalBlock;
	camera = GridTerminalSystem.GetBlockWithName(scannerCamera) as IMyCameraBlock;
}

void Main(string argument){
	
	
	if(trigger == null || camera == null){		
		Echo("Blocks are unset. Please check naming and ownership.");
		return;		
	}
	
	//Enable Raycast, Check If Scan is Possible, Perform Scan.
	camera.EnableRaycast = true;
	if(camera.CanScan(scanRange)){
		
		scannerTarget = camera.Raycast(scanRange, 0, 0);
		Echo("Scanned Target Type / Relationship: \n"+scannerTarget.Type.ToString()+"\n"+scannerTarget.Relationship.ToString());
		
	}
	
	if(triggerTargets.Contains(scannerTarget.Type) && ownershipTargets.Contains(scannerTarget.Relationship)) {
    trigger.ApplyAction(triggerAction);
  }
	
}
