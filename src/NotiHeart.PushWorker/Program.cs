using Microsoft.EntityFrameworkCore;
using NotiHeart.Contracts;
using NotiHeart.Persistence;
using NotiHeart.PushWorker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<NotificationProcessingOptions>(
    builder.Configuration.GetSection(NotificationProcessingOptions.SectionName));
builder.Services.Configure<PushSenderOptions>(
    builder.Configuration.GetSection(PushSenderOptions.SectionName));

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Notifications")));

builder.Services.AddHostedService<PushChannelWorker>();
builder.Services.AddSingleton<IPushSender, FakePushSender>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    dbContext.Database.Migrate();
}

host.Run();
