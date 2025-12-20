namespace NotiHeart.SmsWorker;

public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(string recipient, string text, CancellationToken cancellationToken);
}
