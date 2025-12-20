namespace NotiHeart.Contracts;

public sealed record NotificationDispatchMessage(
    Guid NotificationId,
    NotificationChannel Channel,
    string Recipient,
    string Text,
    Guid[] AttachmentIds,
    string CorrelationId,
    int Attempt);
