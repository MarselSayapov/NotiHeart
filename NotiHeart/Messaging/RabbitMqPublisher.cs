using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NotiHeart.Models;
using RabbitMQ.Client;

namespace NotiHeart.Messaging;

public interface INotificationPublisher
{
    void Publish(NotificationDispatchMessage message);
}

public sealed class RabbitMqPublisher : INotificationPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqOptions _options;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Direct, durable: true);
    }

    public void Publish(NotificationDispatchMessage message)
    {
        var payload = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = _channel.CreateBasicProperties();
        properties.CorrelationId = message.CorrelationId;
        properties.Persistent = true;

        var routingKey = message.Channel switch
        {
            NotificationChannel.Email => _options.RoutingKeyEmail,
            NotificationChannel.Sms => _options.RoutingKeySms,
            NotificationChannel.Push => _options.RoutingKeyPush,
            _ => throw new InvalidOperationException("Unsupported channel.")
        };

        _channel.BasicPublish(_options.Exchange, routingKey, properties, body);
    }

    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
    }
}
