using NNPP.Models.Inputs;

namespace NNPP.Models.Metrics;

public class SwitchBoundMetric(ISwitch sw, string label, string trueValue, string falseValue) : MetricBool(label, sw.Value, trueValue, falseValue)
{
    public ISwitch Switch { get; set; } = sw;

    public override bool Value { get => Switch.Value; set => Switch.Value = value; }
}
