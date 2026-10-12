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

    public HashSet<string> GetActiveSfx()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tile in Layout)
        {
            if (tile.ShouldPlaySfx())
            {
                set.Add(tile.Sfx);
            }
        }

        return set;
    }
}