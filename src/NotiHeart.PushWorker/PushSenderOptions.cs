namespace NotiHeart.PushWorker;

public sealed class PushSenderOptions
{
    public const string SectionName = "PushSender";

    public bool ForceFailure { get; init; }
}
