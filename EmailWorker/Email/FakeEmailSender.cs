using Microsoft.Extensions.Options;

namespace EmailWorker.Email;

public sealed class FakeEmailSender(IOptions<EmailSenderOptions> options, ILogger<FakeEmailSender> logger) : IEmailSender
{
    private readonly EmailSenderOptions _options = options.Value;
    private readonly ILogger<FakeEmailSender> _logger = logger;
    private readonly Random _random = new();

    public Task SendAsync(string recipient, string text, IReadOnlyCollection<EmailAttachment> attachments, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fake email send to {Recipient} (attachments {Count})", recipient, attachments.Count);

        if (_options.ForceTemporaryFailure || _options.TemporaryFailureRate > 0 && _random.NextDouble() < _options.TemporaryFailureRate)
        {
            throw new TemporaryEmailException("Temporary email delivery failure.");
        }

        return Task.CompletedTask;
    }
}
