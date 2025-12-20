using NotiHeart.Contracts;

namespace NotiHeart.Gateway.Services;

public sealed class LoggingNotificationPublisher : INotificationPublisher
{
    private readonly ILogger<LoggingNotificationPublisher> _logger;

    public LoggingNotificationPublisher(ILogger<LoggingNotificationPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publishing notification {NotificationId} to channel {Channel} for {Recipient}.",
            envelope.Id,
            envelope.Request.Channel,
            envelope.Request.Recipient);

        return Task.CompletedTask;
    }
}
