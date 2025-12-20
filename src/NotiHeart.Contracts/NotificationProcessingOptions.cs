namespace NotiHeart.Contracts;

public sealed class NotificationProcessingOptions
{
    public const string SectionName = "NotificationProcessing";

    public int MaxRetries { get; init; } = 3;
    public int RetryDelaySeconds { get; init; } = 5;
}
