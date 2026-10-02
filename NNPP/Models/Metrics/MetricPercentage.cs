namespace NNPP.Models;

public class MetricPercentage : Metric
{
    public MetricPercentage(string displayName, double initialValue) : base(displayName, initialValue, null, "P")
    { }
}