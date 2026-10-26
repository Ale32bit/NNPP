namespace NNPP.Models.Metrics;

public class Metric(string label, double initialValue, string? unit = null, string format = "F") : IParameter
{
    public string Label { get; set; } = label;
    public double Value { get; set; } = initialValue;
    public string? Unit { get; set; } = unit;
    public int DecimalPlaces { get; set; } = 1;
    public bool ShowUnit { get; set; } = true;
    public string? ValueOverride { get; set; }


    public string GetValue()
    {
        if (ValueOverride is not null)
        {
            return ValueOverride;
        }


        return Unit is null || !ShowUnit ? GetFormattedValue() : $"{GetFormattedValue()} {Unit}";
    }

    public string GetFormattedValue()
    {
        return Value.ToString($"{format}{DecimalPlaces}");
    }
}