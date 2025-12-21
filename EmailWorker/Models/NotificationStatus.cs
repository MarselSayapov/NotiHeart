namespace EmailWorker.Models;

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
