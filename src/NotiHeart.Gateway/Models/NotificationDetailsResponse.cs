using NotiHeart.Contracts;

namespace NotiHeart.Gateway.Models;

public sealed record NotificationDetailsResponse(
    Guid Id,
    string CorrelationId,
    NotificationChannel Channel,
    string Recipient,
    string Text,
    NotificationStatus Status,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<NotificationAttemptResponse> Attempts);
