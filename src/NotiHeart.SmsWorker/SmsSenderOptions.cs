namespace NotiHeart.SmsWorker;

public sealed class SmsSenderOptions
{
    public const string SectionName = "SmsSender";

    public bool ForceTransientFailure { get; init; }

    public bool ForcePermanentFailure { get; init; }
}
