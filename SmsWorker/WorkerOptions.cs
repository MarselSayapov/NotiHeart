namespace SmsWorker;

public sealed class WorkerOptions
{
    public string Channel { get; set; } = "Sms";
    public int MaxAttempts { get; set; } = 5;
}
