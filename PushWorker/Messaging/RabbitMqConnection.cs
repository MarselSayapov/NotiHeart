using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PushWorker.Messaging;

public sealed class RabbitMqConnection : IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        var config = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = CreateConnectionWithRetry(factory, logger);
    }

    public IModel CreateChannel() => _connection.CreateModel();

    public void Dispose()
    {
        _connection.Close();
    }

    private static IConnection CreateConnectionWithRetry(ConnectionFactory factory, ILogger logger)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                return factory.CreateConnection();
            }
            catch (Exception ex)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, attempt * 2));
                logger.LogWarning(ex, "RabbitMQ connection failed (attempt {Attempt}). Retrying in {DelaySeconds}s", attempt, delay.TotalSeconds);
                Thread.Sleep(delay);
            }
        }
    }
}
