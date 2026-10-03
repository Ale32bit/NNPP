namespace NNPP.Models.Inputs;

public class RodController
{
    public enum ControlPosition
    {
        Lowering = 1,
        Neutral = 0,
        Raising = -1,
    }

    public ControlPosition Position { get; set; } = ControlPosition.Neutral;
    public bool Locked { get; set; } = false;
}