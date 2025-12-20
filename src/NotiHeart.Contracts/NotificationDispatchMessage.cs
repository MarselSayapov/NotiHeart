namespace NotiHeart.Contracts;

public sealed record NotificationDispatchMessage(
    Guid NotificationId,
    string CorrelationId,
    NotificationChannel Channel,
    int AttemptNo);
