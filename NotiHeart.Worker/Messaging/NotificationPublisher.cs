using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NotiHeart.Worker.Models;
using RabbitMQ.Client;

namespace NotiHeart.Worker.Messaging;

public sealed class NotificationPublisher
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;

    public NotificationPublisher(RabbitMqConnection connection, IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    public void PublishToRetry(NotificationDispatchMessage message)
    {
        Publish(_options.RetryExchange, _options.RoutingKey, message);
    }

    public void PublishToDead(NotificationDispatchMessage message)
    {
        Publish(_options.RetryExchange, $"{_options.RoutingKey}.dead", message);
    }

    private void Publish(string exchange, string routingKey, NotificationDispatchMessage message)
    {
        using var channel = _connection.CreateChannel();
        channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable: true);

        var payload = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.CorrelationId = message.CorrelationId;
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object>
        {
            ["x-attempt"] = message.Attempt
        };

        channel.BasicPublish(exchange, routingKey, properties, body);
    }
}
