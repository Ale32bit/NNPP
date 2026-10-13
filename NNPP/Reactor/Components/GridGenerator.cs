using NNPP.Models;

namespace NNPP.Reactor.Components;

public class GridGenerator(Simulation sim)
{
    public enum GeneratorStatus
    {
        Off,
        On,
        Broken,
    }

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
    public bool Broken => Status == GeneratorStatus.Broken;

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
    
    public void Repair()
    {
        var health = Math.Clamp((int)Health.Value + 25, 0, 100);
        Health.Value = health;
        if (health == 100 && Status == GeneratorStatus.Broken)
        {
            Status = GeneratorStatus.Off;
        }
    }

    public void Refuel()
    {
        Fuel.Value = Math.Clamp(Fuel.Value + 0.25d, 0, 1);
    }
}