using NNPP.Models;
using NNPP.Models.Inputs;
using NNPP.Pages.Components;

namespace NNPP.Reactor.Components;

public class Turbine
{
    public enum TurbineStatus
    {
        Desynced,
        Synced,
        Broken,
    }

    public double SecondsToDamage { get; set; } = 0;

    public MetricPercentage Valve { get; } = new("Valves", 0);

    public Metric FlowRate { get; } = new("Flow Rate", 0, "m³/s")
    {
        DecimalPlaces = 2,
    };

    public MetricEnum<TurbineStatus> Status { get; } = new("Status", TurbineStatus.Desynced);

    public Metric Rpm { get; } = new("Turbine RPM", 0, "rpm")
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };

    public Metric Vibration { get; } = new("Vibration", 0, "µm")
    {
        DecimalPlaces = 0,
        ValueOverride = "N/A", // TODO: fix vibration
    };

    public Metric Health { get; } = new("Health", 100, "/ 100")
    {
        DecimalPlaces = 0, 
        ShowUnit = true,
    };

    public bool IsSynced() => Status.Value == TurbineStatus.Synced;
    public bool IsBroken() => Status.Value == TurbineStatus.Broken;
    public bool CanRun() => !IsBroken();

    public RateSwitch RateSwitch { get; } = new();
    public AccelerationSwitch AccelerationSwitch { get; } = new();
    public TriggerSwitch SyncSwitch { get; } = new();

    public double Phase { get; set; } = Random.Shared.NextDouble() * 360;

    public bool IsSyncPossible() =>
        LitDot(Phase) == 0 && CanRun() && RpmSyncDifference() < Parameters.Turbine.SyncRpmTolerance;

    public double RpmSyncDifference() => Math.Abs(Parameters.Turbine.SyncRpm - Rpm.Value);

    public void Break()
    {
        Health.Value = 0;
        Status.Value = TurbineStatus.Broken;
    }

    public void Repair()
    {
        var health = Math.Clamp((int)Health.Value + 25, 0, 100);
        Health.Value = health;
        if (health == 100 && Status.Value == TurbineStatus.Broken)
        {
            Status.Value = TurbineStatus.Desynced;
        }
    }

    public static int LitDot(double phase)
    {
        return ((int)Math.Round(phase / 12) % Synchroscope.Dots + Synchroscope.Dots) % Synchroscope.Dots;
    }
}