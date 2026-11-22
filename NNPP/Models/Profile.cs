using NNPP.Reactor;

namespace NNPP.Models;

public class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Experience { get; set; }
    
    // Randomly break external transformers
    public bool EnableHostiles { get; set; } = true;
    
    // Automatically repair components
    public bool EnableAutoPm { get; set; } = true;
    public bool ShowGameOverScreen { get; set; } = true;
    public bool LowPerformance { get; set; } = false;
    public int Tickrate { get; set; } = Simulation.DefaultTickrate;
}