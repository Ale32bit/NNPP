namespace NNPP.Models;

public enum ReactorStatus
{
    Offline, // Reactor is offline, shutdown successful, or not yet ignited
    Heating, // Igniting
    Cold, // 323K
    Stable, // >323K
    Overheat, // >2400
    Runaway, // During meltdown
    Error, // Shutdown failure
}