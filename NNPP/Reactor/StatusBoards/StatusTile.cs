namespace NNPP.Reactor.StatusBoards;

public class StatusTile(string label, TileColor color = TileColor.Blue) : AbstractTile(label, color)
{
    
    public override void Ack()
    {
        // nothing to do
    }

    public override void Trigger()
    {
        Status = TileStatus.On;
    }

    public override void Reset()
    {
        Status = TileStatus.Off;
    }

    public override void Check(bool condition)
    {
        var oldValue = Status == TileStatus.On;
        if (oldValue != condition)
        {
            Status = condition ? TileStatus.On : TileStatus.Off;
        }
    }
}