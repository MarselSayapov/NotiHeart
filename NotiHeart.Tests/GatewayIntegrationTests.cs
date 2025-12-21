using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RabbitMQ.Client;
using Xunit;

namespace NotiHeart.Tests;

[Collection("integration")]
public sealed class GatewayIntegrationTests(TestcontainersFixture fixture) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly TestcontainersFixture _fixture = fixture;

    [Fact]
    public async Task SendNotification_PersistsToDb_AndPublishesToRabbit()
    {
        await using var appFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = _fixture.Postgres.GetConnectionString(),
                        ["RabbitMq:HostName"] = _fixture.RabbitMq.Hostname,
                        ["RabbitMq:Port"] = _fixture.RabbitMq.GetMappedPublicPort(5672).ToString()
                    });
                });
            });

        using var connection = CreateRabbitConnection();
        using var channel = connection.CreateModel();
        var queueName = channel.QueueDeclare().QueueName;
        channel.ExchangeDeclare("notifications.dispatch", ExchangeType.Direct, durable: true);
        channel.QueueBind(queueName, "notifications.dispatch", "email");

        var client = appFactory.CreateClient();
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Email"), "channel");
        content.Add(new StringContent("user@example.com"), "recipient");
        content.Add(new StringContent("Hello"), "text");
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("data"))
        {
            Headers = { ContentType = new MediaTypeHeaderValue("text/plain") }
        }, "attachments", "note.txt");

        var response = await client.PostAsync("/api/notifications/send", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var notificationId = json.RootElement.GetProperty("notificationId").GetGuid();

        await AssertNotificationPersistedAsync(notificationId);

        var result = channel.BasicGet(queueName, autoAck: true);
        Assert.NotNull(result);
    }

    private IConnection CreateRabbitConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _fixture.RabbitMq.Hostname,
            Port = _fixture.RabbitMq.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest"
        };
        return factory.CreateConnection();
    }

    private async Task AssertNotificationPersistedAsync(Guid notificationId)
    {
        await using var connection = new NpgsqlConnection(_fixture.Postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM notifications WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", notificationId);
        var count = (long)await command.ExecuteScalarAsync();
        Assert.Equal(1, count);
    }
}
