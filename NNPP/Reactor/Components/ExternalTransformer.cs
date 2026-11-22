using NNPP.Models.Metrics;

namespace NNPP.Reactor.Components;

public class ExternalTransformer : IRepairable
{
    public MetricBool Running { get; set; } = new("Transformer", true, "RUNNING", "BROKEN");
    
    public bool IsBroken() => !Running.Value;

    public void Repair()
    {
        Running.Value = true;
    }

    public void FullRepair()
    {
        Repair();
    }

    public void Break()
    {
        if(IsBroken()) return;
        Running.Value = false;
        Simulation.BreakComponent(this);
    }
    
    public double GetHealthPercentage()
    {
        return Running.Value ? 1.0 : 0.0;
    }
}