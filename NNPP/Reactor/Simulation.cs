using System.Security.Cryptography;
using NNPP.Models;
using NNPP.Models.Inputs;
using NNPP.Models.Metrics;
using NNPP.Reactor.Components;
using AuthButton = NNPP.Models.Inputs.AuthButton;

namespace NNPP.Reactor;

public class Simulation : IAsyncDisposable
{
    private AudioManager Audio { get; set; }
    private PersistentStorage Storage { get; set; }

    public Profile Profile { get; set; } = new();

    // COMPONENTS and VARIABLES
    public bool Running { get; set; } = false;
    public bool Ignited { get; set; } = false;

    public bool Started => _started;

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
    public SwitchBoundMetric CoolantValveMetric { get; }
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

    public MetricBool AuthBravo8 { get; } = new("Auth Bravo-8", false, "AUTHORIZED", "FALSE");
    public MetricBool AuthShutdownPumps { get; } = new("Shutdown Pumps", false, "OFFLINE", "RUNNING");

    public AuthButton IgnitionButton { get; } = new();
    public TriggerSwitch IgnitionAuthBravo8 { get; } = new();
    public TriggerSwitch IgnitionShutdownPumps { get; } = new();

    public Metric GridTotalOutput { get; } = new("Total Output", 0, "kW")
    {
        DecimalPlaces = 0,
    };

    public Metric GridExcessOutput { get; } = new("Excess", 0, "kW")
    {
        DecimalPlaces = 0,
    };

    public Metric ShiftPowerOrderDemand { get; } = new("Current Power Order", 0, "kW")
    {
        DecimalPlaces = 0,
        ValueOverride = _na,
    };

    public Metric ShiftPowerOrderMargin { get; } = new("Margin For Error", 0)
    {
        DecimalPlaces = 0,
        ValueOverride = _na,
    };

    public Metric ShiftPowerOrderHold { get; } = new("Hold For", 0, "seconds")
    {
        ShowUnit = false,
        ValueOverride = _na,
    };

    public Metric ShiftOrders { get; } = new("Orders Completed", 0)
    {
        DecimalPlaces = 0,
    };

    public Metric ShiftTier { get; } = new("Tier", 1)
    {
        DecimalPlaces = 0,
    };

    public Metric ShiftTimeLeft { get; } = new("Time Until Shift End", 0, "seconds")
    {
        DecimalPlaces = 0,
        ShowUnit = false,
    };

    public SwitchBoundMetric ShiftEfficiencyAct { get; }
    public SwitchBoundMetric ShiftHazardPay { get; }

    public TriggerSwitch ShiftOrderSwitch { get; } = new(false);
    public TriggerSwitch ShiftEfficiencyActSwitch { get; } = new(false);
    public TriggerSwitch ShiftHazardPaySwitch { get; } = new(false);

    public Metric PlayerXp { get; } = new("Experience", 0)
    {
        Unit = "XP",
        DecimalPlaces = 0,
    };

    public bool Stalled { get; set; } = false;

    private bool _reactorOverheat = false;
    private double _extraHeat = 0;
    private bool _meltdown = false;
    private bool _scramRodWillFail = false;

    private bool _scramRodFailed = false;

    // it's anrover :pray:
    private bool _notgreatnotterrible = false;
    private bool _forceMeltdown = false;

    private double _ignitionTime = 0;
    private bool _igniting = false;

    private bool _enableTier4 = false;
    private double _shiftRemainingTime = 0;
    private double _shiftTimeSinceLastOrder = double.MaxValue;
    private double? _shiftOrderDemand = null;
    private double? _shiftOrderMargin = null; // met? = demand +- margin
    private double? _shiftOrderTime = null;
    private bool _shiftDay = false;

    private const string _na = "N/A";

    // EVENTS
    public event EventHandler<KeyPressEventArgs>? KeyPress;
    public event EventHandler<double>? OnUpdate;
    public event EventHandler<Notification>? OnNotification;

    public event Action? Ticked;
    private readonly GameLoop _loop;
    private bool _started;
    private bool _firstTick = true;

