using NotiHeart.Contracts;

namespace NotiHeart.Gateway.Models;

public sealed record NotificationAttemptResponse(
    int AttemptNo,
    NotificationStatus Result,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string? Error);
