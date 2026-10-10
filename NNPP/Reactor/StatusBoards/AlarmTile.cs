namespace NNPP.Reactor.StatusBoards;

public class AlarmTile(string label, TileColor color = TileColor.Yellow) : AbstractTile(label, color)
{
    public bool RequireAck { get; set; }

    public override void Ack()
    {
        if (Status != TileStatus.Off)
        {
            Status = TileStatus.On;
        }
    }

    public override void Trigger()
    {
        Status = TileStatus.AlarmActive;
    }

    public override void Reset()
    {
        Status = RequireAck ? TileStatus.AlarmPassive : TileStatus.Off;
    }
}