namespace EmailWorker.Email;

public interface IEmailSender
{
    Task SendAsync(string recipient, string text, IReadOnlyCollection<EmailAttachment> attachments, CancellationToken cancellationToken);
}
