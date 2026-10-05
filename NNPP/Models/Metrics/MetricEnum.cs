namespace NNPP.Models;

public class MetricEnum<T>(string label, T initialValue) : IParameter where T : struct, IConvertible
{
    public string Label { get; set; } = label;
    public T Value { get; set; } = initialValue;
    

    public string GetValue()
    {
        return Value.ToString()?.ToUpperInvariant() ?? "ERROR";
    }
    
    
}