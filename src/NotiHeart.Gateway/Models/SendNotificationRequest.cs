using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using NotiHeart.Contracts;

namespace NotiHeart.Gateway.Models;

public sealed class SendNotificationRequest
{
    [Required]
    public NotificationChannel Channel { get; init; }

    [Required]
    [MinLength(2)]
    public string Recipient { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Text { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }

    public IReadOnlyCollection<IFormFile>? Attachments { get; init; }
}
