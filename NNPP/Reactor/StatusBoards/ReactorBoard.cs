using NNPP.Audio;

namespace NNPP.Reactor.StatusBoards;

public class ReactorBoard : AbstractStatusBoard
{
    public override AbstractTile[,] Layout { get; }

    public ReactorBoard()
    {
        Layout = new AbstractTile[,]
        {
            { Offline, Online, Scram },
            { TempLow, TempHigh, ControlRodLow },
            { PressureLow, PressureHigh, ReliefValveActive },
            { WaterLevelLow, FuelEfficiencyLow, FuelLow }
        };
    }

    public StatusTile Offline { get; } = new("REACTOR OFFLINE");
    public StatusTile Online { get; } = new("REACTOR ONLINE");
    public AlarmTile Scram { get; } = new("REACTOR SCRAM", TileColor.Red, requireAck: true);
    public AlarmTile TempLow { get; } = new("REACTOR TEMP LOW");
    public AlarmTile TempHigh { get; } = new("REACTOR TEMP HIGH", TileColor.Red, sfx: AudioKeys.Sfx.Alarm1, clearSfx: false);
    public AlarmTile ControlRodLow { get; } = new("CONTROL ROD INSERTION LOW", TileColor.Red, requireAck: true, sfx: AudioKeys.Sfx.AlarmSevere);
    public AlarmTile PressureLow { get; } = new("LOW REACTOR PRESSURE");
    public AlarmTile PressureHigh { get; } = new("HIGH REACTOR PRESSURE", TileColor.Red, sfx: AudioKeys.Sfx.AlarmSevere);
    public StatusTile ReliefValveActive { get; } = new("RELIEF VALVES ACTIVE", TileColor.Green);
    public AlarmTile WaterLevelLow { get; } = new("WATER LEVEL LOW", TileColor.Red, sfx: AudioKeys.Sfx.AlarmSevere);
    public AlarmTile FuelEfficiencyLow { get; } = new("REACTOR FUEL EFFICIENCY LOW");
    public AlarmTile FuelLow { get; } = new("REACTOR FUEL LOW", TileColor.Red, sfx: AudioKeys.Sfx.AlarmMalfunction);
}