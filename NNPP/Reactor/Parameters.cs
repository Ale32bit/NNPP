using NNPP.Models.Inputs;

namespace NNPP.Reactor;

/// <summary>
/// Hic sunt dracones.
///
/// Contains all game physics constants and functions.
/// </summary>
public static class Parameters
{
    public static class Core
    {
        public const double StallTemp = 323;
        public const double BoilTemp = 373;
        public const double LowFuelThreshold = 0.75;
        public const double HeatFullRate = 26;
        public const double RodCoolFullRate = 0.2;
        public const double ScramCoolingRate = 3;
        public const double RvCoolingRate = 7;
        public const double FuelBurnRate = 0.000474;
        public const double RodSpeed = 0.02;
        public const double MeltdownTemperature = 3120;
        public const double MeltdownExtraHeat = 17.9;
        public const double RodSpeedScram = 0.08;
        public const double IgnitionRate = 7.604651163;
    }

    public static class Feedwater
    {
        public const double MaxRpm = 3200; // 80% = 2560; 100% = 3200
        public const double RpmRiseRate = 50;
        public const double RpmFallRate = 75;
        public const double MaxFlow = 1.15;
        public const double NeedBase = 0.7;
        public const double NeedRefTemp = 1420;
        public const double NeedSlope = 1500;
        public const double NeedMax = 1.29;
        public const double LevelGain = 0.0326;
        public const double Cooling = 5;
        public const double Knee = 0.8;
        public const double LossPerLevel = 18.77;
        public const double LossMax = 15;
    }

    public static class Pressure
    {
        public const double Atm = 101.3;
        public const double PerKelvin = 6.5381;
        public const double ZeroTemp = 323;
        public const double RampEnd = 423;
        public const double RampWidth = 50;
        public const double RampExp = 1.4;
    }

    public static class CoolantPump
    {
        public const double MaxRpm = 2500;
        public const double RpmRate = 100;
        public const double CoolingPerPump = 5;
    }

    public static class ReliefValve
    {
        public const double Runtime = 10;
        public const double CooldownTime = 90;
        public const double FeedwaterLevelReplenishRate = 0.01; // 10s => 10%. 1s => 1%
    }

    public static class Turbine
    {
        public const double FlowFull = 8.454;
        public const double DeadFlow = 0.276;
        public const double SyncFlow = 3.61;
        public const double SyncRpm = 3000;
        public const double TauFast = 17.5;
        public const double TauMedium = 26.5;
        public const double TauSlow = 52.5;
        public const double TauBroken = 12;
        public const double FlowMin = 1.5;
        public const double SyncRpmTolerance = 50; // made up
        public const double PowerPerFlow = 5512.6; // minus 3.61 for base flow

        // the following vibration values are arbitrary. i need to research more
        public const double VibPerAccel = 2;
        public const double VibAccel0 = 17.5;
        public const double VibRiseTau = 1.3;
        public const double VibRefFlow = 10.17;
        public static readonly double[] VibFlows = [10.17, 10.3, 10.84, 11.1, 11.56, 12, 12.5, 13.5, 15];
        public static readonly double[] VibValues = [100, 110, 120, 130, 140, 150, 160, 170, 200];
    }

    public static class Shift
    {
        
    }

    public static double Lerp(double x, double[] xs, double[] ys)
    {
        if (x <= xs[0]) return ys[0];
        for (var i = 1; i < xs.Length; i++)
        {
            if (x <= xs[i])
            {
                return ys[i - 1] + (ys[i] - ys[i - 1]) * (x - xs[i - 1]) / (xs[i] - xs[i - 1]);
            }
        }

        return ys[^1];
    }

    public static double TemperatureRate(double fuel, double rod, double extraHeat, double waterLevel,
        double coolantRate,
        int rvOpen, bool scramCooling)
    {
        return HeatGeneration(fuel)
               + extraHeat // meltdown heat rate
               - FeedwaterCoolingRate(waterLevel)
               - coolantRate
               - RodCooling(fuel) * 100 * rod
               - Core.RvCoolingRate * rvOpen
               - (scramCooling ? Core.ScramCoolingRate : 0); // scramCooling = SCRAM active and not jammed
    }

    public static double StepTemp(double temp, double rate, double dt)
    {
        return Math.Max(Core.StallTemp, temp + rate * dt);
    }

    public static double HeatGeneration(double fuel)
    {
        return fuel >= Core.LowFuelThreshold
            ? Core.HeatFullRate
            : fuel > 0
                ? 20 * fuel + 6
                : -3;
    }

    public static double RodCooling(double fuel)
    {
        return fuel >= Core.LowFuelThreshold
            ? Core.RodCoolFullRate
            : fuel > 0
                ? 0.16 * fuel + 0.04
                : 0;
    }

    public static double FuelRate(double rod, bool ignited)
    {
        return ignited ? -Core.FuelBurnRate * (1 - rod) : 0;
    }

