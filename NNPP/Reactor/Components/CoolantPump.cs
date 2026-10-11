using NNPP.Models;

namespace NNPP.Reactor.Components;

public class CoolantPump
{
    public Metric Rpm { get; } = new("RPM", 0, "RPM");

    public bool Powered { get; set; } = true;
    public bool Started { get; set; } = true;

    public bool Running => Started && Powered;
    
    
    public int GetPowerDemand() => Parameters.Grid.CoolantPumpDemand;

    public bool Runup() => Running && Rpm.Value < Parameters.CoolantPump.MaxRpm;
    public bool Ready() => Running && !Runup();
}