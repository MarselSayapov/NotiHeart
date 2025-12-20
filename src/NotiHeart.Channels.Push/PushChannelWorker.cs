using Microsoft.Extensions.Hosting;

namespace NotiHeart.Channels.Push;

public sealed class PushChannelWorker : BackgroundService
{
    private readonly ILogger<PushChannelWorker> _logger;

    public PushChannelWorker(ILogger<PushChannelWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Push channel worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Awaiting push dispatch tasks.");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
