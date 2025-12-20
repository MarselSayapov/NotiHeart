using NotiHeart.Contracts;

namespace NotiHeart.Persistence;

public sealed class NotificationAttempt
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public int AttemptNo { get; set; }
    public NotificationStatus Status { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
