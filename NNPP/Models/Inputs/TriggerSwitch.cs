namespace NNPP.Models.Inputs;

public class TriggerSwitch(bool value) : ISwitch
{
    public event EventHandler<bool>? Triggered;

    public bool Value
    {
        get => _value;
        set
        {
            _value = value;
            Triggered?.Invoke(this, value);
        }
    }
    
    private bool _value = value;

    public TriggerSwitch() : this(false)
    {
    }

    public void RawSet(bool newValue)
    {
        _value = newValue;
    }
}