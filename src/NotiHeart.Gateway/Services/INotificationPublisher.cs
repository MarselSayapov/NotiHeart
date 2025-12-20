using NotiHeart.Contracts;

namespace NotiHeart.Gateway.Services;

public interface INotificationPublisher
{
    Task PublishAsync(NotificationDispatchMessage dispatchMessage, CancellationToken cancellationToken);
}
