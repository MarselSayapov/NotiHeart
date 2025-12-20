namespace NotiHeart.Worker;

public sealed class WorkerOptions
{
    public string Channel { get; set; } = "Email";
    public int MaxAttempts { get; set; } = 5;
    public bool MockAlwaysFail { get; set; }
    public bool MockPermanentFailure { get; set; }
}
