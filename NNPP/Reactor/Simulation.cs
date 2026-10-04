using NNPP.Models;
using NNPP.Models.Inputs;
using NNPP.Models.Metrics;
using NNPP.Pages.Components;
using NNPP.Reactor.Components;
using AuthButton = NNPP.Models.Inputs.AuthButton;

namespace NNPP.Reactor;

public class Simulation : IAsyncDisposable
{
    private AudioManager Audio { get; set; }

    // COMPONENTS and VARIABLES
    public bool Running { get; set; } = false;
    public bool Ignited { get; set; } = false;

    public MetricEnum<ReactorStatus> ReactorStatus { get; } = new("Reactor Status", Models.ReactorStatus.Stalled);

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

    public AuthButton ScramButton { get; } = new();

    public MetricBool AuthBravo8 { get; } = new("Auth Bravo-8", false, "Authorized", "Unauthorized");
    public MetricBool AuthShutdownPumps { get; } = new("Shutdown Pumps", false, "Enabled", "Disabled");

    public AuthButton IgnitionButton { get; } = new();
    public TriggerSwitch IgnitionAuthBravo8 { get; } = new();
    public TriggerSwitch IgnitionShutdownPumps { get; } = new();

    public bool Stalled { get; set; } = false;

    public enum MeltdownStage
    {
        None,
        Start,
        Overheating,
        Explosion1,
        MeltdownNotify,
        ShutdownNotify,
        EnableScram,
        TemperatureCheck,
    }

    private bool _reactorOverheat = false;
    private double _extraHeat = 0;
    private bool _meltdown = false;
    private double _meltdownTime = 0;
    private MeltdownStage _meltdownStage = MeltdownStage.Start;
    private bool _scramRodWillFail = false;

    private bool _scramRodFailed = false;

    // it's anrover :pray:
    private bool _notgreatnotterrible = false;
    private bool _forceMeltdown = false;

    private double _ignitionTime = 0;
    private bool _igniting = false;

    // EVENTS
    public event EventHandler<KeyPressEventArgs>? KeyPress;
    public event EventHandler<double>? OnUpdate;
    public event EventHandler<Notification>? OnNotification;

    public event Action? Ticked;
    private readonly GameLoop _loop;
    private bool _started;
    private bool _firstTick = true;

    public Simulation(AudioManager audioManager)
    {
        Audio = audioManager;
        _loop = new GameLoop(20, Update, () => Ticked?.Invoke());

        CoolantValveMetric = new(CoolantValve, "Coolant Valves", "OPEN", "CLOSED");

        Turbine1.SyncSwitch.Triggered += (sender, value) => AttemptTurbineSync(Turbine1);
        Turbine2.SyncSwitch.Triggered += (sender, value) => AttemptTurbineSync(Turbine2);

        ScramButton.Engaged += OnScramEngage;

        IgnitionAuthBravo8.Triggered += (_, value) => OnIgnitionButtons(IgnitionAuthBravo8, value);
        IgnitionShutdownPumps.Triggered += (_, value) => OnIgnitionButtons(IgnitionShutdownPumps, value);
        IgnitionButton.Engaged += AttemptIgnition;

        KeyPress += (_, key) =>
        {
            if (key.Key == "m")
            {
                _forceMeltdown = true;
            }
        };
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
        if (_firstTick)
        {
            OnFirstTick();
            _firstTick = false;
        }


        if (_igniting)
        {
            ReactorStatus.Value = Models.ReactorStatus.Igniting;
        }
        else if (_meltdown)
        {
            if (_notgreatnotterrible)
            {
                ReactorStatus.Value = Models.ReactorStatus.Error;
            }
            else
            {
                if (ReactorTemperature.Value <= 323)
                {
                    ReactorStatus.Value = Models.ReactorStatus.Offline;
                }
                else
                {
                    ReactorStatus.Value = Models.ReactorStatus.Critical;
                }
            }
        }
        else
        {
            if (Running)
            {
                ReactorStatus.Value = ReactorTemperature.Value switch
                {
                    <= 323 => Models.ReactorStatus.Stalled,
                    > 323 and < 2400 => Models.ReactorStatus.Running,
                    >= 2400 => Models.ReactorStatus.Overheat,
                    _ => Models.ReactorStatus.Error,
                };
            }
            else
            {
                ReactorStatus.Value = Models.ReactorStatus.Offline;
            }
        }

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
        if (_notgreatnotterrible)
        {
            Pressure.Value = 0;
        }

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
            if (Turbine1.FlowRate.Value < Parameters.Turbine.FlowMin)
            {
                Turbine1.SyncSwitch.Value = false;
            }
        }
        else
        {
            Turbine1.Phase = (Turbine1.Phase + 6 * (Turbine1.Rpm.Value - Parameters.Turbine.SyncRpm) * dt) % 360;
        }

