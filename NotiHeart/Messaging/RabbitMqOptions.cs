namespace NotiHeart.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "notifications.dispatch";
    public string RoutingKeyEmail { get; set; } = "email";
    public string RoutingKeySms { get; set; } = "sms";
    public string RoutingKeyPush { get; set; } = "push";
}
