namespace PushWorker.Push;

public sealed class PushPayload
{
    public string DeviceToken { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public IReadOnlyCollection<string> AttachmentNames { get; init; } = Array.Empty<string>();
}
