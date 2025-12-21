using Microsoft.AspNetCore.Http;

namespace NotiHeart.Models;

public sealed class SendNotificationRequest
{
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<IFormFile> Attachments { get; set; } = new();
}
