using NotiHeart.Contracts;

namespace NotiHeart.Persistence;

public sealed class NotificationAttempt
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public int AttemptNo { get; set; }
    public NotificationStatus Result { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}