        if (Turbine2.IsSynced())
        {
            Turbine2.Phase = 0;
            if (Turbine2.FlowRate.Value < Parameters.Turbine.FlowMin)
            {
                Turbine2.SyncSwitch.Value = false;
            }
        }
        else
        {
            Turbine2.Phase = (Turbine2.Phase + 6 * (Turbine2.Rpm.Value - Parameters.Turbine.SyncRpm) * dt) % 360;
        }

        if (Running)
        {
            Stalled = (RodInsertion.Value >= 1 && ReactorTemperature.Value <= Parameters.Core.StallTemp) && !_meltdown;
            if (!Stalled)
            {
                // heat rate
                ReactorTemperature.Value += Parameters.TemperatureRate(Fuel.Value, RodInsertion.Value, _extraHeat,
                    FeedwaterLevel.Value, GetCoolantRate(), GetRunningReliefValves(), ScramButton.Enabled) * dt;
            }

            if (RodControl.Position != RodController.ControlPosition.Neutral && !_scramRodFailed)
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

            if (ReactorTemperature.Value >= 2400 && !_reactorOverheat && !_meltdown)
            {
                Audio.PlayMusicAsync(AudioKeys.Music.Overheat, 0.25);
                _reactorOverheat = true;
                Notify(new("Reactor overheat",
                    "The Reactor is above safe operating parameters. Lower temperature immediately.", true));
            }

            if (ReactorTemperature.Value < 2000 && _reactorOverheat)
            {
                if (!_meltdown)
                {
                    Audio.StopMusicAsync();
                }

                _reactorOverheat = false;
            }

            if ((ReactorTemperature.Value >= Parameters.Core.MeltdownTemperature || _forceMeltdown) && !_meltdown)
            {
                _meltdown = true;
                _meltdownStage = MeltdownStage.None;
            }

            if (_meltdown)
            {
                _meltdownTime += dt;

                if (_meltdownStage < MeltdownStage.Start)
                {
                    _meltdownStage = MeltdownStage.Start;
                    Audio.PlaySfxLoopAsync(AudioKeys.Sfx.MeltdownAlarm);
                    Audio.PlaySfxAsync(AudioKeys.Sfx.AnnouncerMeltdown);
                }

                if (_meltdownTime >= 3 && _meltdownStage < MeltdownStage.Overheating)
                {
                    _meltdownStage = MeltdownStage.Overheating;
                    Audio.PlayMusicAsync(AudioKeys.Music.Meltdown, 1, loop: false);
                    Notify(new("Reactor overheat",
                        "The Reactor is above safe operating parameters. Lower temperature immediately.", true));
                }

                if (_meltdownTime >= 10 && _meltdownStage < MeltdownStage.Explosion1)
                {
                    _meltdownStage = MeltdownStage.Explosion1;
                    Audio.PlaySfxAsync(AudioKeys.Sfx.MeltdownExplosion);
                }

                if (_meltdownTime >= 19 && !ScramButton.Enabled && _meltdownStage < MeltdownStage.MeltdownNotify)
                {
                    _meltdownStage = MeltdownStage.MeltdownNotify;
                    _extraHeat = Parameters.Core.MeltdownExtraHeat;
                    Audio.StopSfxAsync(AudioKeys.Sfx.MeltdownAlarm);
                    Notify(new("Reactor meltdown", "All Non-Reactor Operations staff are to evacuate.", true));
                }

                if (_meltdownTime >= 29 && _meltdownStage < MeltdownStage.ShutdownNotify)
                {
                    _meltdownStage = MeltdownStage.ShutdownNotify;
                    Notify(new("Reactor shutdown",
                        "An official emergency has been declared. Emergency options are now available.", true));
                }

                if (_meltdownTime >= 44 && !ScramButton.Available && _meltdownStage < MeltdownStage.EnableScram)
                {
                    _meltdownStage = MeltdownStage.EnableScram;
                    Audio.PlaySfxAsync(AudioKeys.Sfx.RedTrigger);
                    ScramButton.Available = true;
                }

                if (ScramButton.Enabled && RodInsertion.Value < 1)
                {
                    if (RodInsertion.Value < 0.8)
                    {
                        _scramRodWillFail = true;
                    }

                    if (!_scramRodFailed)
                    {
                        RodInsertion.Value += Parameters.Core.RodSpeedScram * dt;
                    }


                    if (_scramRodWillFail && RodInsertion.Value >= 0.8)
                    {
                        _scramRodFailed = true;
                        RodInsertion.Value = 0;
                        RodInsertion.ValueOverride = "ERR";
                        _extraHeat = 57;
                        Notify(new("Reactor SCRAM", "Control rods have sustained damage. SCRAM sequence has failed.",
                            true));
                    }
                }

                if (_meltdownTime >= 234 && _meltdownStage < MeltdownStage.TemperatureCheck)
                {
                    _meltdownStage = MeltdownStage.TemperatureCheck;

                    if (ReactorTemperature.Value < 900)
                    {
                        Notify(new("Reactor shutdown",
                            "Temperature has returned to safe operating parameters. Full shutdown in progress.", true));
                        Audio.PlayMusicAsync(AudioKeys.Music.Shutdown, 1, loop: false);
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            await Audio.PlaySfxAsync(AudioKeys.Sfx.MetalCry);
                            await Audio.PlayMusicAsync(AudioKeys.Music.Evacuate, 1, loop: false);
                            await Task.Delay(6000);
                            await Audio.PlaySfxAsync(AudioKeys.Sfx.ReactorExplosion);
                            Notify(new("Reactor meltdown",
                                "Reactor continues to be in a critical state. Full Evacuation in progress.", true));
                            _extraHeat = 57;
                            _notgreatnotterrible = true;
                        });
                    }
                }
            }
        }
        else if (Ignited && _igniting)
        {
            _ignitionTime += dt;
            ReactorTemperature.Value =
                Math.Clamp(Parameters.Core.IgnitionRate * _ignitionTime + Parameters.Core.StallTemp,
                    Parameters.Core.StallTemp, 650);
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

    private void OnFirstTick()
    {
        RodControl.Locked = true;

        Audio.PreloadAsync(AudioKeys.Sfx.MetalCry, AudioKeys.Sfx.ReactorExplosion);
        Audio.PreloadAsync(AudioKeys.Music.Overheat, AudioKeys.Music.Shutdown, AudioKeys.Music.Evacuate,
            AudioKeys.Music.Meltdown);

        Notify(new Notification("Welcome to NNPPRS", "Please report any bug!", Silent: true));
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
            Audio.PlaySfxAsync(AudioKeys.Sfx.ControlDenied);
        }
    }

    public void OnIgnitionButtons(TriggerSwitch el, bool value)
    {
        if (Ignited)
        {
            Audio.PlaySfxAsync(AudioKeys.Sfx.ControlDenied);
            el.RawSet(true);
            return;
        }

        if (value)
        {
            Audio.PlaySfxAsync(AudioKeys.Sfx.Authorize);
        }

        if (el == IgnitionAuthBravo8)
        {
            AuthBravo8.Value = value;
        }
        else if (el == IgnitionShutdownPumps)
        {
            AuthShutdownPumps.Value = value;
        }

        IgnitionButton.Available = IgnitionAuthBravo8.Value && IgnitionShutdownPumps.Value;
    }

    public void AttemptIgnition()
    {
        if (Running)
        {
            Audio.PlaySfxAsync(AudioKeys.Sfx.ControlDenied);
            return;
        }

        if (IgnitionAuthBravo8.Value && IgnitionShutdownPumps.Value)
        {
            Ignited = true;
            Audio.PlaySfxAsync(AudioKeys.Sfx.RedTrigger);

            Task.Run(async () =>
            {
                await Audio.PlaySfxAsync(AudioKeys.Music.Ignition);
                await Task.Delay(TimeSpan.FromSeconds(8.6));
                Notify(new Notification("Reactor Ignition",
                    "The Facility Reactor is currently being ignited. Standby"));
                await Task.Delay(TimeSpan.FromSeconds(2.4));
                _igniting = true;
                await Task.Delay(TimeSpan.FromSeconds(43));
                Notify(new Notification("Reactor ignition", "Reactor online. Code Bravo-8 is now in effect."));
                RodControl.Locked = false;
                Running = true;
                _igniting = false;
            });
        }
        else
        {
            Audio.PlaySfxAsync(AudioKeys.Sfx.ControlDenied);
        }
    }

    public void Notify(Notification notification)
    {
        OnNotification?.Invoke(this, notification);
    }

    public void OnScramEngage()
    {
        if (!_notgreatnotterrible)
        {
            _extraHeat = 0;
        }

        RodControl.Locked = true;
        Audio.PlaySfxLoopAsync(AudioKeys.Sfx.ScramActive);
        Notify(new Notification("Reactor scram", "SCRAM sequence engaged.", true));
    }

    public void OnKeyPress(KeyPressEventArgs args)
    {
        KeyPress?.Invoke(this, args);
    }

    public void TriggerMeltdown()
    {
        _forceMeltdown = true;
    }

    public async ValueTask DisposeAsync()
    {
        await _loop.DisposeAsync();
    }
}