using NotiHeart.Contracts;

namespace NotiHeart.Persistence;

public sealed class Notification
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<NotificationAttempt> Attempts { get; set; } = new List<NotificationAttempt>();
    public ICollection<NotificationAttachment> Attachments { get; set; } = new List<NotificationAttachment>();
}
