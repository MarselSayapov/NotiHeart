using Microsoft.Extensions.Options;

namespace NotiHeart.PushWorker;

public sealed class FakePushSender : IPushSender
{
    private readonly PushSenderOptions _options;
    private readonly ILogger<FakePushSender> _logger;

    public FakePushSender(IOptions<PushSenderOptions> options, ILogger<FakePushSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> SendAsync(
        string deviceToken,
        string text,
        IReadOnlyCollection<string> attachmentNames,
        CancellationToken cancellationToken)
    {
        if (_options.ForceFailure)
        {
            _logger.LogWarning("Fake push failure for {DeviceToken}.", deviceToken);
            return Task.FromResult(false);
        }

        _logger.LogInformation(
            "Fake push sent to {DeviceToken} with {AttachmentCount} attachment names.",
            deviceToken,
            attachmentNames.Count);

        return Task.FromResult(true);
    }
}
