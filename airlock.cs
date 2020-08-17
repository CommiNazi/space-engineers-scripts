static IMyAirVent vent;
static IMyDoor oxygenDoor;
static IMyDoor vacuumDoor;
static Process airlockProcess;
static bool program_initialized = false;

public enum States {
  OpenToVacuum,
  OpenToOxygen,
  Depressurizing,
  Pressurizing,
  Depressurized,
  Pressurized,
  Paused,
  Inactive,
  Terminated,
  Ready
}

public abstract class ProcessState {
  public abstract void Enter();
  public abstract void Execute();
  public abstract void Exit();
  public override int GetHashCode() {
    return (int)System.Enum.Parse(typeof(States), this.GetType().Name);
  }
}

public class OpenToVacuum : ProcessState {
  public override void Enter(Process process) {
    if(vent.GetOxygenLevel() > 0.1f)
      throw new Exception("Trying to open to vacuum, but warming room is pressurized");

    vacuumDoor.ApplyAction("OnOff_On");
  }
  public override void Execute(Process process) {
    vacuumDoor.OpenDoor();
  }
  public override void Exit(Process process) {
    vacuumDoor.CloseDoor();
  }
}

public class OpenToOxygen : ProcessState {
  public override void Enter(Process process) {
    if(vent.GetOxygenLevel() < 0.9f)
      throw new Exception("Trying to open to pressure, but warming room is depressurized");

    oxygenDoor.ApplyAction("OnOff_On");
  }
  public override void Execute(Process process) {
    oxygenDoor.OpenDoor();
  }
  public override void Exit(Process process) {
    oxygenDoor.CloseDoor();
  }
}

public class Depressurizing : ProcessState {
  public override void Enter(Process process) {
    oxygenDoor.CloseDoor();
  }
  public override void Execute(Process process) {
    if(oxygenDoor.Status == DoorStatus.Closed) {
      if(!vent.Depressurize) vent.ApplyAction("Depressurize_On");
      if(oxygenDoor.Enabled) oxygenDoor.ApplyAction("OnOff_Off");
    }

    if(vent.GetOxygenLevel() <= 0.1f)
      process.MoveNext(Command.Depressurize);
  }
  public override void Exit(Process process) {}
}

public class Pressurizing : ProcessState {
  public override void Enter(Process process) {
    vacuumDoor.CloseDoor();
  }
  public override void Execute(Process process) {
    if(vacuumDoor.Status == DoorStatus.Closed) {
      if(vent.Depressurize) vent.ApplyAction("Depressurize_Off");
      if(vacuumDoor.Enabled) vacuumDoor.ApplyAction("OnOff_Off");
    }

    if(vent.GetOxygenLevel() >= 0.9f)
      process.MoveNext(Command.Pressurize);
  }
  public override void Exit(Process process) {}
}

public class Depressurized : ProcessState {
  public override void Enter(Process process) {}
  public override void Execute(Process process) {
    process.MoveNext(Command.OpenVacuumDoor); // Maybe move this to Enter, to save a tick?
  }
  public override void Exit(Process process) {}
}

public class Pressurized : ProcessState {
  public override void Enter(Process process) {}
  public override void Execute(Process process) {
    process.MoveNext(Command.OpenOxygenDoor); // Maybe move this to Enter, to save a tick?
  }
  public override void Exit(Process process) {}
}

public class Paused : ProcessState {
  public override void Enter(Process process) {
    oxygenDoor.Enabled = false;
    vacuumDoor.Enabled = false;
  }
  public override void Execute(Process process) {}
  public override void Exit(Process process) {}
}

public class Inactive : ProcessState {
  public override void Enter(Process process) {
    oxygenDoor.Enabled = true;
    vacuumDoor.Enabled = true;
    vent.Enabled = false;
  }
  public override void Execute(Process process) {}
  public override void Exit(Process process) {}
}

public class Terminated : ProcessState {
  public override void Enter(Process process) {
    oxygenDoor.OpenDoor();
    oxygenDoor.Enabled = false;

    vacuumDoor.OpenDoor();
    vacuumDoor.Enabled = false;

    vent.Enabled = false;
  }
  public override void Execute(Process process) {}
  public override void Exit(Process process) {}
}

