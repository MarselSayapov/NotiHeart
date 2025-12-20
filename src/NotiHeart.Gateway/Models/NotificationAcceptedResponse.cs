namespace NotiHeart.Gateway.Models;

public sealed record NotificationAcceptedResponse(
    Guid Id,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    string Status);
