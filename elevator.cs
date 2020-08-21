/*----------------------------------Start Config-----------------------------------------------------------------------------------------*/

string elevatorPistonTag = "CrewElevator";
string elevatorDoorName = "";
float elevatorSpeed = 0.5f;
int holdAtFloorDuration = 5; // in seconds

/*----------------------------------End Config-------------------------------------------------------------------------------------------*/


class UniqueQueue<T>
{
    readonly LinkedList<T> list;
    readonly IDictionary<T, int> dictionary;

    public UniqueQueue(LinkedList<T> list, IDictionary<T, int> dictionary)
    {
        this.list = list;
        this.dictionary = dictionary;
    }

    public int Length
    {
        get { return list.Count; }
    }

    public T Peek()
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The queue is empty");

        return list.First.Value;
    }

    public T Last()
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The queue is empty");

        return list.Last.Value;
    }

    public T Dequeue()
    {
        if (list.Count == 0)
            throw new InvalidOperationException("The queue is empty");

        var element = list.First.Value;
        dictionary.Remove(element);
        list.RemoveFirst();

        return element;
    }

    public void Enqueue(T element)
    {
        dictionary[element] = 0;

        if (dictionary.Count > list.Count)
            list.AddLast(element);
    }

    public override string ToString()
    {
        string combinedString = string.Join(",", list.ToArray());
        return combinedString;
    }
}

class Timer
{
    private int ticks;
    Timer(int timeout)
    {
        ticks = timeout * 60;
    }
    public bool Wait()
    {
        if(ticks > 0)
        {
            ticks--;
            return true;
        } else
        {
            return false;
        }
    }
}
const UpdateType CommandUpdate = UpdateType.Trigger | UpdateType.Terminal;
Dictionary<string, float> floorHeights = new Dictionary<string, float>();
UniqueQueue<float> requestQueue = new UniqueQueue<float>(new LinkedList<float>(), new Dictionary<float, int>());
static IMyDoor elevatorDoor;
List<IMyPistonBase> elevatorPistons = new List<IMyPistonBase>();
bool program_initialized = false;
bool elevatorHasDoor = false;
bool isMoving = false;
float currentTarget;
Timer arrivedPauseDuration;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;

    string initializationMessage = Initialize();
    Echo("Initializing...   " + initializationMessage);
}

public void CollectPistons()
{
    List<IMyPistonBase> tempPistons = new List<IMyPistonBase>();
    this.GridTerminalSystem.GetBlocksOfType(tempPistons);

    foreach (IMyPistonBase piston in tempPistons)
    {
        if (piston.CustomName.Contains(elevatorPistonTag))
        {
            elevatorPistons.Add(piston);
        }
    }

    if (elevatorPistons.Count == 0)
    {
        throw new Exception("Piston Panic");
    }

}

public void CollectDoor()
{
    if (elevatorDoorName.Length > 0)
    {
        try
        {
            elevatorDoor = this.GridTerminalSystem.GetBlockWithName(elevatorDoorName) as IMyDoor;
            if(elevatorDoor != null)
                elevatorHasDoor = true;
        }
        catch (Exception e)
        {
            throw new Exception("Didn't Detect Door");
        }
    }
}

public void ParseSavedFloorHeights()
{
    try
    {
        string[] customDataHeights = Me.CustomData.Split('\n');
        string[] rowData;

        foreach (string cdHeight in customDataHeights)
        {
            rowData = cdHeight.Split(':');
            floorHeights.Add(rowData[0], float.Parse(rowData[1]));
        }
    }
    catch (Exception e)
    {
        Me.CustomData = "";
        throw new Exception("Cannot Construe CustomData, Clearing");
    }
}

public string Initialize()
{
    try
    {
        CollectPistons();
        CollectDoor();
        ParseSavedFloorHeights();
        program_initialized = true;
        return SerializeFloorHeights();
    }
    catch (Exception e)
    {
        throw e;
        // return e.Message;
    }
}

