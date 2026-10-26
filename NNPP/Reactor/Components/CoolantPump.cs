using NNPP.Models;
using NNPP.Models.Inputs;
using NNPP.Models.Metrics;

namespace NNPP.Reactor.Components;

public class CoolantPump
{
    public Metric Rpm { get; } = new("RPM", 0, "RPM");
    public MetricBool Status { get; } = new("Status", false, "RUNNING", "NO POWER");
    public Switch Switch { get; } = new();

    public bool Powered { get; set; } = true;
    public bool Started => Switch.Value;

    public bool Running => Started && Powered;

    public int GetPowerDemand() => Parameters.Grid.CoolantPumpDemand;

    public bool Runup() => Running && Rpm.Value < Parameters.CoolantPump.MaxRpm;
    public bool Ready() => Running && !Runup();
}