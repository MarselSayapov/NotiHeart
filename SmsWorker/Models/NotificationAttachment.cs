namespace SmsWorker.Models;

public sealed class NotificationAttachment
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public long Size { get; set; }
}
