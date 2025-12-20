namespace EmailWorker.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    public string DispatchExchange { get; set; } = "notifications.dispatch";
    public string RetryExchange { get; set; } = "notifications.retry";

    public string QueueName { get; set; } = "notifications.email.q";
    public string RetryQueueName { get; set; } = "notifications.email.retry.q";
    public string DeadLetterQueueName { get; set; } = "notifications.email.dlq";
    public string RoutingKey { get; set; } = "email";
    public int RetryDelayMs { get; set; } = 30000;
}
