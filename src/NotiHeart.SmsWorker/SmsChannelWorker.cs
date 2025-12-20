using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using NotiHeart.WorkerBase;
using Serilog;

namespace NotiHeart.SmsWorker;

public sealed class SmsChannelWorker : ChannelWorkerBase
{
    private readonly ISmsSender _smsSender;
    private readonly ILogger<SmsChannelWorker> _logger;

    public SmsChannelWorker(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<NotificationProcessingOptions> processingOptions,
        IServiceScopeFactory scopeFactory,
        ISmsSender smsSender,
        ILogger<SmsChannelWorker> logger)
        : base(NotificationChannel.Sms, rabbitOptions, processingOptions, scopeFactory, Log.Logger.ForContext<SmsChannelWorker>())
    {
        _smsSender = smsSender;
        _logger = logger;
    }

    protected override async Task<SendResult> SendAsync(
        Notification notification,
        IReadOnlyCollection<NotificationAttachment> attachments,
        CancellationToken cancellationToken)
    {
        if (attachments.Count > 0)
        {
            _logger.LogWarning("SMS ignores {AttachmentCount} attachments for notification {NotificationId}.",
                attachments.Count,
                notification.Id);
        }

        var result = await _smsSender.SendAsync(notification.Recipient, notification.Text, cancellationToken);

        if (result.Success)
        {
            return SendResult.Ok();
        }

        return result.IsPermanentFailure
            ? SendResult.Fail(result.Error, "Permanent")
            : SendResult.Fail(result.Error, "Transient");
    }
}
