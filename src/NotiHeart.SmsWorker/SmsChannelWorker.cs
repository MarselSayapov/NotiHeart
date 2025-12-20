using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using NotiHeart.WorkerBase;
using Serilog;

namespace NotiHeart.SmsWorker;

public sealed class SmsChannelWorker : ChannelWorkerBase
{
    public SmsChannelWorker(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<NotificationProcessingOptions> processingOptions,
        IServiceScopeFactory scopeFactory)
        : base(NotificationChannel.Sms, rabbitOptions, processingOptions, scopeFactory, Log.Logger.ForContext<SmsChannelWorker>())
    {
    }

    protected override Task<SendResult> SendAsync(
        Notification notification,
        IReadOnlyCollection<NotificationAttachment> attachments,
        CancellationToken cancellationToken)
    {
        var shouldFail = notification.Recipient.Contains("fail", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(shouldFail
            ? SendResult.Fail("Mock SMS failure", "Transient")
            : SendResult.Ok());
    }
}
