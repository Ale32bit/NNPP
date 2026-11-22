namespace NNPP.Reactor.Components;

public interface IRepairable
{
    public void Repair();
    public void FullRepair();
    public void Break();
    public bool IsBroken();
    public double GetHealthPercentage();
}