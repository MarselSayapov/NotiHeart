namespace NotiHeart.Contracts;

public static class NotificationRouting
{
    public static string GetRoutingKey(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => "email",
        NotificationChannel.Sms => "sms",
        NotificationChannel.Push => "push",
        _ => "unknown"
    };

    public static string GetQueueName(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => "notifications.email.q",
        NotificationChannel.Sms => "notifications.sms.q",
        NotificationChannel.Push => "notifications.push.q",
        _ => "notifications.unknown.q"
    };

    public static string GetRetryQueueName(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => "notifications.email.retry.q",
        NotificationChannel.Sms => "notifications.sms.retry.q",
        NotificationChannel.Push => "notifications.push.retry.q",
        _ => "notifications.unknown.retry.q"
    };

    public static string GetDeadLetterExchange(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => "notifications.email.dlx",
        NotificationChannel.Sms => "notifications.sms.dlx",
        NotificationChannel.Push => "notifications.push.dlx",
        _ => "notifications.unknown.dlx"
    };

    public static string GetDeadLetterQueueName(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => "notifications.email.dlq",
        NotificationChannel.Sms => "notifications.sms.dlq",
        NotificationChannel.Push => "notifications.push.dlq",
        _ => "notifications.unknown.dlq"
    };
}
