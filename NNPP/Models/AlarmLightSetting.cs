namespace NNPP.Models;

public class AlarmLightSetting
{
    public bool Enable { get; set; }
    public required string Color { get; set; }
    public int Interval { get; set; }

    public static readonly AlarmLightSetting None = new()
    {
        Enable = false,
        Color = "black",
        Interval = 10,
    };
    
    public static readonly AlarmLightSetting Warn = new()
    {
        Enable = true,
        Color = "orange",
        Interval = 10,
    };
    
    public static readonly AlarmLightSetting Critical = new()
    {
        Enable = true,
        Color = "red",
        Interval = 5,
    };

}