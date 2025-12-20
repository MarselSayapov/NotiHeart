using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Serilog.Context;

namespace NotiHeart.WorkerBase;

public abstract class ChannelWorkerBase : BackgroundService, IDisposable
{
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly NotificationProcessingOptions _processingOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _queueName;
    private readonly string _retryQueueName;
    private readonly string _deadLetterQueueName;
    private readonly string _deadLetterExchange;
    private readonly string _routingKey;

    protected ChannelWorkerBase(
        NotificationChannel channel,
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<NotificationProcessingOptions> processingOptions,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        Channel = channel;
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

        _routingKey = NotificationRouting.GetRoutingKey(channel);
        _queueName = NotificationRouting.GetQueueName(channel);
        _retryQueueName = NotificationRouting.GetRetryQueueName(channel);
        _deadLetterQueueName = NotificationRouting.GetDeadLetterQueueName(channel);
        _deadLetterExchange = NotificationRouting.GetDeadLetterExchange(channel);

        DeclareTopology();
    }

    protected NotificationChannel Channel { get; }

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

    protected abstract Task<SendResult> SendAsync(
        Notification notification,
        IReadOnlyCollection<NotificationAttachment> attachments,
        CancellationToken cancellationToken);

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

    private static async Task<IReadOnlyCollection<NotificationAttachment>> LoadAttachmentsAsync(
        NotificationDbContext dbContext,
        Guid[] attachmentIds,
        CancellationToken cancellationToken)
    {
        if (attachmentIds.Length == 0)
        {
            return Array.Empty<NotificationAttachment>();
        }

        return await dbContext.NotificationAttachments
            .Where(attachment => attachmentIds.Contains(attachment.Id))
            .ToListAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<NotificationDispatchMessage>(args.Body.Span);
        if (message is null)
        {
            _channel.BasicAck(args.DeliveryTag, multiple: false);
            return;
        }

        using (LogContext.PushProperty("NotificationId", message.NotificationId))
        using (LogContext.PushProperty("CorrelationId", message.CorrelationId))
        using (LogContext.PushProperty("Channel", message.Channel))
        using (LogContext.PushProperty("AttemptNo", message.Attempt))
        {
            _logger.LogInformation("Processing notification message.");

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var notification = await dbContext.Notifications
                .FirstOrDefaultAsync(item => item.Id == message.NotificationId, cancellationToken);

            if (notification is null)
            {
                _logger.LogWarning("Notification not found.");
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

            var attachments = await LoadAttachmentsAsync(dbContext, message.AttachmentIds, cancellationToken);
            SendResult sendResult;
            try
            {
                sendResult = await SendAsync(notification, attachments, cancellationToken);
            }
            catch (Exception ex)
            {
                sendResult = new SendResult(false, ex.Message, "Transient");
            }

            if (sendResult.Success)
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
                attempt.Error = sendResult.Error ?? "Send failed.";
                attempt.FinishedAt = DateTimeOffset.UtcNow;
                notification.LastError = attempt.Error;

                if (message.Attempt < _processingOptions.MaxRetries)
                {
                    notification.Status = NotificationStatus.RetryScheduled;
                    notification.UpdatedAt = DateTimeOffset.UtcNow;
                    await ScheduleRetryAsync(message, cancellationToken, sendResult.ErrorType);
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

            _logger.LogInformation("Completed notification processing.");
            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
    }

    private Task ScheduleRetryAsync(NotificationDispatchMessage message, CancellationToken cancellationToken, string errorType)
    {
        var retryMessage = message with { Attempt = message.Attempt + 1 };
        var payload = JsonSerializer.SerializeToUtf8Bytes(retryMessage);
        var properties = BuildProperties(retryMessage, errorType);

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _retryQueueName,
            mandatory: false,
            basicProperties: properties,
            body: payload);

        return Task.CompletedTask;
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

    private static BasicProperties BuildProperties(NotificationDispatchMessage message, string errorType)
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

public sealed record SendResult(bool Success, string? Error, string ErrorType)
{
    public static SendResult Ok() => new(true, null, "None");
    public static SendResult Fail(string? error, string errorType = "Transient") => new(false, error, errorType);
}
