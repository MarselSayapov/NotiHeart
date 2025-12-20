using System.Text.Json;
using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotiHeart.SmsWorker;

public sealed class SmsChannelWorker : BackgroundService, IDisposable
{
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly NotificationProcessingOptions _processingOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SmsChannelWorker> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _queueName;

    public SmsChannelWorker(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<NotificationProcessingOptions> processingOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<SmsChannelWorker> logger)
    {
        _rabbitOptions = rabbitOptions.Value;
        _processingOptions = processingOptions.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _rabbitOptions.Host,
            Port = _rabbitOptions.Port,
            UserName = _rabbitOptions.UserName,
            Password = _rabbitOptions.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateChannel();

        _channel.ExchangeDeclare(_rabbitOptions.Exchange, ExchangeType.Direct, durable: true, autoDelete: false);
        _queueName = NotificationRouting.GetQueueName(NotificationChannel.Sms);
        _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_queueName, _rabbitOptions.Exchange, NotificationRouting.GetRoutingKey(NotificationChannel.Sms));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            await HandleMessageAsync(args, stoppingToken);
        };

        _channel.BasicConsume(_queueName, autoAck: false, consumer);
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<NotificationDispatchMessage>(args.Body.Span);
        if (message is null)
        {
            _channel.BasicAck(args.DeliveryTag, multiple: false);
            return;
        }

        _logger.LogInformation(
            "Processing notification {NotificationId} {CorrelationId} channel {Channel} attempt {AttemptNo}.",
            message.NotificationId,
            message.CorrelationId,
            message.Channel,
            message.AttemptNo);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var notification = await dbContext.Notifications.FindAsync([message.NotificationId], cancellationToken);

        if (notification is null)
        {
            _logger.LogWarning("Notification {NotificationId} not found.", message.NotificationId);
            _channel.BasicAck(args.DeliveryTag, multiple: false);
            return;
        }

        notification.Status = NotificationStatus.Processing;
        notification.UpdatedAt = DateTimeOffset.UtcNow;

        var attempt = new NotificationAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = message.NotificationId,
            AttemptNo = message.AttemptNo,
            Status = NotificationStatus.Processing,
            Timestamp = DateTimeOffset.UtcNow
        };

        var success = !notification.Recipient.Contains("fail", StringComparison.OrdinalIgnoreCase);

        if (success)
        {
            attempt.Status = NotificationStatus.Delivered;
            notification.Status = NotificationStatus.Delivered;
            notification.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            attempt.Status = NotificationStatus.Failed;
            attempt.Error = "Simulated SMS delivery failure.";

            if (message.AttemptNo < _processingOptions.MaxRetries)
            {
                notification.Status = NotificationStatus.Retrying;
                notification.UpdatedAt = DateTimeOffset.UtcNow;
                await ScheduleRetryAsync(message, cancellationToken);
            }
            else
            {
                notification.Status = NotificationStatus.Failed;
                notification.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        dbContext.NotificationAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Completed notification {NotificationId} {CorrelationId} channel {Channel} attempt {AttemptNo} with {Status}.",
            message.NotificationId,
            message.CorrelationId,
            message.Channel,
            message.AttemptNo,
            attempt.Status);

        _channel.BasicAck(args.DeliveryTag, multiple: false);
    }

    private async Task ScheduleRetryAsync(NotificationDispatchMessage message, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(_processingOptions.RetryDelaySeconds), cancellationToken);

        var retryMessage = message with { AttemptNo = message.AttemptNo + 1 };
        var payload = JsonSerializer.SerializeToUtf8Bytes(retryMessage);
        var properties = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };

        _channel.BasicPublish(
            exchange: _rabbitOptions.Exchange,
            routingKey: NotificationRouting.GetRoutingKey(retryMessage.Channel),
            mandatory: false,
            basicProperties: properties,
            body: payload);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
