using Microsoft.Extensions.Options;
using NotiHeart.Persistence;

namespace NotiHeart.EmailWorker;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly EmailSenderOptions _options;
    private readonly ILogger<FakeEmailSender> _logger;
    private readonly Random _random = new();

    public FakeEmailSender(IOptions<EmailSenderOptions> options, ILogger<FakeEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> SendAsync(
        string recipient,
        string text,
        IReadOnlyCollection<NotificationAttachment> attachments,
        CancellationToken cancellationToken)
    {
        var shouldFail = _options.ForceFailure || (_options.FailureRate > 0 && _random.NextDouble() < _options.FailureRate);

        _logger.LogInformation(
            "Fake email sender to {Recipient} with {AttachmentCount} attachments. Success: {Success}.",
            recipient,
            attachments.Count,
            !shouldFail);

        return Task.FromResult(!shouldFail);
    }
}
