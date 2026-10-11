namespace NNPP.Reactor.StatusBoards;

public enum TileColor
{
    Green,
    Blue,
    Yellow,
    Red,
}

public static class TileColorExtension
{
    public static string Name(this TileColor status)
    {
        return status.ToString().ToLowerInvariant();
    }
}