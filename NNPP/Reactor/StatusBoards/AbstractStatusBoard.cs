namespace NNPP.Reactor.StatusBoards;

public abstract class AbstractStatusBoard
{
    public static readonly HashSet<string> ActiveAlarms = new(StringComparer.OrdinalIgnoreCase);
    
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
            if (tile.Status is TileStatus.AlarmActive or TileStatus.AlarmPassive && tile.Sfx != null)
            {
                set.Add(tile.Sfx);
            }
        }

        return set;
    }
}