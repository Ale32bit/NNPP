namespace NNPP.Models;

public class Metric
{
    public string DisplayName { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public int DecimalPlaces { get; set; } = 1;

    public Metric(string displayName, double initialValue, string? unit = null)
    {
        DisplayName = displayName;
        Value = initialValue;
        Unit = unit;
    }

    public string PrettyValue => Value.ToString($"F{DecimalPlaces}");
    public string ValueDisplay => Unit is null ? PrettyValue : $"{PrettyValue} {Unit}";
}