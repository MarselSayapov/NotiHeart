namespace NotiHeart.Contracts;

public enum NotificationStatus
{
    Pending,
    Queued,
    Sending,
    Sent,
    Failed,
    RetryScheduled,
    Dead
}
