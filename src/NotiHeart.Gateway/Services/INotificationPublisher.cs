using NotiHeart.Contracts;

namespace NotiHeart.Gateway.Services;

public interface INotificationPublisher
{
    Task PublishAsync(NotificationEnvelope envelope, CancellationToken cancellationToken);
}
