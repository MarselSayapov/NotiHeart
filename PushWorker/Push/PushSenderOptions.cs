namespace PushWorker.Push;

public sealed class PushSenderOptions
{
    public bool ForceTemporaryFailure { get; set; }
    public bool ForcePermanentFailure { get; set; }
    public double TemporaryFailureRate { get; set; }
}
