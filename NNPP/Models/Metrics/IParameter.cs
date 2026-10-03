namespace NNPP.Models;

public interface IParameter
{
    public string Label { get; set; }
    public string GetValue();
}