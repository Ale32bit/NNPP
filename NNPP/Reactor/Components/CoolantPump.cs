using NNPP.Models.Inputs;
using NNPP.Models.Metrics;

namespace NNPP.Reactor.Components;

// Not really a repairable component, but it's used as a toggle for auto PM
public class CoolantPump : IRepairable
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
    
    public void Repair()
    {
        Switch.Value = true;
    }

    public void FullRepair()
    {
        Repair();
    }

    public void Break()
    {
        Switch.Value = false;
    }

    public bool IsBroken()
    {
        return !Switch.Value;
    }

    public double GetHealthPercentage()
    {
        return IsBroken() ? 0 : 1;
    }
}