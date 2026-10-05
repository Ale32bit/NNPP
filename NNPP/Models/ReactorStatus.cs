namespace NNPP.Models;

public enum ReactorStatus
{
    Stalled, // 323K
    Running, // >323K
    Overheat, // >2400
    Meltdown, // During meltdown
    Error, // Shutdown failure
    Offline, // Reactor is offline, shutdown successful, or not yet ignited
    Heating, // Igniting
}