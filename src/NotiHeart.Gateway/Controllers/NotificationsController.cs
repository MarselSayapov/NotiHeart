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

    [HttpPost]
    [ProducesResponseType(typeof(NotificationAcceptedResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<NotificationAcceptedResponse>> Create(
        [FromBody] NotificationRequest request,
        CancellationToken cancellationToken)
    {
        var notificationId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var metadata = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata);

        var notification = new Notification
        {
            Id = notificationId,
            CorrelationId = correlationId,
            Channel = request.Channel,
            Recipient = request.Recipient,
            Message = request.Message,
            Metadata = metadata,
            Status = NotificationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.Attachments is { Count: > 0 })
        {
            foreach (var attachment in request.Attachments)
            {
                notification.Attachments.Add(new NotificationAttachment
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notificationId,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    Content = attachment.Content
                });
            }
        }

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var envelope = new NotificationEnvelope(notificationId, request)
        {
            CorrelationId = correlationId
        };

        await _publisher.PublishAsync(envelope, cancellationToken);

        _logger.LogInformation(
            "Gateway accepted notification {NotificationId} {CorrelationId} for {Channel}.",
            notificationId,
            correlationId,
            request.Channel);

        var response = new NotificationAcceptedResponse(
            notificationId,
            correlationId,
            now,
            NotificationStatus.Pending.ToString());

        return Accepted(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationSummaryResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _dbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        return Ok(new NotificationSummaryResponse(
            notification.Id,
            notification.CorrelationId,
            notification.Channel,
            notification.Status,
            notification.CreatedAt,
            notification.UpdatedAt));
    }
}
