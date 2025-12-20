using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using NotiHeart.WorkerBase;
using Serilog;

namespace NotiHeart.PushWorker;

public sealed class PushChannelWorker : ChannelWorkerBase
{
    private readonly IPushSender _pushSender;

    public PushChannelWorker(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<NotificationProcessingOptions> processingOptions,
        IServiceScopeFactory scopeFactory,
        IPushSender pushSender)
        : base(NotificationChannel.Push, rabbitOptions, processingOptions, scopeFactory, Log.Logger.ForContext<PushChannelWorker>())
    {
        _pushSender = pushSender;
    }

    protected override async Task<SendResult> SendAsync(
        Notification notification,
        IReadOnlyCollection<NotificationAttachment> attachments,
        CancellationToken cancellationToken)
    {
        var attachmentNames = attachments.Select(item => item.FileName).ToArray();
        var success = await _pushSender.SendAsync(notification.Recipient, notification.Text, attachmentNames, cancellationToken);
        return success ? SendResult.Ok() : SendResult.Fail("Temporary push failure", "Transient");
    }
}
