using NNPP.Models;
using NNPP.Reactor.Components;

namespace NNPP.Reactor;

public class FacilityGrid(Simulation sim)
{
    // TODO: .NET 11 feature
    // public union GridSource(PrimarySource, AuxiliarySource);

    public enum PrimarySource
    {
        External,
        Turbines,
    }

    public enum AuxiliarySource
    {
        Primary,
        Generators,
    }

    public IReadOnlyList<GridGenerator> Generators { get; } = [new(sim), new(sim), new(sim)];

    // EXT/Turb to Primary
    public MetricEnum<PrimarySource> Primary { get; } = new("Primary", PrimarySource.External);

    // Primary/EDG to Aux
    public MetricEnum<AuxiliarySource> Auxiliary { get; } = new("Auxiliary", AuxiliarySource.Primary);

    public Metric PrimaryDemand { get; } = new("Demand", 0, "kW")
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };

    public Metric PrimarySupply { get; } = new("Supply", 26000, "kW")
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };

    public Metric AuxDemand { get; } = new("Demand", 0, "kW")
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };

    public Metric AuxSupply { get; } = new("Supply", 16800, "kW") // initial value just to stop DC from freaking out
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };

    // External transformers can break

    public ExternalTransformer Transformer1 { get; set; } = new();
    public ExternalTransformer Transformer2 { get; set; } = new();
    
    public bool ExternalRunning => Transformer1.Running.Value && Transformer2.Running.Value;

    // Aux to DC
    public bool DcConnected { get; set; } = true;

    public PrimarySource? PrimaryPending { get; set; } = null;
    public AuxiliarySource? AuxiliaryPending { get; set; } = null;
    public bool? DcPending { get; set; } = null;

    public const double PendingTime = 5;

    private double _primaryPendingTime = 0;
    private double _auxiliaryPendingTime = 0;
    private double _dcPendingTime = 0;

    public bool ExtCanSupply()
    {
        return ExternalRunning;
    }

    public bool TurbinesCanSupply()
    {
        return sim.GetTurbineOutput() > 0;
    }

    public bool GensCanSupply()
    {
        return Generators.Any(q => q.Running);
    }

    public bool PrimCanSupply()
    {
        return IsPrimaryPowered();
    }


    public bool IsPrimaryPowered()
    {
        return PrimarySupply.Value > 0;
    }

    public bool IsAuxiliaryPowered()
    {
        return AuxSupply.Value > 0;
    }

    public bool IsSourcePowering(PrimarySource source)
    {
        return source switch
        {
            PrimarySource.External => ExtCanSupply(),
            PrimarySource.Turbines => TurbinesCanSupply(),
            _ => false,
        };
    }
    
    public bool IsSourcePowering(AuxiliarySource source)
    {
        return source switch
        {
            AuxiliarySource.Primary => PrimCanSupply(),
            AuxiliarySource.Generators => GensCanSupply(),
            _ => false,
        };
    }

    public bool IsDcPowered()
    {
        var auxSupply = AuxSupply.Value;
        var auxDemand = GetAuxDemand();
        return DcConnected && auxSupply - auxDemand >= 0;
    }

    private int GetGeneratorNoise(int running)
    {
        var seed = (int)((double)sim.Tick / sim.Tickrate * 10) * 1000;
        var rng = new Random(seed);
        var wave = Math.Sin(0.5 * sim.Runtime * Math.PI * rng.NextDouble());
        var noise = 0;
        for (var i = 0; i < running; i++)
        {
            noise += (int)(wave * rng.NextDouble() * Parameters.Grid.GenPowerNoiseMax);
        }

        return noise;
    }

    public int GetGeneratorsOutput()
    {
        return (int)Generators.Sum(q => q.GetOutput());
    }

    /// <summary>
    /// Get demand of primary bus alone, without aux and dc
    /// </summary>
    /// <returns></returns>
    public int GetPrimaryBusDemand()
    {
        var demand = 0;
        demand += sim.CoolantPumpAlpha.GetPowerDemand(); // always demanding power regardless of state
        demand += sim.FeedwaterPump1 is { Powered: true, Alive: true } ? sim.FeedwaterPump1.GetPowerDemand() : 0;

        return demand;
    }

    /// <summary>
    /// Get demand of aux and dc, it's inconsistent
    /// </summary>
    /// <returns></returns>
    public int GetAuxBusDemand()
    {
        var demand = 0;
        demand += sim.CoolantPumpBeta.GetPowerDemand(); // always demanding power regardless of state
        demand += sim.FeedwaterPump2 is { Powered: true, Alive: true } ? sim.FeedwaterPump2.GetPowerDemand() : 0;


        return demand;
    }

    /// <summary>
    /// Get demand of dc
    /// </summary>
    /// <returns></returns>
    public int GetDcBusDemand()
    {
        return DcConnected ? Parameters.Grid.DcDemand : 0;
    }

    public int GetPrimaryDemand()
    {
        return GetPrimaryBusDemand() + (Auxiliary.Value == AuxiliarySource.Primary ? GetAuxBusDemand() : 0);
    }

    public int GetAuxDemand()
    {
        // faithfully, dc is accounted here
        return GetAuxBusDemand() + GetDcBusDemand();
    }

    public double GetTotalOutput()
    {
        var value = sim.GetTurbineOutput();
        if (Auxiliary.Value == AuxiliarySource.Generators)
        {
            value += GetGeneratorsOutput();
        }

        return value;
    }

    public double GetExcessOutput()
    {
        var output = sim.GetTurbineOutput();
        if (Primary.Value == PrimarySource.Turbines)
        {
            output -= Parameters.Grid.OverallFacilityDemand;
        }

        return Math.Clamp(output, 0, Parameters.Grid.MaxExcessOutput);
    }

    public void Update(double dt)
    {
        // the bus displays in CR are misleading
        // the primary overview shows a demand of ~18400kW, that's Primary + Aux alone, without DC
        // Consider that a normal bus alone, like prim or aux, draw coolant and FW (6000 and 3200 (at 80%) respectively).
        // so prim + aux is... you guessed it: 9200 * 2 = 18,400.
        // but aux also powers DC, and that's an extra 5000kW!!!
        // so why doesn't the primary overview show a demand of ~23400kW, that's Primary + Aux + DC?
        // at least the aux overview shows aux + dc, but cmon.

        // Primary overview demand = Primary + AUX (NO DC!)
        // Aux overview demand = Aux + DC.

        // absolutely insane.

        // should i be faithful or should i be sane?

        // primary

        var primarySupply = Primary.Value switch
        {
            PrimarySource.External => ExternalRunning ? Parameters.Grid.ExternalSupply : 0,
            PrimarySource.Turbines => sim.GetTurbineOutput(),
            _ => 0,
        };

        primarySupply = Math.Clamp(primarySupply, 0, 30000);

        // aux

        var auxSupply = Auxiliary.Value switch
        {
            AuxiliarySource.Primary => Math.Max(0, primarySupply - GetPrimaryBusDemand()),
            AuxiliarySource.Generators => GetGeneratorsOutput(), // TODO: implement
            _ => 0
        };

        // dc

        var dcSupply = DcConnected ? Math.Max(0, auxSupply - GetAuxBusDemand()) : 0;

        PrimaryDemand.Value = GetPrimaryDemand();
        AuxDemand.Value = GetAuxDemand();

        PrimarySupply.Value = primarySupply;
        AuxSupply.Value = auxSupply;

        // handle components

        // primary
        primarySupply -= sim.CoolantPumpAlpha.GetPowerDemand();
        sim.CoolantPumpAlpha.Powered = primarySupply >= 0;
        primarySupply -= sim.FeedwaterPump1.GetPowerDemand();
        sim.FeedwaterPump1.Powered = primarySupply >= 0;

        // Auxiliary
        auxSupply -= sim.CoolantPumpBeta.GetPowerDemand();
        sim.CoolantPumpBeta.Powered = auxSupply >= 0;
        auxSupply -= sim.FeedwaterPump2.GetPowerDemand();
        sim.FeedwaterPump2.Powered = auxSupply >= 0;

        if (PrimaryPending.HasValue)
        {
            if (_primaryPendingTime <= 0)
            {
                Primary.Value = PrimaryPending.Value;
                PrimaryPending = null;
                NotifyGridChange("Reactor Bus", Primary.Value == PrimarySource.External ? "External Bus" : "Turbines");
            }

            _primaryPendingTime -= dt;
        }

        if (AuxiliaryPending.HasValue)
        {
            if (_auxiliaryPendingTime <= 0)
            {
                Auxiliary.Value = AuxiliaryPending.Value;
                AuxiliaryPending = null;
                NotifyGridChange("Auxiliary Bus",
                    Auxiliary.Value == AuxiliarySource.Primary ? "Reactor Bus" : "Generators");
            }

            _auxiliaryPendingTime -= dt;
        }

        if (DcPending.HasValue)
        {
            if (_dcPendingTime <= 0)
            {
                DcConnected = DcPending.Value;
                DcPending = null;

                if (DcConnected)
                {
                    NotifyGridChange("DC Bus", "Auxiliary Bus");
                }
                else
                {
                    sim.Notify("Facility Power Grid", "Power to DC Bus has been cut.");
                }
            }

            _dcPendingTime -= dt;
        }
    }

    private void NotifyGridPending()
    {
        sim.Notify("Facility Power Grid",
            "Reactor Operations requests a grid change. This change will happen automatically. Standby.");
    }

    private void NotifyGridChange(string bus, string source)
    {
        sim.Notify("Facility Power Grid", $"{bus} is now being powered by {source}");
    }

    // TODO: once .NET 11 is released, switch to union types

    /// <summary>
    /// Switch primary source.
    /// </summary>
    /// <param name="source"></param>
    public void SwitchGrid(PrimarySource source)
    {
        PrimaryPending = source;
        _primaryPendingTime = PendingTime;
        NotifyGridPending();
    }

    /// <summary>
    /// Switch auxiliary source.
    /// </summary>
    /// <param name="source"></param>
    public void SwitchGrid(AuxiliarySource source)
    {
        AuxiliaryPending = source;
        _auxiliaryPendingTime = PendingTime;
        NotifyGridPending();
    }

    /// <summary>
    /// Toggle DC grid.
    /// </summary>
    /// <param name="toggle"></param>
    public void SwitchGrid(bool toggle)
    {
        DcPending = toggle;
        _dcPendingTime = PendingTime;
        NotifyGridPending();
    }

    public bool IsPending(PrimarySource source)
    {
        return PrimaryPending == source;
    }

    public bool IsPending(AuxiliarySource source)
    {
        return AuxiliaryPending == source;
    }

    public bool IsDcPending()
    {
        return DcPending.HasValue;
    }
}