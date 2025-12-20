using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EmailWorker.Messaging;

public sealed class RabbitMqConnection : IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
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

        _connection = factory.CreateConnection();
    }

    public IModel CreateChannel() => _connection.CreateModel();

    public void Dispose()
    {
        _connection.Close();
    }
}
