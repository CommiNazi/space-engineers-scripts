/*----------------------------------Start Config-----------------------------------------------------------------------------------------*/

string elevatorPistonTag = "CrewElevator";
string elevatorDoorName = "CrewElevatorDoor";
Dictionary<string, float> floorHeights = new Dictionary<string, float>();
UniqueQueue<float> requestQueue = new UniqueQueue<>(new LinkedList<float>(), new IDictionary<float , int>());

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
      elevatorPistons.Add(piston);
    }
  }

  if(elevatorPistons.Count == 0)
    throw new Exception('No Pistons Detected');
}

public void CollectDoor() {
  if(elevatorDoorName && elevatorDoorName != "") {
    elevatorDoor = GridTerminalSystem.GetBlockWithName(elevatorDoorName);
    if(elevatorDoor)
      elevatorHasDoor = true;
  }
}

public void ParseSavedFloorHeights() {
  try {
    string[] customDataHeights = Me.CustomData.Split('\n');
    string[] rowData;

    foreach (string cdHeight in customDataHeights) {
      rowData = cdHeight.Split(':');
      floorHeights.Add( rowData[0], (float) rowData[1] );
    }
  } catch (Exception e) {
    Me.CustomData = "";
    throw new Exception("Cannot Parse CustomData, clearing");
  }
}

public void SaveFloorHeight(string floorName = "") {
  float floorHeight = 0;
  foreach (IMyPistonBase piston in tempPistons) {
    floorHeight += piston.CurrentPosition;
  }

  if(floorName == "")
    floorName = (floorHeights.Count + 1).ToString();

  floorHeights.Add(floorName, floorHeight);

  Me.CustomData = SerializeFloorHeights();
  Echo("Floor Saved, assign a button action to run this script with this argument:" + $"request {floorName}")
}

public string SerializeFloorHeights() {
  string outputString = "";

  foreach(KeyValuePair<string, float> pair in floorHeights) {
    outputString += $"{pair.Key}:{pair.Value.ToString()}\n"
  }
  return outputString.Trim();
}

public void DeleteFloorHeight(string floorName) {
  floorHeights.Remove(floorName);
  Me.CustomData = SerializeFloorHeights();
}

class UniqueQueue<T> : IQueue<T> {
    readonly LinkedList<T> list;
    readonly IDictionary<T, int> dictionary;

    public UniqueQueue(LinkedList<T> list, IDictionary<T, int> dictionary) {
        this.list = list;
        this.dictionary = dictionary;
    }

    public int Length {
        get { return list.Count; }
    }

    public T Peek() {
      if (list.Count == 0)
          throw new InvalidOperationException("The queue is empty");

      return list.First.Value;
    }

    public T Dequeue() {
        if (list.Count == 0)
            throw new InvalidOperationException("The queue is empty");

        var element = list.First.Value;
        dictionary.Remove(element);
        list.RemoveFirst();

        return element;
    }

    public void Enqueue(T element) {
        dictionary[element] = 0;

        if (dictionary.Count > list.Count)
            list.AddLast(element);
    }
}

public void Main(string argument, UpdateType updateType) {

    // If the update source is either Trigger or Terminal, run the interactive logic
    if ((updateType & CommandUpdate) != 0) {

      if(argument.Contains("save")) {
        string saveName = argument.Split(' ')[1];
        if(saveName) {
          SaveFloorHeight(saveName);
        } else {
          SaveFloorHeight();
        }
      }

      if(argument.Contains("delete")) {
        string deleteName = argument.Split(' ')[1];
        DeleteFloorHeight(deleteName);
      }

      if(argument.Contains("request")) {
        string requestFloor = argument.Split(' ')[1];
        requestQueue.Enqueue((float) requestFloor);
      }

        return;
    }

    // Update source is not manual; run the polling logic
    airlockProcess.Update();

    return;
}
