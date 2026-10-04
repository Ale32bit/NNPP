using System.Globalization;

namespace NNPP.Models;

public class Metric : IParameter
{
    public string Label { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public int DecimalPlaces { get; set; } = 1;
    public bool ShowUnit { get; set; } = true;
    public string? ValueOverride { get; set; }

    private string _format = "F";

    public Metric(string label, double initialValue, string? unit = null, string format = "F")
    {
        Label = label;
        Value = initialValue;
        Unit = unit;
        _format = format;
    }


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
        return Value.ToString($"{_format}{DecimalPlaces}");
    }
}