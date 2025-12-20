using Microsoft.EntityFrameworkCore;
using Serilog;
using SmsWorker;
using SmsWorker.Data;
using SmsWorker.Messaging;
using SmsWorker.Sms;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<NotificationWorker>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<SmsSenderOptions>(builder.Configuration.GetSection("SmsSender"));

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<NotificationPublisher>();
builder.Services.AddSingleton<ISmsSender, FakeSmsSender>();

builder.Services.AddSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var host = builder.Build();
host.Run();
