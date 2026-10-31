namespace NNPP.Models;

public class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Experience { get; set; }
    
    // Randomly break external transformers
    public bool EnableHostiles { get; set; } = true;
    public bool ShowGameOverScreen { get; set; } = true;
}