    public Simulation(AudioManager audioManager, PersistentStorage storage)
    {
        Audio = audioManager;
        Storage = storage;
        
        _loop = new GameLoop(20, Update, () => Ticked?.Invoke());

        CoolantValveMetric = new(CoolantValve, "Coolant Valves", "OPEN", "CLOSED");
        ShiftEfficiencyAct = new(ShiftEfficiencyActSwitch, "PO Efficiency Act", "ACTIVE", "INACTIVE");
        ShiftHazardPay = new(ShiftHazardPaySwitch, "Hazard Pay Bill", "ACTIVE", "INACTIVE");

        Turbine1.SyncSwitch.Triggered += (sender, value) => AttemptTurbineSync(Turbine1);
        Turbine2.SyncSwitch.Triggered += (sender, value) => AttemptTurbineSync(Turbine2);

        ScramButton.Engaged += OnScramEngage;

        IgnitionAuthBravo8.Triggered += (_, value) => OnIgnitionButtons(IgnitionAuthBravo8, value);
        IgnitionShutdownPumps.Triggered += (_, value) => OnIgnitionButtons(IgnitionShutdownPumps, value);
        IgnitionButton.Engaged += AttemptIgnition;

        ShiftOrderSwitch.Triggered += (_, value) =>
        {
            if (value && _shiftTimeSinceLastOrder >= Parameters.Shift.RequestInterval)
            {
                AttemptRequestOrder();
            }
        };
        ShiftEfficiencyActSwitch.Triggered += PoeaSwitchTriggered;
        ShiftHazardPaySwitch.Triggered += HazardPaySwitchTriggered;

        KeyPress += (_, key) =>
        {
            if (key.Key == "m")
            {
                TriggerMeltdown();
            }

            if (key.Key == "n")
            {
                Turbine1.Phase = 0;
                Turbine1.FlowRate.Value = 3.61;
                Turbine1.Valve.Value = 1;
                Turbine1.Rpm.Value = 3000;
                AttemptTurbineSync(Turbine1);

                Turbine2.Phase = 0;
                Turbine2.FlowRate.Value = 3.61;
                Turbine2.Valve.Value = 1;
                Turbine2.Rpm.Value = 3000;
                AttemptTurbineSync(Turbine2);
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

    private void OnFirstTick()
    {
        RodControl.Locked = true;

        Audio.PreloadAsync(AudioKeys.Sfx.MetalCry, AudioKeys.Sfx.ReactorExplosion);
        Audio.PreloadAsync(AudioKeys.Music.Overheat, AudioKeys.Music.Shutdown, AudioKeys.Music.Evacuate,
            AudioKeys.Music.Meltdown);

        Storage.GetAsync<Profile>("profile").AsTask().ContinueWith((task) =>
        {
            if (task.Result is not null)
            {
                Profile = task.Result;
                PlayerXp.Value = Profile.Experience;
            }

            return Task.CompletedTask;
        });
        
        Notify("Welcome to NNPPRS", "Please report any bug!", silent: true);

        Task.Run(OrderRequestInterval);
        Task.Run(RunShiftLoop);
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
            ReactorStatus.Value = Models.ReactorStatus.Heating;
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
                    ReactorStatus.Value = Models.ReactorStatus.Meltdown;
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

        GridTotalOutput.Value = GetTurbineOutput();
        GridExcessOutput.Value = GetExcessOutput();

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
                Notify("Reactor overheat",
                    "The Reactor is above safe operating parameters. Lower temperature immediately.", true);
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
                Task.Run(Meltdown);
            }

            if (_meltdown && ScramButton.Enabled && RodInsertion.Value < 1)
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
                    ScramFailRods();
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

        PowerOrderCheckLoop(dt);

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
            Audio.PlaySfxAsync(AudioKeys.Sfx.AuthTrigger, 2d);

            Task.Run(async () =>
            {
                await Audio.PlaySfxAsync(AudioKeys.Music.Ignition);
                await Sleep(8.6);
                Notify("Reactor Ignition", "The Facility Reactor is currently being ignited. Standby");
                await Sleep(2.4);
                _igniting = true;
                await Sleep(43);
                Notify("Reactor ignition", "Reactor online. Code Bravo-8 is now in effect.");
                RodControl.Locked = false;
                Running = true;
                _igniting = false;
                await AddXpAsync(100, "Ignited the reactor.");
            });
        }
        else
        {
            Audio.PlaySfxAsync(AudioKeys.Sfx.ControlDenied);
        }
    }

    public async Task Meltdown()
    {
        _meltdown = true;

        await Audio.PlaySfxLoopAsync(AudioKeys.Sfx.MeltdownAlarm);
        await Audio.PlaySfxAsync(AudioKeys.Sfx.AnnouncerMeltdown);

        await Sleep(3);

        await Audio.PlayMusicAsync(AudioKeys.Music.Meltdown, 1, loop: false);
        Notify("Reactor overheat",
            "The Reactor is above safe operating parameters. Lower temperature immediately.", true);

        await Sleep(7);

        await Audio.PlaySfxAsync(AudioKeys.Sfx.MeltdownExplosion);

        await Sleep(9);

        _extraHeat = Parameters.Core.MeltdownExtraHeat;
        await Audio.StopSfxAsync(AudioKeys.Sfx.MeltdownAlarm);
        Notify("Reactor meltdown", "All Non-Reactor Operations staff are to evacuate.", true);

        await Sleep(10);

        Notify("Reactor shutdown",
            "An official emergency has been declared. Emergency options are now available.", true);

        await Sleep(15);

        await Audio.PlaySfxAsync(AudioKeys.Sfx.AuthTrigger, 2d);
        ScramButton.Available = true;

        await Sleep(190);

        if (ReactorTemperature.Value < 900)
        {
            Notify("Reactor shutdown",
                "Temperature has returned to safe operating parameters. Full shutdown in progress.", true);
            _extraHeat = -57;
            await Audio.PlayMusicAsync(AudioKeys.Music.Shutdown, 1, loop: false);

            await Audio.StopSfxAsync(AudioKeys.Sfx.ScramActive, 30);
            await Sleep(10);

            await AddXpAsync(1000, "Successfully shutdown the reactor.");
            
            await Sleep(20);
            Notify("SCRAM Qualification",
                "\"That... Was close.\" Successfully scram the reactor before it explodes. Refresh the page to restart.",
                silent: true, permanent: true);
        }
        else
        {
            if (!ScramButton.Enabled)
            {
                ScramButton.Available = false;
            }

            await Audio.PlaySfxAsync(AudioKeys.Sfx.MetalCry);
            await Audio.PlayMusicAsync(AudioKeys.Music.Evacuate, 1, loop: false);
            await Sleep(6);
            await Audio.PlaySfxAsync(AudioKeys.Sfx.ReactorExplosion, 2d);
            Notify("Reactor meltdown",
                "Reactor continues to be in a critical state. Full Evacuation in progress.", true);
            _extraHeat = 57;
            _notgreatnotterrible = true;

            await Audio.StopSfxAsync(AudioKeys.Sfx.ScramActive, 30);
            await Sleep(30);
            Notify("Unforeseen Consequences", "Experience a meltdown. Refresh the page to restart.", silent: true,
                permanent: true);
        }
    }

    public void OnScramEngage()
    {
        if (!_notgreatnotterrible)
        {
            _extraHeat = 0;
        }

        RodControl.Position = RodController.ControlPosition.Neutral;
        RodControl.Locked = true;
        Audio.PlaySfxLoopAsync(AudioKeys.Sfx.ScramActive);
        Notify("Reactor scram", "SCRAM sequence engaged.", true);
    }

    private void ScramFailRods()
    {
        _scramRodFailed = true;
        RodInsertion.Value = 0;
        RodInsertion.ValueOverride = "ERR";
        _extraHeat = 57;
        Notify("Reactor SCRAM", "Control rods have sustained damage. SCRAM sequence has failed.", true);
    }

    public double GetTurbineOutput()
    {
        var output1 = Math.Max(0, Parameters.TurbineOutput(Turbine1.FlowRate.Value));
        var output2 = Math.Max(0, Parameters.TurbineOutput(Turbine2.FlowRate.Value));
        var output = 0d;
        output += Turbine1.Status.Value == Turbine.TurbineStatus.Synced
            ? output1
            : 0;

        output += Turbine2.Status.Value == Turbine.TurbineStatus.Synced
            ? output2
            : 0;

        return output;
    }

    public double GetExcessOutput()
    {
        return Math.Clamp(GetTurbineOutput(), 0, 50000);
    }

    public void OnKeyPress(KeyPressEventArgs args)
    {
        if (_started)
        {
            KeyPress?.Invoke(this, args);
        }
    }

    public void TriggerMeltdown()
    {
        _forceMeltdown = true;
    }

    private void PoeaSwitchTriggered(object? sender, bool active)
    {
        if (active)
        {
            Notify("Power Order Efficiency Act", "Power orders can now be 2x the size, but will give 2x pay.");
        }
        else
        {
            Notify("Power Order Efficiency Act", "Power orders returned to normal size.");
        }
    }

    private void HazardPaySwitchTriggered(object? sender, bool active)
    {
        if (active)
        {
            Notify("Hazard Pay Bill",
                "The higher the temperature is above 2100, the more XP is awarded, and a reduction in XP otherwise.");
        }
        else
        {
            Notify("Hazard Pay Bill", "Hazard temperature bonus inactive.");
        }
    }

    public double GetHazardPayBonus()
    {
        if (!ShiftHazardPaySwitch.Value)
        {
            return 0d;
        }

        var temperature = ReactorTemperature.Value;
        const double hazardTempBase = 2100;
        const double maxXpCoef = 0.45;
        const double minXpCoef = -0.15;
        var maxDelta = Parameters.Core.MeltdownTemperature - hazardTempBase;
        var delta = temperature - hazardTempBase;
        var bonus = delta / maxDelta * maxXpCoef;

        return Math.Clamp(bonus, minXpCoef, maxXpCoef);
    }

    public int GetPoeaMultiplier()
    {
        return ShiftEfficiencyActSwitch.Value ? 2 : 1;
    }

    public double GetBonusMultiplier()
    {
        double baseMul = GetPoeaMultiplier();

        baseMul += GetHazardPayBonus();

        return baseMul;
    }

    public double GetPowerOrderXp()
    {
        return 150;
    }

    public int GetShiftTier()
    {
        return ShiftOrders.Value switch
        {
            <= 3 => 1,
            <= 6 => 2,
            <= 9 => 3,
            _ when _enableTier4 => 4,
            _ => 3
        };
    }

    public double GetShiftXp(int tier)
    {
        var baseXp = tier switch
        {
            1 => 250,
            2 => 500,
            3 => 1500,
            4 => 3000,
            _ => 0,
        };

        return baseXp;
    }

    public bool IsDemandMet(double demand, double margin)
    {
        var excess = GetExcessOutput();
        var delta = Math.Abs(excess - demand);
        return delta <= margin;
    }

    public int GenerateOrder()
    {
        return RandomNumberGenerator.GetInt32(Parameters.Shift.PowerOrderMinPower,
            Parameters.Shift.PowerOrderMaxPower) * GetPoeaMultiplier();
    }

    public int GenerateOrderTime()
    {
        var index = RandomNumberGenerator.GetInt32(0, Parameters.Shift.PowerOrderTimes.Length);
        return Parameters.Shift.PowerOrderTimes[index];
    }

    public int GenerateOrderMargin()
    {
        return RandomNumberGenerator.GetInt32(Parameters.Shift.DemandMinMargin, Parameters.Shift.DemandMaxMargin);
    }

    private void AttemptRequestOrder()
    {
        if (RequestOrder() && !_meltdown)
        {
            // the shift manager screen never had more details btw.
            Notify("Incoming Power Order", "See the \"Shift Manager\" screen for more details.");
            Audio.PlaySfxAsync(AudioKeys.Sfx.PowerOrder);
        }
    }

    private async Task OrderRequestInterval()
    {
        while (!_meltdown)
        {
            if (ShiftOrderSwitch.Value)
            {
                AttemptRequestOrder();
            }

            await Sleep(Parameters.Shift.RequestInterval);
        }
    }

    private void PowerOrderCheckLoop(double dt)
    {
        _shiftRemainingTime -= dt;
        _shiftTimeSinceLastOrder += dt;

        ShiftTimeLeft.Value = _shiftRemainingTime;
        ShiftTimeLeft.ValueOverride = _shiftRemainingTime < 0 ? _na : null;

        if (_shiftOrderDemand is null || _shiftOrderMargin is null)
        {
            return;
        }

        if (IsDemandMet(_shiftOrderDemand ?? 0, _shiftOrderMargin ?? 0))
        {
            _shiftOrderTime -= dt;
            ShiftPowerOrderHold.Value = _shiftOrderTime ?? 0;
        }

        if (AttemptCompleteOrder())
        {
            Task.Run(async () =>
            {
                Notify("Power Order Completed", "Bonus paycheck enroute.");
                await Sleep(10);

                var xp = GetPowerOrderXp();
                await AddXpAsync(xp, "Power order completed.");
            });
        }
    }

    private async Task RunShiftLoop()
    {
        while (!_notgreatnotterrible)
        {
            ShiftOrders.Value = 0;
            ShiftTier.Value = 1;

            _shiftDay = !_shiftDay;
            var day = _shiftDay ? "Day" : "Night";
            Notify("Shift Management",
                $"{day} Shift personnel. You have 5 minutes to get to your stations. Reactor prep may begin at the shift tone.");
            await Sleep(16);
            // TODO: HORN HERE
            // supposedly have to wait lots of time before starting, but nah
            Notify("Shift Management",
                $"{day} Shift personnel. The shift has started. You may begin doing power orders.");
            _shiftRemainingTime = Parameters.Shift.Duration;

            await Task.Delay(TimeSpan.FromSeconds(_shiftRemainingTime));
            
            
            var tier = GetShiftTier();
            var xp = GetShiftXp(tier);
            Notify("Shift Management",
                $"Tier {tier} shift achieved. Excellent work. Your paychecks will reflect your dedication.");
            await Sleep(16);
            await AddXpAsync(xp, $"Successfully completed a Tier {tier} shift!");
            await Sleep(10);
        }
    }

    private bool RequestOrder()
    {
        if (_shiftOrderDemand is not null)
        {
            return false;
        }

        _shiftTimeSinceLastOrder = 0;

        _shiftOrderDemand = GenerateOrder();
        _shiftOrderTime = GenerateOrderTime();
        _shiftOrderMargin = GenerateOrderMargin();

        ShiftPowerOrderDemand.Value = _shiftOrderDemand ?? 0;
        ShiftPowerOrderHold.Value = _shiftOrderTime ?? 0;
        ShiftPowerOrderMargin.Value = _shiftOrderMargin ?? 0;

        ShiftPowerOrderHold.ValueOverride = null;
        ShiftPowerOrderDemand.ValueOverride = null;
        ShiftPowerOrderMargin.ValueOverride = null;

        return true;
    }

    private bool AttemptCompleteOrder()
    {
        if (_shiftOrderTime > 0)
        {
            return false;
        }

        _shiftOrderDemand = null;
        _shiftOrderTime = null;
        _shiftOrderMargin = null;

        ShiftPowerOrderHold.ValueOverride = "N/A";
        ShiftPowerOrderDemand.ValueOverride = "N/A";
        ShiftPowerOrderMargin.ValueOverride = "N/A";

        ShiftOrders.Value++;
        ShiftTier.Value = GetShiftTier();

        return true;
    }

    public async Task AddXpAsync(double xp, string message)
    {
        var bonus = GetBonusMultiplier();
        xp *= bonus;
        Profile.Experience += (int)xp;
        PlayerXp.Value = Profile.Experience;
        await Storage.SetAsync("profile", Profile);
        Notify("Bonus Paycheck", $"{message} (+{xp:N0}) ({bonus:F1}x)");
    }

    public void Notify(Notification notification)
    {
        OnNotification?.Invoke(this, notification);
    }

    public void Notify(string title, string message, bool critical = false, bool permanent = false, bool silent = false)
    {
        Notify(new Notification(title, message, critical, permanent, silent));
    }

    public Task Sleep(double seconds)
    {
        return Task.Delay(TimeSpan.FromSeconds(seconds));
    }

    public async ValueTask DisposeAsync()
    {
        await _loop.DisposeAsync();
    }
}