using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using NotiHeart.WorkerBase;
using Serilog;

namespace NotiHeart.EmailWorker;

public sealed class EmailChannelWorker : ChannelWorkerBase
{
    public EmailChannelWorker(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<NotificationProcessingOptions> processingOptions,
        IServiceScopeFactory scopeFactory)
        : base(NotificationChannel.Email, rabbitOptions, processingOptions, scopeFactory, Log.Logger.ForContext<EmailChannelWorker>())
    {
    }

    protected override Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        var shouldFail = notification.Recipient.Contains("fail", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(!shouldFail);
    }
}
