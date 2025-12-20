using System.ComponentModel.DataAnnotations;

namespace NotiHeart.Contracts;

public sealed class NotificationRequest
{
    [Required]
    public NotificationChannel Channel { get; init; }

    [Required]
    [MinLength(2)]
    public string Recipient { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Message { get; init; } = string.Empty;

    public Dictionary<string, string>? Metadata { get; init; }
}
