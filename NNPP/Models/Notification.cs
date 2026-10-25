namespace NNPP.Models;

public enum NotificationKind
{
    Normal,
    Critical,
    Experience,
}

public record Notification(string Title, string Message, NotificationKind Kind = NotificationKind.Normal, bool Permanent = false, bool Silent = false)
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
}