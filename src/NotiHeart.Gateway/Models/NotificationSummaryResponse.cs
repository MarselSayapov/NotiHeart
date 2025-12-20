using NotiHeart.Contracts;

namespace NotiHeart.Gateway.Models;

public sealed record NotificationSummaryResponse(
    Guid Id,
    string CorrelationId,
    NotificationChannel Channel,
    NotificationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
