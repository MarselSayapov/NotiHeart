using EmailWorker;
using EmailWorker.Data;
using EmailWorker.Email;
using EmailWorker.Messaging;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<NotificationWorker>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<EmailSenderOptions>(builder.Configuration.GetSection("EmailSender"));

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<NotificationPublisher>();
builder.Services.AddSingleton<IEmailSender, FakeEmailSender>();

builder.Services.AddSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var host = builder.Build();
host.Run();
