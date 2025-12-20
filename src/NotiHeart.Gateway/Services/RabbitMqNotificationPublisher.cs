using System.Text.Json;
using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using RabbitMQ.Client;

namespace NotiHeart.Gateway.Services;

public sealed class RabbitMqNotificationPublisher : INotificationPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqNotificationPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };
        _connection = factory
            .CreateConnectionAsync()
            .GetAwaiter()
            .GetResult();
        _channel = _connection
            .CreateChannelAsync()
            .GetAwaiter()
            .GetResult();
        _channel
            .ExchangeDeclareAsync(_options.Exchange, ExchangeType.Direct, durable: true, autoDelete: false)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<Task> PublishAsync(NotificationDispatchMessage dispatchMessage,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(dispatchMessage);
        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object>
            {
                ["x-attempt"] = dispatchMessage.Attempt,
                ["x-correlation-id"] = dispatchMessage.CorrelationId,
                ["x-notification-id"] = dispatchMessage.NotificationId.ToString()
            }!
        };

        await _channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: NotificationRouting.GetRoutingKey(dispatchMessage.Channel),
            mandatory: false,
            basicProperties: properties,
            body: payload, cancellationToken: cancellationToken);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
