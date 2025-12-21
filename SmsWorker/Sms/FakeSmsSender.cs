using Microsoft.Extensions.Options;

namespace SmsWorker.Sms;

public sealed class FakeSmsSender(IOptions<SmsSenderOptions> options, ILogger<FakeSmsSender> logger) : ISmsSender
{
    private readonly SmsSenderOptions _options = options.Value;
    private readonly ILogger<FakeSmsSender> _logger = logger;
    private readonly Random _random = new();

    public Task SendAsync(string recipient, string text, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fake SMS send to {Recipient}", recipient);

        if (_options.ForcePermanentFailure)
        {
            throw new PermanentSmsException("Permanent SMS delivery failure.");
        }

        if (_options.ForceTemporaryFailure || _options.TemporaryFailureRate > 0 && _random.NextDouble() < _options.TemporaryFailureRate)
        {
            throw new TemporarySmsException("Temporary SMS delivery failure.");
        }

        return Task.CompletedTask;
    }
}
