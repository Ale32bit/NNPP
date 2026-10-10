using NNPP.Models;
using NNPP.Models.Inputs;

namespace NNPP.Reactor.Components;

public class FeedwaterPump
{
    // todo: research rates
    public Metric Rpm { get; } = new("RPM", 0) { ShowUnit = false, DecimalPlaces = 0 };

    public Metric Flow { get; } = new("Flow", 0, "m³/s")
    {
        DecimalPlaces = 2,
    };

    public MetricPercentage Utilization { get; } = new("Utilization", 0.8);

    public RateSwitch Switch { get; } = new();

    public bool Powered { get; set; } = true;
    public bool Alive { get; set; } = true;

    public bool Running => Powered && Alive;

    public int GetPowerDemand() => Parameters.FeedwaterPumpPowerDemand(Utilization.Value);
    
    public bool IsCavitating() => Flow.Value > Parameters.Feedwater.CavitationLimit;

}