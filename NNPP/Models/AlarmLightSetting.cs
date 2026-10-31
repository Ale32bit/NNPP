namespace NNPP.Models;

public class AlarmLightSetting
{
    public bool Enable { get; set; }
    public required string Color { get; set; }
    public double Interval { get; set; }

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
    
    public static readonly AlarmLightSetting Meltdown = new()
    {
        Enable = true,
        Color = "red",
        Interval = 2.33,
    };

}