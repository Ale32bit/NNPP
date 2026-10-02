using System.Globalization;

namespace NNPP.Models;

public class Metric : IParameter
{
    public string Label { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public int DecimalPlaces { get; set; } = 1;
    
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
        return Unit is null ? GetFormattedValue() : $"{GetFormattedValue()} {Unit}";
    }
    
    public string GetFormattedValue()
    {
        return Value.ToString($"{_format}{DecimalPlaces}");
    }
}