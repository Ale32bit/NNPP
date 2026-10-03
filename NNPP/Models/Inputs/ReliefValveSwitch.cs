using NNPP.Reactor;

namespace NNPP.Models.Inputs;

public class ReliefValveSwitch
{
    public event Action? OnStatusChanged;
    
    public enum ReliefValveStatus
    {
        Ready,
        Running,
        Cooldown,
    }

    public ReliefValveStatus Status { get; set; } = ReliefValveStatus.Ready;

    public bool IsReady() => Status == ReliefValveStatus.Ready;

    public bool IsOpen() => Status == ReliefValveStatus.Running;

    public bool IsOnCooldown() => Status == ReliefValveStatus.Cooldown;

    public double RunTimeRemaining { get; set; } = 0;

    public double CooldownTimeRemaining { get; set; } = 0;

    public void Open()
    {
        if (!IsReady())
        {
            return;
        }

        Status = ReliefValveStatus.Running;
        RunTimeRemaining = Parameters.ReliefValve.Runtime;
        OnStatusChanged?.Invoke();
    }

    public void Update(double dt)
    {
        if (IsOpen())
        {
            RunTimeRemaining -= dt;

            if (RunTimeRemaining < 0)
            {
                Status = ReliefValveStatus.Cooldown;
                CooldownTimeRemaining = Parameters.ReliefValve.CooldownTime;
                OnStatusChanged?.Invoke();
            }
        }
        else if (IsOnCooldown())
        {
            CooldownTimeRemaining -= dt;

            if (CooldownTimeRemaining < 0)
            {
                Status = ReliefValveStatus.Ready;
                OnStatusChanged?.Invoke();
            }
        }
    }
}