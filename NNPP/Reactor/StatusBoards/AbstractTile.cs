using System.Diagnostics.CodeAnalysis;

namespace NNPP.Reactor.StatusBoards;

public abstract class AbstractTile(
    string label,
    TileColor color = TileColor.Yellow,
    string? sfx = null,
    bool clearSfx = true)
{
    public string Label { get; set; } = label;
    public TileColor Color { get; set; } = color;
    public TileStatus Status { get; set; } = TileStatus.Off;
    public string? Sfx { get; set; } = sfx;
    public bool ClearSfx { get; set; } = clearSfx;

    /// <summary>
    /// Player acknowledges the alarm.
    /// </summary>
    public abstract void Ack();

    /// <summary>
    /// An event triggers the status and turns on.
    /// </summary>
    public abstract void Trigger();

    /// <summary>
    /// An event automatically clears status and turns off.
    /// </summary>
    public abstract void Reset();

    public abstract void Check(bool condition);

    [MemberNotNullWhen(true, nameof(Sfx))]
    public bool ShouldPlaySfx()
    {
        if (ClearSfx)
        {
            return Status is TileStatus.AlarmActive or TileStatus.AlarmPassive && Sfx != null;
        }

        return Status != TileStatus.Off && Sfx != null;
    }
}