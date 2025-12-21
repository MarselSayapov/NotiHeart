using Microsoft.EntityFrameworkCore;
using PushWorker;
using PushWorker.Data;
using PushWorker.Messaging;
using PushWorker.Push;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<NotificationWorker>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<PushSenderOptions>(builder.Configuration.GetSection("PushSender"));

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<NotificationPublisher>();
builder.Services.AddSingleton<IPushSender, FakePushSender>();

builder.Services.AddSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var host = builder.Build();
host.Run();
