using Microsoft.Extensions.Options;

namespace PushWorker.Push;

public sealed class FakePushSender(IOptions<PushSenderOptions> options, ILogger<FakePushSender> logger) : IPushSender
{
    private readonly PushSenderOptions _options = options.Value;
    private readonly ILogger<FakePushSender> _logger = logger;
    private readonly Random _random = new();

    public Task SendAsync(PushPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fake push send to {DeviceToken} (attachments {Count})",
            payload.DeviceToken,
            payload.AttachmentNames.Count);

        if (_options.ForcePermanentFailure)
        {
            throw new PermanentPushException("Permanent push delivery failure.");
        }

        if (_options.ForceTemporaryFailure || _options.TemporaryFailureRate > 0 && _random.NextDouble() < _options.TemporaryFailureRate)
        {
            throw new TemporaryPushException("Temporary push delivery failure.");
        }

        return Task.CompletedTask;
    }
}
