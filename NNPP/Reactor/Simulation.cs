using NNPP.Models;
using NNPP.Models.Inputs;
using NNPP.Models.Metrics;
using NNPP.Reactor.Components;

namespace NNPP.Reactor;

public class Simulation : IAsyncDisposable
{
    // COMPONENTS and VARIABLES
    public bool Running { get; set; } = true;
    public bool Igniting { get; set; } = false;

    public Metric ReactorTemperature { get; } = new("Reactor Temp", Parameters.Core.StallTemp, "K");
    public Metric Pressure { get; } = new("Pressure", Parameters.Pressure.Atm, "kPa");
    public MetricPercentage Fuel { get; } = new("Fuel", 1);

    public MetricPercentage RodInsertion { get; } = new("Rod Insertion", 1)
    {
        DecimalPlaces = 0,
    };

    public MetricPercentage FeedwaterLevel { get; } = new("Water Level", 1);
    public RodController RodControl { get; } = new();

    public CoolantPump CoolantPumpAlpha { get; } = new();
    public CoolantPump CoolantPumpBeta { get; } = new();
    public Switch CoolantValve { get; } = new();
    public SwitchBoundMetric CoolantValveMetric { get; init; }
    public MetricText CoolantPumpsStatus { get; } = new("Coolant Pumps Status", "0/2");

    public FeedwaterPump FeedwaterPump1 { get; } = new();
    public FeedwaterPump FeedwaterPump2 { get; } = new();

    public Metric TotalFeedwaterFlow { get; } = new("Feedwater Flow", 0, "m³/s")
    {
        DecimalPlaces = 2,
    };

    public Switch FeedwaterValve { get; } = new();
    public MetricText FeedwaterOverview { get; } = new("Feedwater", "0/0 ACTIVE");

    public MetricText ReliefValveStatus { get; } = new("Relief Valve Status", "0/0");

    public readonly List<ReliefValveSwitch> ReliefValves =
    [
        new(), new(), new(), new()
    ];

    public Turbine Turbine1 { get; } = new();
    public Turbine Turbine2 { get; } = new();

    public bool Scramming { get; set; } = false;

    public bool Stalled { get; set; } = false;


    private double _extraHeat = 0; // meltdown. todo


    // EVENTS
    public event EventHandler<KeyPressEventArgs>? KeyPress;
    public event EventHandler<double>? OnUpdate;

    public event Action? Ticked;
    private readonly GameLoop _loop;
    private bool _started;

