namespace NotiHeart.Models;

public sealed class Notification
{
    public Guid Id { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<NotificationAttachment> Attachments { get; set; } = new List<NotificationAttachment>();
    public ICollection<NotificationAttempt> Attempts { get; set; } = new List<NotificationAttempt>();
}
