using NNPP.Models.Inputs;

namespace NNPP.Models.Metrics;

public class SwitchBoundMetric(Switch sw, string label, string trueValue, string falseValue) : MetricBool(label, sw.Value, trueValue, falseValue)
{
    public Switch Switch { get; set; } = sw;

    public override bool Value { get => Switch.Value; set => Switch.Value = value; }
}
