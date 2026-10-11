using System.Xml;

namespace NNPP.Reactor.StatusBoards;

public class AlarmTile(string label, TileColor color = TileColor.Yellow, bool requireAck = false)
    : AbstractTile(label, color)
{
    public bool RequireAck { get; set; } = requireAck;

    public override void Ack()
    {
        if (Status != TileStatus.Off)
        {
            Status = TileStatus.On;
        }
    }

    public override void Trigger()
    {
        if (Status != TileStatus.On) // if acknowledged
        {
            Status = TileStatus.AlarmActive;
        }
    }

    public override void Reset()
    {
        if (Status == TileStatus.Off)
        {
            return;
        }

        if (Status == TileStatus.On)
        {
            Status = TileStatus.Off;
            return;
        }

        Status = RequireAck ? TileStatus.AlarmPassive : TileStatus.Off;
    }

    public override void Check(bool enable)
    {
        if (enable)
        {
            Trigger();
        }
        else
        {
            Reset();
        }
    }
}