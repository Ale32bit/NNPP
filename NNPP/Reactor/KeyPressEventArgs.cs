namespace NNPP.Reactor;

public class KeyPressEventArgs(string key, string code, bool ctrl, bool shift, bool alt) : EventArgs
{
    public string Key { get; } = key;
    public string Code { get; } = code;
    public bool Ctrl { get; } = ctrl;
    public bool Shift { get; } = shift;
    public bool Alt { get; } = alt;
}
