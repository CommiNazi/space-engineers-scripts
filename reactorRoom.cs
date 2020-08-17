static IMyLightingBlock spotlight;
static IMyLightingBlock bedroomLight;
static int reactorOccupantCount = 0;
static int bedroomOccupantCount = 0;
static Color colorVacant = new Color(75, 255, 155);
static Color colorOccupied = new Color(255, 255, 255);

public void EnterRoom(IMyLightingBlock light, ref int occupantCount, Color color) {
  light.ApplyAction("OnOff_On");
  light.SetValue<Color>("Color", color);
  light.Intensity = 1.0f;
  occupantCount++;
}

public void ExitRoom(IMyLightingBlock light, ref int occupantCount) {
  occupantCount--;

  if(occupantCount < 0)
    occupantCount = 0;

  if(occupantCount == 0) {
    light.ApplyAction("OnOff_Off");
  }
}

public void ExitReactorRoom(IMyLightingBlock light, ref int occupantCount, Color color) {
  occupantCount--;

  if(occupantCount < 0)
    occupantCount = 0;

  if(occupantCount == 0) {
    light.SetValue<Color>("Color", color);
    light.Intensity = 0.5f;
  }
}

const UpdateType CommandUpdate = UpdateType.Trigger | UpdateType.Terminal;

public Program() {
    spotlight = GridTerminalSystem.GetBlockWithName("Spotlight") as IMyLightingBlock;
    bedroomLight = GridTerminalSystem.GetBlockWithName("Bedroom Light") as IMyLightingBlock;
}

public void Main(string argument, UpdateType updateType) {
  switch(argument) {
      case "EnterReactorRoom":
        EnterRoom(spotlight, ref reactorOccupantCount, colorOccupied);
        break;
      case "LeaveReactorRoom":
        ExitReactorRoom(spotlight, ref reactorOccupantCount, colorVacant);
        break;
      case "EnterBedroom":
        EnterRoom(bedroomLight, ref bedroomOccupantCount, colorOccupied);
        break;
      case "ExitRoom":
        ExitRoom(bedroomLight, ref bedroomOccupantCount);
        break;
      default:
          return;
  }
  return;
}
