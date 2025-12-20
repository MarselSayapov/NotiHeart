using NotiHeart.Contracts;

namespace NotiHeart.Gateway.Services;

public interface INotificationPublisher
{
    Task<Task> PublishAsync(NotificationDispatchMessage dispatchMessage, CancellationToken cancellationToken);
}