public float GetCurrentHeight()
{
    float currentHeight = 0;
    foreach (IMyPistonBase piston in elevatorPistons)
    {
        floorHeight += piston.CurrentPosition;
    }
    
    return currentHeight;
}

public void SaveFloorHeight(string floorName = "")
{
    float floorHeight = GetCurrentHeight();

    if (floorName == "")
        floorName = (floorHeights.Count + 1).ToString();

    floorHeights.Add(floorName, floorHeight);

    Me.CustomData = SerializeFloorHeights();
    Echo("Floor Saved, assign a button action to run this script with this argument:" + $"request {floorName}");
}

public string SerializeFloorHeights()
{
    string outputString = "";

    foreach (KeyValuePair<string, float> pair in floorHeights)
    {
        outputString += $"{pair.Key}:{pair.Value.ToString()}\n";
    }
    return outputString.Trim();
}

public void DeleteFloorHeight(string floorName)
{
    floorHeights.Remove(floorName);
    Me.CustomData = SerializeFloorHeights();
}

public void GoToHeight(float targetHeight)
{
    float currentHeight = GetCurrentHeight();
    
    if(isMoving)
    {
        if(currentHeight == targetHeight)
            Arrive();
    } 
    else 
    {
        double pistonPortion = Math.Round(targetHeight / elevatorPistons.Count, 4);

        isMoving = true;

        foreach(IMyPistonBase piston in elevatorPistons)
        {
            piston.MinLimit = (float) pistonPortion;
            piston.MaxLimit = (float) pistonPortion;
            if (currentHeight < targetHeight)
            {
                piston.Velocity = elevatorSpeed;
            }
            else if(currentHeight > targetHeight)
            {
                piston.Velocity = -elevatorSpeed;
            }
            else
            {
                Arrive();
            }
        }
    }
}

public void Arrive()
{
    isMoving = false;
    currentTarget  = null;
    foreach (IMyPistonBase piston in elevatorPistons)
    {
        piston.Velocity = 0.0f;
    }
    if (elevatorHasDoor)
    {
        elevatorDoor.OpenDoor();
    }
    arrivedPauseDuration = new Timer(holdAtFloorDuration);
}

public void Main(string argument, UpdateType updateType)
{

    // If the update source is either Trigger or Terminal, run the interactive logic
    if ((updateType & CommandUpdate) != 0)
    {
        string[] request = argument.Split(' ');
        string operation = request[0];
        string[] opArguments = request.Skip(1).ToArray();
        if (operation.Contains("save"))
        {
            SaveFloorHeight(opArguments[0]);
        }

        if (operation.Contains("delete"))
        {
            DeleteFloorHeight(opArguments[0]);
        }

        if (operation.Contains("request"))
        {
            Echo(opArguments[0]);
            float floor;
            floorHeights.TryGetValue(opArguments[0], out floor);
            requestQueue.Enqueue(floor);
        }

        return;
    }

    // Update source is not manual; run the polling logic
    if (program_initialized)
    {
        if(requestQueue.Length > 0)
        {
            float nextInLine = requestQueue.Peek();
            float lastInLine = requestQueue.Last();
            Echo(nextInLine.ToString());
            Echo(lastInLine.ToString());
            Echo(requestQueue.ToString());

            if(isMoving)
            {
                GoToHeight(currentTarget);
                return;
            }
            else if(arrivedPauseDuration != null && arrivedPauseDuration.Wait()))
            { // Arrived but pausing at floor.
                return;
            }
            else if(arrivedPauseDuration != null && !arrivedPauseDuration.Wait())
            { //Arrived, done pausing at floor
                arrivedPauseDuration = null;
                // return here, we could move onto the floor, but one extra tick won't kill anything.
                return;
            }
            else if (currentHeight != requestQueue.Peek())
            {
                currentTarget = requestQueue.Dequeue()
                GoToHeight(currentTarget);
                return;
            }
        } else {
            // make sure that once there's a new request, we're not waiting on an old timer.
            arrivedPauseDuration = null;
        }
    }

    return;
}
