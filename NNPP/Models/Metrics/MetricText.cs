namespace NNPP.Models.Metrics;

public class MetricText(string label, string value) : IParameter
{
    public string Label { get; set; } = label;

    public string Value { get; set; } = value;

    public string GetValue()
    {
        return Value;
    }
}