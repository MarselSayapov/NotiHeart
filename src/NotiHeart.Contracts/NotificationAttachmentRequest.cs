using System.ComponentModel.DataAnnotations;

namespace NotiHeart.Contracts;

public sealed class NotificationAttachmentRequest
{
    [Required]
    [MinLength(1)]
    public string FileName { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string ContentType { get; init; } = string.Empty;

    [Required]
    public byte[] Content { get; init; } = Array.Empty<byte>();
}
