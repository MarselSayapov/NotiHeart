using Microsoft.EntityFrameworkCore;
using NotiHeart.Contracts;
using NotiHeart.EmailWorker;
using NotiHeart.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<NotificationProcessingOptions>(
    builder.Configuration.GetSection(NotificationProcessingOptions.SectionName));

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Notifications")));

builder.Services.AddHostedService<EmailChannelWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    dbContext.Database.EnsureCreated();
}

host.Run();
