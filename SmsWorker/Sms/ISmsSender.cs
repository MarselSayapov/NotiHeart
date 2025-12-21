namespace SmsWorker.Sms;

public interface ISmsSender
{
    Task SendAsync(string recipient, string text, CancellationToken cancellationToken);
}
