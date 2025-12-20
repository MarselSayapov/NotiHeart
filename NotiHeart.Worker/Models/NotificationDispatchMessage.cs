namespace NotiHeart.Worker.Models;

public sealed class NotificationDispatchMessage
{
    public Guid NotificationId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Guid[] AttachmentIds { get; set; } = Array.Empty<Guid>();
    public string CorrelationId { get; set; } = string.Empty;
    public int Attempt { get; set; }
}
