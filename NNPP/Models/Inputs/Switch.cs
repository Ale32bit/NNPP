namespace NNPP.Models.Inputs;

public class Switch(bool value)
{
    public bool Value { get; set; } = value;

    public Switch() : this(false)
    {
    }
}
