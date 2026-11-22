using NNPP.Models;
using NNPP.Models.Metrics;

namespace NNPP.Reactor.Components;

public class GridGenerator(Simulation sim) : IRepairable
{
    public enum GeneratorStatus
    {
        Off,
        On,
        Broken,
    }

    private bool _wasActive;

    public GeneratorStatus Status { get; set; } = GeneratorStatus.Off;

    public Metric Health { get; } = new("Health", 100)
    {
        DecimalPlaces = 0, 
    };

    public MetricPercentage Fuel { get; } = new("Fuel", 1);

    public Metric Power { get; } = new("Gen. Output", 0, "kW")
    {
        DecimalPlaces = 0,
        ShowUnit = false,
    };
    
    public bool Running => Status == GeneratorStatus.On;
    public bool IsBroken() => Status == GeneratorStatus.Broken;

    public double GetOutput()
    {
        return Running ? Parameters.Grid.GeneratorSupply + GetGeneratorNoise() : 0;
    }

    private int GetGeneratorNoise()
    {
        var seed = (int)((double)sim.Tick / sim.Tickrate * 10) * 1000;
        var rng = new Random(seed);
        var wave = Math.Sin(0.5 * sim.Runtime * Math.PI * rng.NextDouble());
        return (int)(wave * rng.NextDouble() * Parameters.Grid.GenPowerNoiseMax);
    }

    public void Break()
    {
        if(IsBroken()) return;
        _wasActive = Running;
        Health.Value = 0;
        Status = GeneratorStatus.Broken;
        Simulation.BreakComponent(this);
    }

    public void Repair()
    {
        var health = Math.Clamp((int)Health.Value + 25, 0, 100);
        Health.Value = health;
        if (health == 100 && Status == GeneratorStatus.Broken)
        {
            Status = _wasActive ? GeneratorStatus.On : GeneratorStatus.Off;
        }
    }

    public void FullRepair()
    {
        Health.Value = 100;
        Status = _wasActive ? GeneratorStatus.On : GeneratorStatus.Off;
    }

    public bool CanRefuel()
    {
        return Fuel.Value < 0.7;
    }

    public void Refuel()
    {
        Fuel.Value = Math.Clamp(Fuel.Value + 0.3, 0, 1);
    }
    
    public double GetHealthPercentage()
    {
        return Health.Value / 100;
    }
}