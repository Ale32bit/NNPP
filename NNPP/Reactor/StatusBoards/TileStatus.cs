namespace NNPP.Reactor.StatusBoards;

public enum TileStatus
{
    Off,
    AlarmActive,
    AlarmPassive,
    On,
}

public static class TileStatusExtension
{
    public static string Name(this TileStatus status)
    {
        return status.ToString().ToLowerInvariant();
    }
}