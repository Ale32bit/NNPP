namespace NNPP.Models.Metrics;

public interface IParameter
{
    public string Label { get; set; }
    public string GetValue();
}