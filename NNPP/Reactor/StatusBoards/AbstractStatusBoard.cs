namespace NNPP.Reactor.StatusBoards;

public abstract class AbstractStatusBoard
{
    public int Width => Layout.GetLength(1);
    public int Height => Layout.GetLength(0);

    public abstract AbstractTile[,] Layout { get; }

    public void Acknowledge()
    {
        foreach (var tile in Layout)
        {
            tile.Ack();
        }
    }
}