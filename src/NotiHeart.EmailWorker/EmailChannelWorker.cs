using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using NotiHeart.WorkerBase;
using Serilog;

namespace NotiHeart.EmailWorker;

public sealed class EmailChannelWorker : ChannelWorkerBase
{
    private readonly IEmailSender _emailSender;

    public EmailChannelWorker(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<NotificationProcessingOptions> processingOptions,
        IServiceScopeFactory scopeFactory,
        IEmailSender emailSender)
        : base(NotificationChannel.Email, rabbitOptions, processingOptions, scopeFactory, Log.Logger.ForContext<EmailChannelWorker>())
    {
        _emailSender = emailSender;
    }

    protected override async Task<SendResult> SendAsync(
        Notification notification,
        IReadOnlyCollection<NotificationAttachment> attachments,
        CancellationToken cancellationToken)
    {
        var success = await _emailSender.SendAsync(notification.Recipient, notification.Text, attachments, cancellationToken);
        return success ? SendResult.Ok() : SendResult.Fail("Temporary email failure", "Transient");
    }
}
