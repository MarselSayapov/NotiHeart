namespace NotiHeart.Contracts;

public sealed class NotificationEnvelope
{
    public NotificationEnvelope(Guid id, NotificationRequest request)
    {
        Id = id;
        Request = request;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public NotificationRequest Request { get; }
    public DateTimeOffset CreatedAt { get; }
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
}
