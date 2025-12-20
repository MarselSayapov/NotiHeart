using System.Text;
using System.Text.Json;
using EmailWorker.Data;
using EmailWorker.Email;
using EmailWorker.Messaging;
using EmailWorker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EmailWorker;

public sealed class NotificationWorker : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly WorkerOptions _workerOptions;
    private readonly NotificationPublisher _publisher;
    private readonly IEmailSender _emailSender;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationWorker> _logger;
    private IModel? _channel;

    public NotificationWorker(
        RabbitMqConnection connection,
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<WorkerOptions> workerOptions,
        NotificationPublisher publisher,
        IEmailSender emailSender,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationWorker> logger)
    {
        _connection = connection;
        _rabbitOptions = rabbitOptions.Value;
        _workerOptions = workerOptions.Value;
        _publisher = publisher;
        _emailSender = emailSender;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateChannel();
        _channel.ExchangeDeclare(_rabbitOptions.DispatchExchange, ExchangeType.Direct, durable: true);
        _channel.ExchangeDeclare(_rabbitOptions.RetryExchange, ExchangeType.Direct, durable: true);

        _channel.QueueDeclare(_rabbitOptions.QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_rabbitOptions.QueueName, _rabbitOptions.DispatchExchange, _rabbitOptions.RoutingKey);

        _channel.QueueDeclare(
            _rabbitOptions.RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = _rabbitOptions.RetryDelayMs,
                ["x-dead-letter-exchange"] = _rabbitOptions.DispatchExchange,
                ["x-dead-letter-routing-key"] = _rabbitOptions.RoutingKey
            });
        _channel.QueueBind(_rabbitOptions.RetryQueueName, _rabbitOptions.RetryExchange, _rabbitOptions.RoutingKey);

        _channel.QueueDeclare(_rabbitOptions.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_rabbitOptions.DeadLetterQueueName, _rabbitOptions.RetryExchange, $"{_rabbitOptions.RoutingKey}.dead");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;
        _channel.BasicConsume(_rabbitOptions.QueueName, autoAck: false, consumer);

        _logger.LogInformation("Email worker started on queue {Queue}", _rabbitOptions.QueueName);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var correlationId = args.BasicProperties.CorrelationId ?? Guid.NewGuid().ToString("N");
        NotificationDispatchMessage? message = null;

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            message = JsonSerializer.Deserialize<NotificationDispatchMessage>(json);
            if (message is null)
            {
                throw new InvalidOperationException("Message payload is empty.");
            }

            var attemptNo = GetAttemptNumber(message, args.BasicProperties?.Headers);
            await UpdateStatusAsync(message.NotificationId, NotificationStatus.Sending, null, CancellationToken.None);
            await InsertAttemptAsync(message.NotificationId, attemptNo, "Started", null, CancellationToken.None);

            var attachments = await LoadAttachmentsAsync(message.AttachmentIds, CancellationToken.None);
            var emailAttachments = attachments.Select(a => new EmailAttachment
            {
                FileName = a.FileName,
                ContentType = a.ContentType,
                Content = a.Content,
                Size = a.Size
            }).ToList();

            await _emailSender.SendAsync(message.Recipient, message.Text, emailAttachments, CancellationToken.None);

            await UpdateStatusAsync(message.NotificationId, NotificationStatus.Sent, null, CancellationToken.None);
            await CompleteAttemptAsync(message.NotificationId, attemptNo, "Sent", null, CancellationToken.None);
            _channel?.BasicAck(args.DeliveryTag, false);
            _logger.LogInformation(
                "Notification sent {NotificationId} {CorrelationId}",
                message.NotificationId,
                correlationId);
        }
        catch (TemporaryEmailException ex) when (message is not null)
        {
            await HandleFailureAsync(message, GetAttemptNumber(message, args.BasicProperties?.Headers), ex.Message, correlationId, isRetryable: true);
            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex) when (message is not null)
        {
            await HandleFailureAsync(message, GetAttemptNumber(message, args.BasicProperties?.Headers), ex.Message, correlationId, isRetryable: false);
            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing message {CorrelationId}", correlationId);
            _channel?.BasicNack(args.DeliveryTag, false, requeue: true);
        }
    }

    private async Task HandleFailureAsync(
        NotificationDispatchMessage message,
        int attemptNo,
        string error,
        string correlationId,
        bool isRetryable)
    {
        await CompleteAttemptAsync(message.NotificationId, attemptNo, "Failed", error, CancellationToken.None);

        var decision = RetryDecider.Decide(isRetryable, attemptNo, _workerOptions.MaxAttempts);

        if (decision.Action == RetryAction.Retry)
        {
            await UpdateStatusAsync(message.NotificationId, NotificationStatus.RetryScheduled, error, CancellationToken.None);
            _publisher.PublishToRetry(new NotificationDispatchMessage
            {
                NotificationId = message.NotificationId,
                Channel = message.Channel,
                Recipient = message.Recipient,
                Text = message.Text,
                AttachmentIds = message.AttachmentIds,
                CorrelationId = message.CorrelationId,
                Attempt = decision.NextAttempt
            });
            _logger.LogWarning(
                "Retry scheduled {NotificationId} Attempt {Attempt} {CorrelationId}",
                message.NotificationId,
                decision.NextAttempt,
                correlationId);
            return;
        }

        var terminalStatus = decision.Action == RetryAction.Dead
            ? NotificationStatus.Dead
            : NotificationStatus.Failed;
        await UpdateStatusAsync(message.NotificationId, terminalStatus, error, CancellationToken.None);
        _publisher.PublishToDead(message);
        _logger.LogWarning(
            "Notification dead-lettered {NotificationId} {CorrelationId}",
            message.NotificationId,
            correlationId);
    }

    private async Task<List<Models.NotificationAttachment>> LoadAttachmentsAsync(Guid[] attachmentIds, CancellationToken cancellationToken)
    {
        if (attachmentIds.Length == 0)
        {
            return [];
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        return await dbContext.NotificationAttachments
            .AsNoTracking()
            .Where(a => attachmentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
    }

    private async Task UpdateStatusAsync(
        Guid notificationId,
        NotificationStatus status,
        string? error,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
        if (notification is null)
        {
            return;
        }

        notification.Status = status;
        notification.LastError = error;
        notification.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task InsertAttemptAsync(
        Guid notificationId,
        int attemptNo,
        string result,
        string? error,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var attempt = new NotificationAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            AttemptNo = attemptNo,
            StartedAt = DateTime.UtcNow,
            Result = result,
            Error = error
        };

        dbContext.NotificationAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CompleteAttemptAsync(
        Guid notificationId,
        int attemptNo,
        string result,
        string? error,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var attempt = await dbContext.NotificationAttempts
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefaultAsync(a => a.NotificationId == notificationId && a.AttemptNo == attemptNo, cancellationToken);

        if (attempt is null)
        {
            return;
        }

        attempt.FinishedAt = DateTime.UtcNow;
        attempt.Result = result;
        attempt.Error = error;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static int GetAttemptNumber(NotificationDispatchMessage message, IDictionary<string, object>? headers)
    {
        if (message.Attempt > 0)
        {
            return message.Attempt;
        }

        if (headers is not null && headers.TryGetValue("x-attempt", out var headerValue))
        {
            if (headerValue is byte[] bytes && int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed))
            {
                return parsed;
            }

            if (headerValue is int intValue)
            {
                return intValue;
            }
        }

        return 1;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
