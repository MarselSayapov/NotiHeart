using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotiHeart.Data;
using NotiHeart.Messaging;
using NotiHeart.Models;

namespace NotiHeart.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(
    NotificationDbContext dbContext,
    INotificationPublisher publisher,
    ILogger<NotificationsController> logger) : ControllerBase
{
    [HttpPost("send")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Send([FromForm] SendNotificationRequest request, CancellationToken cancellationToken)
    {
        var correlationId = GetOrCreateCorrelationId();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Channel = request.Channel,
            Recipient = request.Recipient,
            Text = request.Text,
            Status = NotificationStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var attachment in request.Attachments)
        {
            await using var stream = attachment.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);

            notification.Attachments.Add(new NotificationAttachment
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType ?? "application/octet-stream",
                Content = memory.ToArray(),
                Size = attachment.Length
            });
        }

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        var message = new NotificationDispatchMessage
        {
            NotificationId = notification.Id,
            Channel = notification.Channel,
            Recipient = notification.Recipient,
            Text = notification.Text,
            AttachmentIds = notification.Attachments.Select(a => a.Id).ToArray(),
            CorrelationId = correlationId,
            Attempt = 1
        };

        publisher.Publish(message);
        logger.LogInformation("Notification queued {NotificationId} {Channel} {CorrelationId}",
            notification.Id, notification.Channel, correlationId);

        return Accepted(new { notificationId = notification.Id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .AsNoTracking()
            .Include(n => n.Attempts)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            notification.Id,
            notification.Status,
            Attempts = notification.Attempts
                .OrderBy(a => a.AttemptNo)
                .Select(a => new
                {
                    a.AttemptNo,
                    a.StartedAt,
                    a.FinishedAt,
                    a.Result,
                    a.Error
                })
        });
    }

    private string GetOrCreateCorrelationId()
    {
        if (Request.Headers.TryGetValue("X-Correlation-Id", out var values) && values.Count > 0)
        {
            return values[0]!;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        Response.Headers["X-Correlation-Id"] = correlationId;
        return correlationId;
    }
}
