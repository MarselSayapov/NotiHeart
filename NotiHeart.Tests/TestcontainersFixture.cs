using Microsoft.EntityFrameworkCore;
using NotiHeart.Data;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace NotiHeart.Tests;

public sealed class TestcontainersFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("notiheart")
        .WithUsername("noti")
        .WithPassword("noti")
        .Build();

    public RabbitMqContainer RabbitMq { get; } = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();
        await RabbitMq.StartAsync();

        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(Postgres.GetConnectionString())
            .Options;

        await using var dbContext = new NotificationDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await RabbitMq.DisposeAsync();
        await Postgres.DisposeAsync();
    }
}
