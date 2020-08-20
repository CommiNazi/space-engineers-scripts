/*----------------------------------Start Config-----------------------------------------------------------------------------------------*/

string elevatorPistonTag = "CrewElevator";
string elevatorDoorName = "CrewElevatorDoor";
List<float> floorHeights = new List<float>();

/*----------------------------------End Config-------------------------------------------------------------------------------------------*/

const UpdateType CommandUpdate = UpdateType.Trigger | UpdateType.Terminal;
static IMyDoor elevatorDoor;
List<IMyPistonBase> elevatorPistons = new List<IMyPistonBase>();
static bool program_initialized = false;
static bool elevatorHasDoor = false;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    const string initializationMessage = Initialize();
    Echo("Initializing...   " + initializationMessage);
}

public string Initialize() {
  try {
    CollectPistons();
    CollectDoor();
    ParseSavedFloorHeights();
    program_initialized = true;
    return "Success";
  } catch(Exception e) {
    return e.Message;
  }
}

public void CollectPistons() {
  List<IMyPistonBase> tempPistons = new List<IMyPistonBase>();
  GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(tempPistons);

  foreach (IMyPistonBase piston in tempPistons) {
    if(piston.Name.Contains(elevatorPistonTag)) {
      elevatorPistons.Add(piston)
    }
  }

  if(elevatorPistons.Count == 0)
    throw new Exception('No Pistons Detected')
}

public void CollectDoor() {
  if(elevatorDoorName && elevatorDoorName != "") {
    elevatorDoor = GridTerminalSystem.GetBlockWithName(elevatorDoorName);
    if(elevatorDoor) elevatorHasDoor = true;
  }
}

public void ParseSavedFloorHeights() {

}

public void Main(string argument, UpdateType updateType) {

    // If the update source is either Trigger or Terminal, run the interactive logic
    if ((updateType & CommandUpdate) != 0) {
        switch(argument) {
            case "requestOuterOpen":
                airlockProcess.MoveNext(Command.RequestVacuumDoor);
                break;
            case "requestOuterClose":
                // Process.MoveNext(Command.RequestVacuumDoor);
                break;
            case "requestInnerOpen":
                airlockProcess.MoveNext(Command.RequestOxygenDoor);
                break;
            case "requestInnerClose":
                // Process.MoveNext(Command.RequestVacuumDoor);
                break;
            default:
                return;
        }
        return;
    }

    // Update source is not manual; run the polling logic
    airlockProcess.Update();

    return;
}
