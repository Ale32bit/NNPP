namespace NNPP.Models.Inputs;

public class ScramButton
{
    public event Action? Engaged;
    
    public bool Enabled { get; private set; } = false;
    public bool Available { get; set; } = false;

    public void Enable()
    {
        if (Available && !Enabled)
        {
            Enabled = true;
            Engaged?.Invoke();
        }
    }
}