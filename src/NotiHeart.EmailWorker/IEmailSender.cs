using NotiHeart.Persistence;

namespace NotiHeart.EmailWorker;

public interface IEmailSender
{
    Task<bool> SendAsync(string recipient, string text, IReadOnlyCollection<NotificationAttachment> attachments, CancellationToken cancellationToken);
}
