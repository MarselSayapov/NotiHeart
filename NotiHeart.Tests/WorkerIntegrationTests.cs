using System;
using EmailWorker;
using EmailWorker.Data;
using EmailWorker.Messaging;
using EmailWorker.Models;
using EmailWorker.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace NotiHeart.Tests;

[Collection("integration")]
public sealed class WorkerIntegrationTests(TestcontainersFixture fixture)
{
    private readonly TestcontainersFixture _fixture = fixture;

    [Fact]
    public async Task EmailWorker_ConsumesMessage_AndSetsSentStatus()
    {
        var notificationId = await SeedNotificationAsync();

        using var host = BuildWorkerHost();
        await host.StartAsync();

        PublishMessage(notificationId);

        await WaitForStatusAsync(notificationId, "Sent");

        await host.StopAsync();
    }

    private async Task<Guid> SeedNotificationAsync()
    {
        var options = new DbContextOptionsBuilder<NotiHeart.Data.NotificationDbContext>()
            .UseNpgsql(_fixture.Postgres.GetConnectionString())
            .Options;

        var id = Guid.NewGuid();
        await using var dbContext = new NotiHeart.Data.NotificationDbContext(options);
        dbContext.Notifications.Add(new NotiHeart.Models.Notification
        {
            Id = id,
            Channel = NotiHeart.Models.NotificationChannel.Email,
            Recipient = "user@example.com",
            Text = "Hello",
            Status = NotiHeart.Models.NotificationStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return id;
    }

    private IHost BuildWorkerHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<RabbitMqOptions>(options =>
        {
            options.HostName = _fixture.RabbitMq.Hostname;
            options.Port = _fixture.RabbitMq.GetMappedPublicPort(5672);
            options.QueueName = "notifications.email.q";
            options.RetryQueueName = "notifications.email.retry.q";
            options.DeadLetterQueueName = "notifications.email.dlq";
            options.RoutingKey = "email";
        });
        builder.Services.Configure<WorkerOptions>(options =>
        {
            options.Channel = "Email";
            options.MaxAttempts = 5;
        });
        builder.Services.Configure<EmailSenderOptions>(options =>
        {
            options.ForceTemporaryFailure = false;
            options.TemporaryFailureRate = 0;
        });

        builder.Services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(_fixture.Postgres.GetConnectionString());
        });
        builder.Services.AddSingleton<RabbitMqConnection>();
        builder.Services.AddSingleton<NotificationPublisher>();
        builder.Services.AddSingleton<IEmailSender, FakeEmailSender>();
        builder.Services.AddHostedService<NotificationWorker>();

        return builder.Build();
    }

    private void PublishMessage(Guid notificationId)
    {
        var factory = new ConnectionFactory
        {
            HostName = _fixture.RabbitMq.Hostname,
            Port = _fixture.RabbitMq.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare("notifications.dispatch", ExchangeType.Direct, durable: true);

        var message = new NotificationDispatchMessage
        {
            NotificationId = notificationId,
            Channel = NotificationChannel.Email,
            Recipient = "user@example.com",
            Text = "Hello",
            AttachmentIds = Array.Empty<Guid>(),
            CorrelationId = Guid.NewGuid().ToString("N"),
            Attempt = 1
        };
        var payload = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.CorrelationId = message.CorrelationId;
        properties.Persistent = true;

        channel.BasicPublish("notifications.dispatch", "email", properties, body);
    }

    private async Task WaitForStatusAsync(Guid notificationId, string expectedStatus)
    {
        var timeout = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < timeout)
        {
            await using var connection = new NpgsqlConnection(_fixture.Postgres.GetConnectionString());
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT status FROM notifications WHERE id = @id", connection);
            command.Parameters.AddWithValue("id", notificationId);
            var status = (string?)await command.ExecuteScalarAsync();
            if (status == expectedStatus)
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Expected status '{expectedStatus}' not reached.");
    }
}
