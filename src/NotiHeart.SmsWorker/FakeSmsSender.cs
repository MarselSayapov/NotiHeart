using Microsoft.Extensions.Options;

namespace NotiHeart.SmsWorker;

public sealed class FakeSmsSender : ISmsSender
{
    private readonly SmsSenderOptions _options;
    private readonly ILogger<FakeSmsSender> _logger;

    public FakeSmsSender(IOptions<SmsSenderOptions> options, ILogger<FakeSmsSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<SmsSendResult> SendAsync(string recipient, string text, CancellationToken cancellationToken)
    {
        if (_options.ForcePermanentFailure)
        {
            _logger.LogWarning("Fake SMS permanent failure to {Recipient}.", recipient);
            return Task.FromResult(new SmsSendResult(false, true, "Permanent SMS failure"));
        }

        if (_options.ForceTransientFailure)
        {
            _logger.LogWarning("Fake SMS transient failure to {Recipient}.", recipient);
            return Task.FromResult(new SmsSendResult(false, false, "Transient SMS failure"));
        }

        _logger.LogInformation("Fake SMS sent to {Recipient}.", recipient);
        return Task.FromResult(new SmsSendResult(true, false, null));
    }
}
