namespace EmailWorker;

public sealed class WorkerOptions
{
    public string Channel { get; set; } = "Email";
    public int MaxAttempts { get; set; } = 5;
}
