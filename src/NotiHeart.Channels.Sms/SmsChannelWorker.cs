using Microsoft.Extensions.Hosting;

namespace NotiHeart.Channels.Sms;

public sealed class SmsChannelWorker : BackgroundService
{
    private readonly ILogger<SmsChannelWorker> _logger;

    public SmsChannelWorker(ILogger<SmsChannelWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SMS channel worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Awaiting SMS dispatch tasks.");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