    public static double FeedwaterCooling(double level)
    {
        return Feedwater.Cooling -
               Math.Min(Feedwater.LossMax, Feedwater.LossPerLevel * Math.Max(0, Feedwater.Knee - level));
    }

    public static double FullPressure(double temp)
    {
        if (temp <= Core.BoilTemp)
        {
            return Pressure.Atm;
        }

        var ramp = temp >= Pressure.RampEnd
            ? 1
            : Math.Pow((temp - Core.BoilTemp) / Pressure.RampWidth, Pressure.RampExp);

        return Pressure.Atm + Pressure.PerKelvin * (temp - Pressure.ZeroTemp) * ramp;
    }

    public static double FeedwaterPumpFlow(double rpm)
    {
        return Feedwater.MaxFlow * rpm / Feedwater.MaxRpm;
    }

    public static double FeedwaterTotalFlow(double pump1, double pump2, bool valveOpen) =>
        valveOpen ? pump1 + pump2 : 0;

    public static double FeedwaterNeed(double temp, bool ignited) =>
        !ignited || temp < Core.BoilTemp
            ? 0
            : Math.Clamp(Feedwater.NeedBase + (temp - Feedwater.NeedRefTemp) / Feedwater.NeedSlope, 0,
                Feedwater.NeedMax);

    public static double FeedwaterLevelRate(double feedwater, double need) =>
        Feedwater.LevelGain * (feedwater - need); // caller: level = Math.Clamp(level + rate * dt, 0, 1)

    public static double FeedwaterCoolingRate(double level) =>
        Feedwater.Cooling - Math.Min(Feedwater.LossMax, Feedwater.LossPerLevel * Math.Max(0, Feedwater.Knee - level));

    public static double SwitchRate(int position) =>
        position switch
        {
            2 => 0.010,
            1 => 0.002,
            -1 => -0.002,
            -2 => -0.010,
            _ => 0,
        };

    public static double FeedwaterPumpStepRpm(double rpm, double utilization, bool running, double dt)
    {
        double goal = running ? Feedwater.MaxRpm * utilization : 0;
        return goal > rpm
            ? Math.Min(goal, rpm + Feedwater.RpmRiseRate * dt)
            : Math.Max(goal, rpm - Feedwater.RpmFallRate * dt);
    }

    public static double SteamPressure(double temp, double level) =>
        Pressure.Atm + (FullPressure(temp) - Pressure.Atm) * level;

    public static double StepCoolantRpm(double rpm, bool running, double dt) // running = switched on and bus powered
    {
        double goal = running ? CoolantPump.MaxRpm : 0;
        double max = CoolantPump.RpmRate * dt;
        return rpm + Math.Clamp(goal - rpm, -max, max);
    }

    public static double CoolantRate(double rpm1, double rpm2, bool valveOpen) =>
        valveOpen ? CoolantPump.CoolingPerPump * (rpm1 + rpm2) / CoolantPump.MaxRpm : 0;

    public static double TurbineFlow(double valve, double temp, double level, bool broken) =>
        broken ? 0 : Turbine.FlowFull * valve * SteamFactor(temp) * level;

    public static double SteamFactor(double temp) // TODO: ???? document this
    {
        if (temp <= Core.BoilTemp) return 0;
        if (temp <= 530) return 0.427 * (temp - Core.BoilTemp) / 157;
        return 0.427 + 0.573 * (temp - 530) / 890; // some of these values come from ANRO handbooks. i think....
    }

    public static double RpmTarget(double flow, bool broken) =>
        broken
            ? 0
            : Turbine.SyncRpm * Math.Max(0, flow - Turbine.DeadFlow) / (Turbine.SyncFlow - Turbine.DeadFlow);

    public static double RpmTau(AccelerationSwitch.SwitchPosition mode, bool broken) =>
        broken
            ? Turbine.TauBroken // needs more research
            : mode switch
            {
                AccelerationSwitch.SwitchPosition.Fast => Turbine.TauFast,
                AccelerationSwitch.SwitchPosition.Medium => Turbine.TauMedium,
                _ => Turbine.TauSlow,
            };

    public static double RpmAccel(double rpm, double target, double tau) =>
        (target - rpm) / tau; // synced: rpm = 3000 and accel = 0; else rpm = Math.Max(0, rpm + accel * dt)

    public static double VibrationBase(double flow) => // todo: needs more research
        flow < Turbine.VibRefFlow
            ? 100 * Math.Pow(Math.Max(0, flow) / Turbine.VibRefFlow, 2.5)
            : Lerp(flow, Turbine.VibFlows, Turbine.VibValues);

    public static double VibrationTarget(double flow, double accel) =>
        Math.Max(VibrationBase(flow), Turbine.VibPerAccel * (Math.Abs(accel) - Turbine.VibAccel0));

    public static double StepVibration(double vib, double target, double dt) =>
        target > vib
            ? vib + (target - vib) * Math.Min(1, dt / Turbine.VibRiseTau)
            : target;

    public static double TurbineOutput(double flow) => (flow - Turbine.SyncFlow) * Turbine.PowerPerFlow;
}