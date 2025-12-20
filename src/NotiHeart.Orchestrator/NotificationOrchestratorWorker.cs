using Microsoft.Extensions.Hosting;

namespace NotiHeart.Orchestrator;

public sealed class NotificationOrchestratorWorker : BackgroundService
{
    private readonly ILogger<NotificationOrchestratorWorker> _logger;

    public NotificationOrchestratorWorker(ILogger<NotificationOrchestratorWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification orchestrator started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Waiting for notification events from the broker.");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
