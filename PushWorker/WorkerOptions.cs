namespace PushWorker;

public sealed class WorkerOptions
{
    public string Channel { get; set; } = "Push";
    public int MaxAttempts { get; set; } = 5;
}
