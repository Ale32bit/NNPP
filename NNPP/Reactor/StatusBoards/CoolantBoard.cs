namespace NNPP.Reactor.StatusBoards;

public class CoolantBoard : AbstractStatusBoard
{
    public override AbstractTile[,] Layout { get; }

    public CoolantBoard()
    {
        Layout = new AbstractTile[,]
        {
            { Fw1Malfunction, Fw1Cavitation, FwValveOpen },
            { Fw2Malfunction, Fw2Cavitation, FwValveClosed },
            { CoolantValveOpen, CoolantValveClosed, FwFlowLow },
            { Clp1Off, Clp1Runup, Clp1Running },
            { Clp2Off, Clp2Runup, Clp2Running },
        };
    }

    public StatusTile FwValveOpen { get; } = new("FEEDWATER VALVE OPEN");
    public AlarmTile FwValveClosed { get; } = new("FEEDWATER VALVE CLOSED");
    public AlarmTile Fw1Malfunction { get; } = new("FEED PUMP 1 MALFUNCTION", TileColor.Red);
    public AlarmTile Fw1Cavitation { get; } = new("FEED PUMP 1 CAVITATING", TileColor.Red);
    public AlarmTile Fw2Malfunction { get; } = new("FEED PUMP 2 MALFUNCTION", TileColor.Red);
    public AlarmTile Fw2Cavitation { get; } = new("FEED PUMP 2 CAVITATING", TileColor.Red);
    public AlarmTile FwFlowLow { get; } = new("FEED PUMP FLOW LOW");
    public StatusTile CoolantValveOpen { get; } = new("COOLANT VALVE OPEN");
    public AlarmTile CoolantValveClosed { get; } = new("COOLANT VALVE CLOSED");
    public AlarmTile Clp1Off { get; } = new("COOLANT PUMP 1 OFF", TileColor.Red);
    public AlarmTile Clp2Off { get; } = new("COOLANT PUMP 1 OFF", TileColor.Red);
    public AlarmTile Clp1Runup { get; } = new("COOLANT PUMP 1 RUNUP");
    public AlarmTile Clp2Runup { get; } = new("COOLANT PUMP 2 RUNUP");
    public StatusTile Clp1Running { get; } = new("COOLANT PUMP 1 RUNNING");
    public StatusTile Clp2Running { get; } = new("COOLANT PUMP 1 RUNNING");
}