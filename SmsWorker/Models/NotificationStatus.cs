namespace SmsWorker.Models;

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
