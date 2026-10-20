namespace NNPP.Models;

public enum ReactorStatus
{
    Cold, // 323K
    Stable, // >323K
    Overheat, // >2400
    Emergency, // During meltdown
    Error, // Shutdown failure
    Offline, // Reactor is offline, shutdown successful, or not yet ignited
    Heating, // Igniting
}