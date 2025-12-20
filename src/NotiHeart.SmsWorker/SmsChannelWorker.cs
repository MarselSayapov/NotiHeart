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
    private readonly string _retryQueueName;
    private readonly string _deadLetterQueueName;
    private readonly string _deadLetterExchange;
    private readonly string _routingKey;

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

        _routingKey = NotificationRouting.GetRoutingKey(NotificationChannel.Sms);
        _queueName = NotificationRouting.GetQueueName(NotificationChannel.Sms);
        _retryQueueName = NotificationRouting.GetRetryQueueName(NotificationChannel.Sms);
        _deadLetterQueueName = NotificationRouting.GetDeadLetterQueueName(NotificationChannel.Sms);
        _deadLetterExchange = NotificationRouting.GetDeadLetterExchange(NotificationChannel.Sms);

        DeclareTopology();
    }

    private void DeclareTopology()
    {
        _channel.ExchangeDeclare(_rabbitOptions.Exchange, ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.ExchangeDeclare(_deadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false);

        _channel.QueueDeclare(_deadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_deadLetterQueueName, _deadLetterExchange, _routingKey);

        _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _deadLetterExchange,
            ["x-dead-letter-routing-key"] = _routingKey
        });
        _channel.QueueBind(_queueName, _rabbitOptions.Exchange, _routingKey);

        _channel.QueueDeclare(_retryQueueName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            ["x-message-ttl"] = _processingOptions.RetryDelaySeconds * 1000,
            ["x-dead-letter-exchange"] = _rabbitOptions.Exchange,
            ["x-dead-letter-routing-key"] = _routingKey
        });
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
            message.Attempt);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var notification = await dbContext.Notifications.FindAsync([message.NotificationId], cancellationToken);

        if (notification is null)
        {
            _logger.LogWarning("Notification {NotificationId} not found.", message.NotificationId);
            _channel.BasicAck(args.DeliveryTag, multiple: false);
            return;
        }

        notification.Status = NotificationStatus.Sending;
        notification.UpdatedAt = DateTimeOffset.UtcNow;

        var attempt = new NotificationAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = message.NotificationId,
            AttemptNo = message.Attempt,
            Result = NotificationStatus.Sending,
            StartedAt = DateTimeOffset.UtcNow
        };

        var success = !notification.Recipient.Contains("fail", StringComparison.OrdinalIgnoreCase);

        if (success)
        {
            attempt.Result = NotificationStatus.Sent;
            attempt.FinishedAt = DateTimeOffset.UtcNow;
            notification.Status = NotificationStatus.Sent;
            notification.UpdatedAt = DateTimeOffset.UtcNow;
            notification.LastError = null;
        }
        else
        {
            attempt.Result = NotificationStatus.Failed;
            attempt.Error = "Simulated SMS delivery failure.";
            attempt.FinishedAt = DateTimeOffset.UtcNow;
            notification.LastError = attempt.Error;

            if (message.Attempt < _processingOptions.MaxRetries)
            {
                notification.Status = NotificationStatus.RetryScheduled;
                notification.UpdatedAt = DateTimeOffset.UtcNow;
                await ScheduleRetryAsync(message, cancellationToken, "Transient");
            }
            else
            {
                notification.Status = NotificationStatus.Dead;
                notification.UpdatedAt = DateTimeOffset.UtcNow;
                await PublishToDeadLetterQueueAsync(message, "MaxAttemptsExceeded");
            }
        }

        dbContext.NotificationAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Completed notification {NotificationId} {CorrelationId} channel {Channel} attempt {AttemptNo} with {Status}.",
            message.NotificationId,
            message.CorrelationId,
            message.Channel,
            message.Attempt,
            attempt.Result);

        _channel.BasicAck(args.DeliveryTag, multiple: false);
    }

    private async Task ScheduleRetryAsync(NotificationDispatchMessage message, CancellationToken cancellationToken, string errorType)
    {
        await Task.Delay(TimeSpan.FromSeconds(_processingOptions.RetryDelaySeconds), cancellationToken);

        var retryMessage = message with { Attempt = message.Attempt + 1 };
        var payload = JsonSerializer.SerializeToUtf8Bytes(retryMessage);
        var properties = BuildProperties(retryMessage, errorType);

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _retryQueueName,
            mandatory: false,
            basicProperties: properties,
            body: payload);
    }

    private Task PublishToDeadLetterQueueAsync(NotificationDispatchMessage message, string errorType)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = BuildProperties(message, errorType);
        _channel.BasicPublish(
            exchange: _deadLetterExchange,
            routingKey: _routingKey,
            mandatory: false,
            basicProperties: properties,
            body: payload);
        return Task.CompletedTask;
    }

    private BasicProperties BuildProperties(NotificationDispatchMessage message, string errorType)
    {
        return new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object>
            {
                ["x-attempt"] = message.Attempt,
                ["x-correlation-id"] = message.CorrelationId,
                ["x-notification-id"] = message.NotificationId.ToString(),
                ["x-error-type"] = errorType
            }
        };
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
