namespace NNPP.Models;

public record Notification(string Title, string Message, bool Critical = false, bool Permanent = false, bool Silent = false)
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
}