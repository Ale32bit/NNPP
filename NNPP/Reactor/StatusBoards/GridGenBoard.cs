using NNPP.Audio;

namespace NNPP.Reactor.StatusBoards;

public class GridGenBoard : AbstractStatusBoard
{
    public override AbstractTile[,] Layout { get; }

    public GridGenBoard()
    {
        Layout = new AbstractTile[,]
        {
            { MainBusPowerInsuf, AuxBusPowerInsuf, DcBusPowerInsuf },
            { Gen1Running, Gen2Running, Gen3Running },
            { Gen1Malf, Gen2Malf, Gen3Malf },
            { TurbinesUnsync, TurbinesReversePower, GridPendingHandoff },
        };
    }

    public AlarmTile MainBusPowerInsuf { get; } = new("MAIN BUS POWER INSUF.", TileColor.Red, requireAck: true,
        sfx: AudioKeys.Sfx.AlarmAttention);

    public AlarmTile AuxBusPowerInsuf { get; } = new("AUX BUS POWER INSUF.", TileColor.Red, requireAck: true,
        sfx: AudioKeys.Sfx.AlarmAttention);

    public AlarmTile DcBusPowerInsuf { get; } = new("DC BUS POWER INSUF.", TileColor.Red, requireAck: true,
        sfx: AudioKeys.Sfx.AlarmAttention);

    public StatusTile Gen1Running { get; } = new("GEN 1 RUNNING");
    public StatusTile Gen2Running { get; } = new("GEN 2 RUNNING");
    public StatusTile Gen3Running { get; } = new("GEN 3 RUNNING");
    public AlarmTile Gen1Malf { get; } = new("GEN 1 MALFUNCTION", TileColor.Red, sfx: AudioKeys.Sfx.AlarmMalfunction);
    public AlarmTile Gen2Malf { get; } = new("GEN 2 MALFUNCTION", TileColor.Red, sfx: AudioKeys.Sfx.AlarmMalfunction);
    public AlarmTile Gen3Malf { get; } = new("GEN 3 MALFUNCTION", TileColor.Red, sfx: AudioKeys.Sfx.AlarmMalfunction);
    public AlarmTile TurbinesUnsync { get; } = new("TURBINES UNSYNCED");
    public AlarmTile TurbinesReversePower { get; } = new("TURBINE REV. POWER");
    public AlarmTile GridPendingHandoff { get; } = new("GRID PENDING HANDOFF");
}