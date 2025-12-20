using Microsoft.AspNetCore.Mvc;
using NotiHeart.Contracts;
using NotiHeart.Gateway.Models;
using NotiHeart.Gateway.Services;

namespace NotiHeart.Gateway.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationPublisher _publisher;

    public NotificationsController(INotificationPublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost]
    [ProducesResponseType(typeof(NotificationAcceptedResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<NotificationAcceptedResponse>> Create(
        [FromBody] NotificationRequest request,
        CancellationToken cancellationToken)
    {
        var envelope = new NotificationEnvelope(Guid.NewGuid(), request);
        await _publisher.PublishAsync(envelope, cancellationToken);

        var response = new NotificationAcceptedResponse(
            envelope.Id,
            envelope.CorrelationId,
            envelope.CreatedAt,
            NotificationStatus.Pending.ToString());

        return Accepted(response);
    }
}
