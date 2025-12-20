using System.Text;
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

        _connection = factory.CreateConnection();
        _channel = _connection.CreateChannel();
        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Direct, durable: true, autoDelete: false);
    }

    public Task PublishAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        var dispatch = new NotificationDispatchMessage(
            envelope.Id,
            envelope.CorrelationId,
            envelope.Request.Channel,
            attemptNo: 1);

        var payload = JsonSerializer.SerializeToUtf8Bytes(dispatch);
        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        _channel.BasicPublish(
            exchange: _options.Exchange,
            routingKey: NotificationRouting.GetRoutingKey(dispatch.Channel),
            mandatory: false,
            basicProperties: properties,
            body: payload);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
