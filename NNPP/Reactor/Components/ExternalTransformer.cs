using NNPP.Models;
using NNPP.Models.Metrics;

namespace NNPP.Reactor.Components;

public class ExternalTransformer
{
    public MetricBool Running { get; set; } = new("Transformer", true, "RUNNING", "BROKEN");
    
    public bool IsBroken() => !Running.Value;
    
    public void Break()
    {
        if(IsBroken()) return;
        Running.Value = false;
        Simulation.BreakComponent(this);
    }
}