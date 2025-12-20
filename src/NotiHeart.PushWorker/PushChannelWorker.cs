using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using NotiHeart.WorkerBase;
using Serilog;

namespace NotiHeart.PushWorker;

public sealed class PushChannelWorker : ChannelWorkerBase
{
    public PushChannelWorker(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<NotificationProcessingOptions> processingOptions,
        IServiceScopeFactory scopeFactory)
        : base(NotificationChannel.Push, rabbitOptions, processingOptions, scopeFactory, Log.Logger.ForContext<PushChannelWorker>())
    {
    }

    protected override Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        var shouldFail = notification.Recipient.Contains("fail", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(!shouldFail);
    }
}
