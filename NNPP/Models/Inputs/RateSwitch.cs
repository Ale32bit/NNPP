namespace NNPP.Models.Inputs;

public class RateSwitch
{
    public enum SwitchPosition
    {
        MinusMinus = -2,
        Minus = -1,
        Neutral = 0,
        Plus = 1,
        PlusPlus = 2,
    }

    public SwitchPosition Position { get; set; } = SwitchPosition.Neutral;

    public int Value => (int)Position;
}
