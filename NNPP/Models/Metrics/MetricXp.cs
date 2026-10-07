namespace NNPP.Models.Metrics;

public class MetricXp(string label, int experience) : IParameter
{
    public string Label { get; set; } = label;
    public int Value { get; set; } = experience;

    public string GetValue()
    {
        return ToPrefixedValue(Value);
    }

    public static string ToPrefixedValue(double d)
    {
        char[] incPrefixes = new[] { 'k', 'M', 'G', 'T', 'P', 'E', 'Z', 'Y' };
        char[] decPrefixes = new[] { 'm', '\u03bc', 'n', 'p', 'f', 'a', 'z', 'y' };

        int degree = (int)Math.Floor(Math.Log10(Math.Abs(d)) / 3);
        double scaled = d * Math.Pow(1000, -degree);

        char? prefix = null;
        switch (Math.Sign(degree))
        {
            case 1:  prefix = incPrefixes[degree - 1]; break;
            case -1: prefix = decPrefixes[-degree - 1]; break;
        }

        return scaled.ToString("F1") + prefix;
    }
}