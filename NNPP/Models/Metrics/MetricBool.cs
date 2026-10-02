namespace NNPP.Models;

public class MetricBool : IParameter
{
    public string Label { get; set; }
    public virtual bool Value { get; set; }

    private readonly string _true;
    private readonly string _false;
    
    public MetricBool(string label, bool initialValue, string trueValue, string falseValue)
    {
        Value = initialValue;
        _true = trueValue;
        _false = falseValue;
        Label = label;
    }
    
    
    public string GetValue()
    {
        return Value ? _true : _false;
    }
}