public class Ready : ProcessState {
  public override void Enter(Process process) {
    oxygenDoor.Enabled = true;
    oxygenDoor.CloseDoor();

    vacuumDoor.Enabled = true;
    vacuumDoor.CloseDoor();

    vent.Enabled = true;
    vent.ApplyAction("Depressurize_On");
  }
  public override void Execute(Process process) {}
  public override void Exit(Process process) {}
}

public enum Command {
  Initialize,

  RequestOxygenDoor,
  Pressurize,
  OpenOxygenDoor,

  RequestVacuumDoor,
  Depressurize,
  OpenVacuumDoor,

  HoldOpen,
  Lockdown
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
      // return other != null && this.CurrentState == other.CurrentState && this.Command == other.Command;
      // throw new Exception(this.CurrentState.GetType() + " " + other.CurrentState.GetType() + " " + this.Command + " " + other.Command + " " + (this.Command == other.Command));
      return other != null && this.CurrentState.GetType() == other.CurrentState.GetType() && this.Command == other.Command;
    }
  }

  Dictionary<StateTransition, ProcessState> transitions;
  public ProcessState CurrentState { get; private set; }

  public Process() {
    CurrentState = new Inactive();
    transitions = new Dictionary<StateTransition, ProcessState> {
      { new StateTransition(new Ready(),          Command.RequestOxygenDoor), new Pressurizing()},
      { new StateTransition(new Pressurizing(),   Command.Pressurize),        new Pressurized()},
      { new StateTransition(new Pressurized(),    Command.OpenOxygenDoor),    new OpenToOxygen()},

      { new StateTransition(new Ready(),          Command.RequestVacuumDoor), new Depressurizing()},
      { new StateTransition(new Depressurizing(), Command.Depressurize),      new Depressurized()},
      { new StateTransition(new Depressurized(),  Command.OpenVacuumDoor),    new OpenToVacuum()},

      { new StateTransition(new OpenToOxygen(),   Command.RequestVacuumDoor), new Depressurizing()},
      { new StateTransition(new OpenToVacuum(),   Command.RequestOxygenDoor), new Pressurizing()},

      { new StateTransition(new OpenToOxygen(),   Command.RequestOxygenDoor), new OpenToOxygen()},
      { new StateTransition(new OpenToVacuum(),   Command.RequestVacuumDoor), new OpenToVacuum()},
      // { new StateTransition(new OpenToOxygen(),   Command.CloseOxygenDoor),   new Ready()},
      // { new StateTransition(new OpenToVacuum(),   Command.CloseVacuumDoor),   new Ready()},

      // { new StateTransition(new Terminated(),     Command.Initialize),        new Inactive() }
      { new StateTransition(new Inactive(),       Command.Initialize),        new Ready() },
      // { new StateTransition(new Ready(),          Command.End),               new Inactive() },
      // { new StateTransition(new Ready(),          Command.Pause),             new Paused() },
      // { new StateTransition(new Inactive(),       Command.Exit),              new Terminated() },
      // { new StateTransition(new Paused(),         Command.End),               new Inactive() },
      // { new StateTransition(new Paused(),         Command.Resume),            new Active() },
    };
  }


  public ProcessState GetNext(Command command) {
    StateTransition transition = new StateTransition(CurrentState, command);

    ProcessState nextState;
    if (!transitions.TryGetValue(transition, out nextState)) {
      // throw new Exception("Invalid transition: " + CurrentState + " -> " + command + " :: " + nextState);
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

    vent         = GridTerminalSystem.GetBlockWithName("Airlock-Vent")   as IMyAirVent;
    oxygenDoor   = GridTerminalSystem.GetBlockWithName("CrewAirlockInner")  as IMyDoor;
    vacuumDoor   = GridTerminalSystem.GetBlockWithName("CrewAirlockOuter")  as IMyDoor;
    airlockProcess = new Process();
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
