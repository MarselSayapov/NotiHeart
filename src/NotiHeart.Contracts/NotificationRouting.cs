namespace NotiHeart.Contracts;

public static class NotificationRouting
{
    public static string GetRoutingKey(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => "notifications.email",
        NotificationChannel.Sms => "notifications.sms",
        NotificationChannel.Push => "notifications.push",
        NotificationChannel.Messenger => "notifications.messenger",
        _ => "notifications.unknown"
    };

    public static string GetQueueName(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => "notifications.email.queue",
        NotificationChannel.Sms => "notifications.sms.queue",
        NotificationChannel.Push => "notifications.push.queue",
        NotificationChannel.Messenger => "notifications.messenger.queue",
        _ => "notifications.unknown.queue"
    };
}
