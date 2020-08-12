IMyReflectorLight light;
Process ProcessManager;

Color colorVacant = new Color(75, 255, 155);
Color colorOccupied = new Color(255, 255, 255);

public interface ProcessState {
  void Enter(Process process);
  void Execute(Process process);
  void Exit(Process process);
}

public enum States {
  ReactorRoomPresent,
  ReactorRoomVacant
}


public class ReactorRoomPresent : ProcessState {
  Enter() {
    light.SetValue<Color>("Color", colorOccupied);
    light.Intensity = 1;
  }
  Execute() {}
  Exit() {}
  public override int GetHashCode() {
    return States.ReactorRoomPresent.GetHashCode();
  }
}

public class ReactorRoomVacant : ProcessState {
  Enter() {
    light.SetValue<Color>("Color", colorVacant);
    light.Intensity = 0.5;
  }
  Execute() {}
  Exit() {}
  public override int GetHashCode() {
    return States.ReactorRoomVacant.GetHashCode();
  }
}

public enum Command {
  EnterReactorRoom,
  LeaveReactorRoom
}

public class Process {

  class StateTransition {
    readonly ProcessState CurrentState;
    readonly Command Command;

    public StateTransition(ProcessState currentState, Command command) {
      CurrentState = currentState;
      Command = command;
    }

    public override int GetHashCode() {
      return 17 + 31 * CurrentState.GetHashCode() + 31 * Command.GetHashCode();
    }

    public override bool Equals(object obj) {
      StateTransition other = obj as StateTransition;
      return other != null && this.CurrentState.GetType() == other.CurrentState.GetType() && this.Command == other.Command;
    }
  }

  Dictionary<StateTransition, ProcessState> transitions;
  public ProcessState CurrentState { get; private set; }

  public Process() {
    CurrentState = new Inactive();
    transitions = new Dictionary<StateTransition, ProcessState> {
      /* Reactor Room States*/
      { new StateTransition(new ReactorRoomVacant(),  Command.EnterReactorRoom), new ReactorRoomPresent() },
      { new StateTransition(new ReactorRoomPresent(), Command.EnterReactorRoom), new ReactorRoomPresent() },
      { new StateTransition(new ReactorRoomPresent(), Command.LeaveReactorRoom), new ReactorRoomVacant() },
      { new StateTransition(new ReactorRoomVacant(),  Command.LeaveReactorRoom), new ReactorRoomVacant() },
    };
  }


  public ProcessState GetNext(Command command) {
    StateTransition transition = new StateTransition(CurrentState, command);

    ProcessState nextState;
    if (!transitions.TryGetValue(transition, out nextState)) {
      throw new Exception("Invalid transition: " + CurrentState + " -> " + command + " :: " + nextState);
      return CurrentState;
    }

    return nextState;
  }

  public ProcessState MoveNext(Command command) {

    ProcessState nextState = GetNext(command);
    if(nextState.GetHashCode() == CurrentState.GetHashCode()) {
      return CurrentState;
    }

    CurrentState.Exit(this);

    CurrentState = GetNext(command);
    CurrentState.Enter(this);
    return CurrentState;
  }

  public void Update() {
    CurrentState.Execute(this);
  }
}

const UpdateType CommandUpdate = UpdateType.Trigger | UpdateType.Terminal;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    light = GridTerminalSystem.GetBlockWithName("Airlock-Vent") as IMyReflectorLight;
    ProcessManager = new Process();
}

public void Main(string argument, UpdateType updateType) {

    // If the update source is either Trigger or Terminal, run the interactive logic
    if ((updateType & CommandUpdate) != 0) {
        switch(argument) {
            case "EnterReactorRoom":
              ProcessManager.MoveNext(Command.EnterReactorRoom);
              break;
            case "LeaveReactorRoom":
              ProcessManager.MoveNext(Command.LeaveReactorRoom);
              break;
            default:
                return;
        }
        return;
    }

    // Update source is not manual; run the polling logic
    ProcessManager.Update();

    return;
}
