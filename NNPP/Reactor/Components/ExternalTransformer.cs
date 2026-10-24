using NNPP.Models;

namespace NNPP.Reactor.Components;

public class ExternalTransformer
{
    public MetricBool Running { get; set; } = new("Transformer", true, "RUNNING", "BROKEN");
}