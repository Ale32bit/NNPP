using NNPP.Models;

namespace NNPP.Reactor.Grid;

public class FacilityGrid(Simulation Sim)
{
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
    
    public const int DcDemand = 5000;
    public const double GeneratorSupply = 5666; // x 3 =  17000
    public const double ExternalSupply = 26000;
    
    
    // EXT/Turb to Primary
    public MetricEnum<PrimarySource> Primary { get; } = new("Primary", PrimarySource.External);
    
    // Primary/EDG to Aux
    public MetricEnum<AuxiliarySource> Auxiliary { get; } = new("Auxiliary", AuxiliarySource.Primary);
    
    public Metric PrimaryDemand { get; } = new("Demand", 0, "kW")
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };
    
    public Metric PrimarySupply { get; } = new("Supply", 0, "kW")
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };
    
    public Metric AuxDemand { get; } = new("Demand", 0, "kW")
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };
    
    public Metric AuxSupply { get; } = new("Supply", 0, "kW")
    {
        ShowUnit = false,
        DecimalPlaces = 0,
    };
    
    // External transformers can break
    public bool ExternalRunning { get; set; } = true;
    
    // Aux to DC
    public bool DcConnected { get; set; } = true;
    

    public bool IsPrimaryPowered()
    {
        return true;
    }
    
    public bool IsAuxiliaryPowered()
    {
        return true;
    }

    public bool IsDcPowered()
    {
        return DcConnected && IsAuxiliaryPowered();
    }

    /// <summary>
    /// Get demand of primary bus alone, without aux and dc
    /// </summary>
    /// <returns></returns>
    public int GetPrimaryBusDemand()
    {
        var demand = 0;
        demand += Sim.CoolantPumpAlpha.GetPowerDemand(); // always demanding power regardless of state
        demand += Sim.FeedwaterPump1 is { Powered: true, Alive: true } ? Sim.FeedwaterPump1.GetPowerDemand() : 0;

        return demand;
    }

    /// <summary>
    /// Get demand of aux and dc, it's inconsistent
    /// </summary>
    /// <returns></returns>
    public int GetAuxBusDemand()
    {
        var demand = 0;
        demand += Sim.CoolantPumpBeta.GetPowerDemand(); // always demanding power regardless of state
        demand += Sim.FeedwaterPump2 is { Powered: true, Alive: true } ? Sim.FeedwaterPump2.GetPowerDemand() : 0;
        

        return demand;
    }
    
    /// <summary>
    /// Get demand of dc
    /// </summary>
    /// <returns></returns>
    public int GetDcBusDemand()
    {
        return DcConnected ? DcDemand : 0;
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
            PrimarySource.External => ExternalRunning ? ExternalSupply : 0,
            PrimarySource.Turbines => Sim.GetTurbineOutput(),
            _ => 0,
        };

        // aux
        
        var auxSupply = Auxiliary.Value switch
        {
            AuxiliarySource.Primary => Math.Max(0, primarySupply - GetPrimaryBusDemand()),
            AuxiliarySource.Generators => 0, // TODO: implement
            _ => 0
        };
        
        // dc
        
        var dcSupply = DcConnected ? Math.Max(0, auxSupply - GetAuxBusDemand()) : 0;

        PrimaryDemand.Value = GetPrimaryDemand();
        AuxDemand.Value = GetAuxDemand();
        
        PrimarySupply.Value = primarySupply;
        AuxSupply.Value = auxSupply;
    }
}