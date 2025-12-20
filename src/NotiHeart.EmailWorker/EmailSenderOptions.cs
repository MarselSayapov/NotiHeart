namespace NotiHeart.EmailWorker;

public sealed class EmailSenderOptions
{
    public const string SectionName = "EmailSender";

    public bool ForceFailure { get; init; }

    public double FailureRate { get; init; }
}
