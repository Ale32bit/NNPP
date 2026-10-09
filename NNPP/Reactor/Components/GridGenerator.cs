namespace NNPP.Reactor;

public class GridGenerator
{
    public enum GeneratorStatus
    {
        Off,
        On,
        Broken,
    }
    
    public GeneratorStatus Status { get; set; } = GeneratorStatus.On;
    public double Fuel { get; set; } = 1d;
    public bool Running => Status == GeneratorStatus.On;
    public bool Broken => Status == GeneratorStatus.Broken;
}