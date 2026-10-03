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

    public MetricText ReliefValveStatus { get; } = new("Relief Valve Status", "0/0");

    public List<ReliefValveSwitch> ReliefValves =
    [
        new(), new(), new(), new()
    ];

    public bool Scramming { get; set; } = false;

    public bool Stalled { get; set; } = false;


    private double _extraHeat = 0; // meltdown. todo


    // EVENTS
    public event EventHandler<KeyPressEventArgs>? KeyPress;


    public event Action? Ticked;
    private readonly GameLoop _loop;
    private bool _started;

    public Simulation()
    {
        _loop = new GameLoop(20, Update, () => Ticked?.Invoke());

        CoolantValveMetric = new(CoolantValve, "Coolant Valves", "OPEN", "CLOSED");
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
        var targetLevel =  Parameters.FeedwaterLevelRate(TotalFeedwaterFlow.Value, need) +
                          (Parameters.ReliefValve.FeedwaterLevelReplenishRate * GetRunningReliefValves());
        FeedwaterLevel.Value = Math.Clamp(FeedwaterLevel.Value + targetLevel * dt, 0, 1);

        // Feedwater switch
        FeedwaterPump1.Utilization.Value += Parameters.FeedwaterSwitchRate(FeedwaterPump1.Switch.Value) * dt;
        FeedwaterPump2.Utilization.Value += Parameters.FeedwaterSwitchRate(FeedwaterPump2.Switch.Value) * dt;
        FeedwaterPump1.Utilization.Value = Math.Clamp(FeedwaterPump1.Utilization.Value, 0, 1);
        FeedwaterPump2.Utilization.Value = Math.Clamp(FeedwaterPump2.Utilization.Value, 0, 1);

        // Pressure

        Pressure.Value = Parameters.SteamPressure(ReactorTemperature.Value, FeedwaterLevel.Value);

        // Fuel

        Fuel.Value += Parameters.FuelRate(RodInsertion.Value, Running) * dt;

        // Relief valves
        ReliefValves.ForEach(rv => rv.Update(dt));
        ReliefValveStatus.Value = $"{ReliefValves.Count(rv => rv.IsOpen())}/{ReliefValves.Count}";

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
    }

    public double GetCoolantRate()
    {
        return Parameters.CoolantRate(CoolantPumpAlpha.Rpm.Value, CoolantPumpBeta.Rpm.Value, CoolantValve.Value);
    }

    public int GetRunningReliefValves()
    {
        return ReliefValves.Count(q => q.IsOpen());
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