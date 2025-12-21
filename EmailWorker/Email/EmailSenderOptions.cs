namespace EmailWorker.Email;

public sealed class EmailSenderOptions
{
    public bool ForceTemporaryFailure { get; set; }
    public double TemporaryFailureRate { get; set; }
}
