namespace NotiHeart.Contracts;

public sealed record NotificationAttempt(
    DateTimeOffset Timestamp,
    NotificationStatus Status,
    string? Error);
