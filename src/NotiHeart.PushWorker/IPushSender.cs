namespace NotiHeart.PushWorker;

public interface IPushSender
{
    Task<bool> SendAsync(string deviceToken, string text, IReadOnlyCollection<string> attachmentNames, CancellationToken cancellationToken);
}
