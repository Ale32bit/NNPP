namespace NNPP.Models.Inputs;

public class AccelerationSwitch
{
    public enum SwitchPosition
    {
        Slow = 1,
        Medium = 2,
        Fast = 3,
    }

    public SwitchPosition Position { get; set; } = SwitchPosition.Medium;

    public int Value => (int)Position;
}
