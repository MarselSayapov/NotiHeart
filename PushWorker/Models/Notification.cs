namespace PushWorker.Models;

public sealed class Notification
{
    public Guid Id { get; set; }
    public NotificationStatus Status { get; set; }
    public string? LastError { get; set; }
    public DateTime UpdatedAt { get; set; }
}