    public Simulation()
    {
        _loop = new GameLoop(20, Update, () => Ticked?.Invoke());

        CoolantValveMetric = new(CoolantValve, "Coolant Valves", "OPEN", "CLOSED");

        Turbine1.SyncSwitch.Triggered += (sender, value) => AttemptTurbineSync(Turbine1);
        Turbine2.SyncSwitch.Triggered += (sender, value) => AttemptTurbineSync(Turbine2);
    }

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _loop.Start();
    }

    public void Update(double dt)
    {
        // coolant
        CoolantPumpAlpha.Rpm.Value =
            Parameters.StepCoolantRpm(CoolantPumpAlpha.Rpm.Value, CoolantPumpAlpha.Running, dt);
        CoolantPumpBeta.Rpm.Value = Parameters.StepCoolantRpm(CoolantPumpBeta.Rpm.Value, CoolantPumpBeta.Running, dt);
        var runningCoolantPumps = 0;
        runningCoolantPumps += CoolantPumpAlpha.Running ? 1 : 0;
        runningCoolantPumps += CoolantPumpBeta.Running ? 1 : 0;
        CoolantPumpsStatus.Value = $"{runningCoolantPumps}/2";

        // Feedwater flow
        FeedwaterPump1.Rpm.Value = Parameters.FeedwaterPumpStepRpm(FeedwaterPump1.Rpm.Value,
            FeedwaterPump1.Utilization.Value, FeedwaterPump1.Running, dt);
        FeedwaterPump2.Rpm.Value = Parameters.FeedwaterPumpStepRpm(FeedwaterPump2.Rpm.Value,
            FeedwaterPump2.Utilization.Value, FeedwaterPump2.Running, dt);

        FeedwaterPump1.Flow.Value = Parameters.FeedwaterPumpFlow(FeedwaterPump1.Rpm.Value);
        FeedwaterPump2.Flow.Value = Parameters.FeedwaterPumpFlow(FeedwaterPump2.Rpm.Value);
        TotalFeedwaterFlow.Value = Parameters.FeedwaterTotalFlow(FeedwaterPump1.Flow.Value, FeedwaterPump2.Flow.Value,
            FeedwaterValve.Value);

        var need = Parameters.FeedwaterNeed(ReactorTemperature.Value, Running);
        var targetLevel = Parameters.FeedwaterLevelRate(TotalFeedwaterFlow.Value, need) +
                          (Parameters.ReliefValve.FeedwaterLevelReplenishRate * GetRunningReliefValves());
        FeedwaterLevel.Value = Math.Clamp(FeedwaterLevel.Value + targetLevel * dt, 0, 1);

        // Feedwater switch
        FeedwaterPump1.Utilization.Value += Parameters.SwitchRate(FeedwaterPump1.Switch.Value) * dt;
        FeedwaterPump2.Utilization.Value += Parameters.SwitchRate(FeedwaterPump2.Switch.Value) * dt;
        FeedwaterPump1.Utilization.Value = Math.Clamp(FeedwaterPump1.Utilization.Value, 0, 1);
        FeedwaterPump2.Utilization.Value = Math.Clamp(FeedwaterPump2.Utilization.Value, 0, 1);

        FeedwaterOverview.Value = $"2/2 ACTIVE"; // todo: implement HP

        // Pressure

        Pressure.Value = Parameters.SteamPressure(ReactorTemperature.Value, FeedwaterLevel.Value);

        // Fuel

        Fuel.Value += Parameters.FuelRate(RodInsertion.Value, Running) * dt;

        // Relief valves
        ReliefValves.ForEach(rv => rv.Update(dt));
        ReliefValveStatus.Value = $"{ReliefValves.Count(rv => rv.IsOpen())}/{ReliefValves.Count}";

        // Turbines
        Turbine1.Valve.Value += Parameters.SwitchRate(Turbine1.RateSwitch.Value) * dt;
        Turbine2.Valve.Value += Parameters.SwitchRate(Turbine2.RateSwitch.Value) * dt;
        Turbine1.Valve.Value = Math.Clamp(Turbine1.Valve.Value, 0, 1);
        Turbine2.Valve.Value = Math.Clamp(Turbine2.Valve.Value, 0, 1);

        Turbine1.FlowRate.Value = Parameters.TurbineFlow(Turbine1.Valve.Value, ReactorTemperature.Value,
            FeedwaterLevel.Value, Turbine1.IsDestroyed());
        Turbine2.FlowRate.Value = Parameters.TurbineFlow(Turbine2.Valve.Value, ReactorTemperature.Value,
            FeedwaterLevel.Value, Turbine2.IsDestroyed());

        var turb1RpmTarget = Parameters.RpmTarget(Turbine1.FlowRate.Value, Turbine1.IsDestroyed());
        var turb1RpmTau = Parameters.RpmTau(Turbine1.AccelerationSwitch.Position, Turbine1.IsDestroyed());
        var turb1Accel = 0d;
        if (Turbine1.IsSynced())
        {
            Turbine1.Rpm.Value = Parameters.Turbine.SyncRpm;
        }
        else
        {
            turb1Accel = Parameters.RpmAccel(Turbine1.Rpm.Value, turb1RpmTarget, turb1RpmTau);
            Turbine1.Rpm.Value = Math.Max(0, Turbine1.Rpm.Value + turb1Accel * dt);
        }

        var turb2RpmTarget = Parameters.RpmTarget(Turbine2.FlowRate.Value, Turbine2.IsDestroyed());
        var turb2RpmTau = Parameters.RpmTau(Turbine2.AccelerationSwitch.Position, Turbine2.IsDestroyed());
        var turb2Accel = 0d;
        if (Turbine2.IsSynced())
        {
            Turbine2.Rpm.Value = Parameters.Turbine.SyncRpm;
        }
        else
        {
            turb2Accel = Parameters.RpmAccel(Turbine2.Rpm.Value, turb2RpmTarget, turb2RpmTau);
            Turbine2.Rpm.Value = Math.Max(0, Turbine2.Rpm.Value + turb2Accel * dt);
        }

        var vibTarget1 = Parameters.VibrationTarget(Turbine1.FlowRate.Value, turb1Accel);
        var vibTarget2 = Parameters.VibrationTarget(Turbine2.FlowRate.Value, turb2Accel);

        Turbine1.Vibration.Value = Parameters.StepVibration(Turbine1.Vibration.Value, vibTarget1, dt);
        Turbine2.Vibration.Value = Parameters.StepVibration(Turbine2.Vibration.Value, vibTarget2, dt);

        if (Turbine1.IsSynced())
        {
            Turbine1.Phase = 0;
        }
        else
        {
            Turbine1.Phase = (Turbine1.Phase + 6 * (Turbine1.Rpm.Value - Parameters.Turbine.SyncRpm) * dt) % 360;
        }

        if (Turbine2.IsSynced())
        {
            Turbine2.Phase = 0;
        }
        else
        {
            Turbine2.Phase = (Turbine2.Phase + 6 * (Turbine2.Rpm.Value - Parameters.Turbine.SyncRpm) * dt) % 360;
        }

        if (Running)
        {
            Stalled = RodInsertion.Value >= 1 && ReactorTemperature.Value <= Parameters.Core.StallTemp;
            if (!Stalled)
            {
                // heat rate
                ReactorTemperature.Value += Parameters.TemperatureRate(Fuel.Value, RodInsertion.Value, _extraHeat,
                    FeedwaterLevel.Value, GetCoolantRate(), GetRunningReliefValves(), Scramming) * dt;
            }

            if (RodControl.Position != RodController.ControlPosition.Neutral)
            {
                var sign = (int)RodControl.Position;
                var delta = Parameters.Core.RodSpeed * dt * sign;
                RodInsertion.Value += delta;

                if (RodInsertion.Value > 1)
                {
                    RodInsertion.Value = 1;
                }

                if (RodInsertion.Value < 0)
                {
                    RodInsertion.Value = 0;
                }
            }
        }
        else
        {
            ReactorTemperature.Value = Parameters.Core.StallTemp;
        }

        if (ReactorTemperature.Value < Parameters.Core.StallTemp)
        {
            ReactorTemperature.Value = Parameters.Core.StallTemp;
        }

        OnUpdate?.Invoke(this, dt);
    }

    public double GetCoolantRate()
    {
        return Parameters.CoolantRate(CoolantPumpAlpha.Rpm.Value, CoolantPumpBeta.Rpm.Value, CoolantValve.Value);
    }

    public int GetRunningReliefValves()
    {
        return ReliefValves.Count(q => q.IsOpen());
    }

    public void AttemptTurbineSync(Turbine turbine)
    {
        if (turbine.IsSynced())
        {
            turbine.Status.Value = Turbine.TurbineStatus.Desynced;
            turbine.SyncSwitch.RawSet(false);
        }
        else if (turbine.IsSyncPossible())
        {
            turbine.Status.Value = Turbine.TurbineStatus.Synced;
            turbine.SyncSwitch.RawSet(true);
        }
        else
        {
            turbine.SyncSwitch.RawSet(false);
        }
    }

    public void OnKeyPress(KeyPressEventArgs args)
    {
        KeyPress?.Invoke(this, args);
    }


    public async ValueTask DisposeAsync()
    {
        await _loop.DisposeAsync();
    }
}