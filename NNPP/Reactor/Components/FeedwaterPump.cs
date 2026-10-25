using NNPP.Models;
using NNPP.Models.Inputs;

namespace NNPP.Reactor.Components;

public class FeedwaterPump
{
    public double SecondsToDamage = 0;
    public Metric Rpm { get; } = new("RPM", 0) { ShowUnit = false, DecimalPlaces = 0 };

    public Metric Flow { get; } = new("Flow", 0, "m³/s")
    {
        DecimalPlaces = 2,
    };

    public MetricPercentage Utilization { get; } = new("Utilization", 0.8);

    public Metric Health { get; } = new("Health", 100, "/ 100")
    {
        DecimalPlaces = 0, 
        ShowUnit = true,
    };

    public RateSwitch Switch { get; } = new();

    public bool Powered { get; set; } = true;
    public bool Alive { get; set; } = true;

    public bool Running => Powered && Alive;
    
    public bool IsBroken() => !Alive;

    public int GetPowerDemand() => Parameters.FeedwaterPumpPowerDemand(Utilization.Value);

    public bool IsCavitating() => Rpm.Value > Parameters.Feedwater.MaxRpm * Parameters.Feedwater.CavitationLimit;

    public void Break()
    {
        if (IsBroken()) return;
        Alive = false;
        Health.Value = 0;
        Simulation.BreakComponent(this);
    }

    public void Repair()
    {
        var health = Math.Clamp((int)Health.Value + 25, 0, 100);
        Health.Value = health;
        if (health == 100 && !Alive)
        {
            Alive = true;
        }
    }
}