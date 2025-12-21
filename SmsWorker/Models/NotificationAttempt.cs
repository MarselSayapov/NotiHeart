namespace SmsWorker.Models;

public sealed class NotificationAttempt
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public int AttemptNo { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Error { get; set; }
}
