using Microsoft.Extensions.Hosting;

namespace NotiHeart.Channels.Email;

public sealed class EmailChannelWorker : BackgroundService
{
    private readonly ILogger<EmailChannelWorker> _logger;

    public EmailChannelWorker(ILogger<EmailChannelWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email channel worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Awaiting email dispatch tasks.");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
