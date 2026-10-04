namespace NNPP.Reactor;

public class FacilityGrid
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
    
    public const double PrimaryDemand = 9160;
    public const double AuxiliaryDemand = 9160;
    public const double DcDemand = 5000;
    public const double GeneratorSupply = 5666; // x 3 =  17000
    public const double ExternalSupply = 26000;
    
    // EXT/Turb to Primary
    public PrimarySource Primary { get; set; } = PrimarySource.External;
    
    // Primary/EDG to Aux
    public AuxiliarySource Auxiliary { get; set; } = AuxiliarySource.Primary;
    
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

    public double GetPrimaryDemand()
    {
        return PrimaryDemand + (Auxiliary == AuxiliarySource.Primary ? GetAuxDemand() : 0);
    }

    public double GetAuxDemand()
    {
        return AuxiliaryDemand + (DcConnected ? GetDcDemand() : 0);
    }
    
    public double GetDcDemand()
    {
        return DcDemand;
    }
}