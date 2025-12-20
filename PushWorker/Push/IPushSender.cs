namespace PushWorker.Push;

public interface IPushSender
{
    Task SendAsync(PushPayload payload, CancellationToken cancellationToken);
}
