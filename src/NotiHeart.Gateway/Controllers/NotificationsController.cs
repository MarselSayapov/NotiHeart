using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotiHeart.Contracts;
using NotiHeart.Gateway.Models;
using NotiHeart.Gateway.Services;
using NotiHeart.Persistence;

namespace NotiHeart.Gateway.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationPublisher _publisher;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        NotificationDbContext dbContext,
        INotificationPublisher publisher,
        ILogger<NotificationsController> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("send")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(NotificationAcceptedResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<NotificationAcceptedResponse>> Create(
        [FromForm] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var notificationId = Guid.NewGuid();
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId.Trim();
        var now = DateTimeOffset.UtcNow;
        var metadata = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata);

        var notification = new Notification
        {
            Id = notificationId,
            CorrelationId = correlationId,
            Channel = request.Channel,
            Recipient = request.Recipient,
            Text = request.Text,
            Metadata = metadata,
            Status = NotificationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.Attachments is not null)
        {
            foreach (var attachment in request.Attachments)
            {
                if (attachment.Length == 0)
                {
                    continue;
                }

                await using var stream = new MemoryStream();
                await attachment.CopyToAsync(stream, cancellationToken);

                notification.Attachments.Add(new NotificationAttachment
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notificationId,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    Content = stream.ToArray(),
                    Size = attachment.Length
                });
            }
        }

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var attachmentIds = notification.Attachments.Select(item => item.Id).ToArray();
        var dispatch = new NotificationDispatchMessage(
            notificationId,
            notification.Channel,
            notification.Recipient,
            notification.Text,
            attachmentIds,
            correlationId,
            Attempt: 1);

        await _publisher.PublishAsync(dispatch, cancellationToken);

        notification.Status = NotificationStatus.Queued;
        notification.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Gateway accepted notification {NotificationId} {CorrelationId} for {Channel}.",
            notificationId,
            correlationId,
            request.Channel);

        var response = new NotificationAcceptedResponse(
            notificationId,
            correlationId,
            now,
            NotificationStatus.Queued.ToString());

        return Accepted(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDetailsResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications
            .Include(item => item.Attempts)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        var attempts = notification.Attempts
            .OrderBy(attempt => attempt.AttemptNo)
            .Select(attempt => new NotificationAttemptResponse(
                attempt.AttemptNo,
                attempt.Result,
                attempt.StartedAt,
                attempt.FinishedAt,
                attempt.Error))
            .ToArray();

        return Ok(new NotificationDetailsResponse(
            notification.Id,
            notification.CorrelationId,
            notification.Channel,
            notification.Recipient,
            notification.Text,
            notification.Status,
            notification.LastError,
            notification.CreatedAt,
            notification.UpdatedAt,
            attempts));
    }
}
