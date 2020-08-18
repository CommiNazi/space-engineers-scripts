/*----------------------------------Start Config-----------------------------------------------------------------------------------------*/

string elevatorPistonTag = "CrewElevator";
string elevatorDoorName = "CrewElevatorDoor";
List<float> floors = new List<float>(){

};

/*----------------------------------End Config-------------------------------------------------------------------------------------------*/

const UpdateType CommandUpdate = UpdateType.Trigger | UpdateType.Terminal;
static IMyDoor elevatorDoor;
List<IMyPistonBase> elevatorPistons = new List<IMyPistonBase>();
static bool program_initialized = false;
static bool elevatorHasDoor = false;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    Initialize();
}

public void Initialize() {
  CollectPistons();
  if(elevatorDoorName && elevatorDoorName != "") {
    elevatorDoor = GridTerminalSystem.GetBlockWithName(elevatorDoorName);
    if(elevatorDoor) elevatorHasDoor = true;
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
}

public void Main(string argument, UpdateType updateType) {
  if(!program_initialized) {
    airlockProcess.MoveNext(Command.Initialize);
    program_initialized = true;
  }

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
