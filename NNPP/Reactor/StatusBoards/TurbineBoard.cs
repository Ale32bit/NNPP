using NNPP.Audio;

namespace NNPP.Reactor.StatusBoards;

public class TurbineBoard : AbstractStatusBoard
{
    public class Turbine(AbstractTile running, AbstractTile synced) : AbstractStatusBoard
    {
        public override AbstractTile[,] Layout { get; } = new[,]
        {
            { running, },
            { synced, },
        };
    }

    public override AbstractTile[,] Layout { get; }

    public TurbineBoard()
    {
        Layout = new AbstractTile[,]
        {
            { Turbine1Malfunction, Turbine1Overspeed, Turbine1VibrationHigh },
            { Turbine1Tripped, Turbine1Underspeed, Turbine1VibrationCritical },
            { Turbine2Malfunction, Turbine2Overspeed, Turbine2VibrationHigh },
            { Turbine2Tripped, Turbine2Underspeed, Turbine2VibrationCritical },
        };

        Turbine1 = new Turbine(Turbine1Running, Turbine1Synced);
        Turbine2 = new Turbine(Turbine2Running, Turbine2Synced);
    }

    public Turbine Turbine1 { get; }
    public Turbine Turbine2 { get; }

    public StatusTile Turbine1Running { get; } = new("TURBINE 1 RUNNING");
    public StatusTile Turbine2Running { get; } = new("TURBINE 2 RUNNING");
    public StatusTile Turbine1Synced { get; } = new("TURBINE 1 SYNCED");
    public StatusTile Turbine2Synced { get; } = new("TURBINE 2 SYNCED");

    public AlarmTile Turbine1Malfunction { get; } = new("TURBINE 1 MALFUNCTION", TileColor.Red, requireAck: true,
        sfx: AudioKeys.Sfx.AlarmMalfunction);

    public AlarmTile Turbine2Malfunction { get; } = new("TURBINE 2 MALFUNCTION", TileColor.Red, requireAck: true,
        sfx: AudioKeys.Sfx.AlarmMalfunction);

    public AlarmTile Turbine1Tripped { get; } = new("TURBINE 1 TRIPPED", TileColor.Red, requireAck: true,
        sfx: AudioKeys.Sfx.AlarmOverspeed);

    public AlarmTile Turbine2Tripped { get; } = new("TURBINE 2 TRIPPED", TileColor.Red, requireAck: true,
        sfx: AudioKeys.Sfx.AlarmOverspeed);

    public AlarmTile Turbine1Overspeed { get; } =
        new("TURBINE 1 OVERSPEED", TileColor.Red, requireAck: true, clearSfx: false, sfx: AudioKeys.Sfx.AlarmOverspeed);

    public AlarmTile Turbine2Overspeed { get; } =
        new("TURBINE 2 OVERSPEED", TileColor.Red, requireAck: true, clearSfx: false, sfx: AudioKeys.Sfx.AlarmOverspeed);

    public AlarmTile Turbine1Underspeed { get; } = new("TURBINE 1 UNDERSPEED");
    public AlarmTile Turbine2Underspeed { get; } = new("TURBINE 2 UNDERSPEED");

    public AlarmTile Turbine1VibrationHigh { get; } =
        new("TURBINE 1 VIBRATION HIGH", requireAck: true, sfx: AudioKeys.Sfx.Alarm1);

    public AlarmTile Turbine2VibrationHigh { get; } =
        new("TURBINE 2 VIBRATION HIGH", requireAck: true, sfx: AudioKeys.Sfx.Alarm1);

    public AlarmTile Turbine1VibrationCritical { get; } = new("TURBINE 1 VIBRATION CRIT.", TileColor.Red,
        requireAck: true, sfx: AudioKeys.Sfx.AlarmAttention);

    public AlarmTile Turbine2VibrationCritical { get; } = new("TURBINE 2 VIBRATION CRIT.", TileColor.Red,
        requireAck: true, sfx: AudioKeys.Sfx.AlarmAttention);
}