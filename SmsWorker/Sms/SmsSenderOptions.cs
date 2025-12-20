namespace SmsWorker.Sms;

public sealed class SmsSenderOptions
{
    public bool ForceTemporaryFailure { get; set; }
    public bool ForcePermanentFailure { get; set; }
    public double TemporaryFailureRate { get; set; }
}
