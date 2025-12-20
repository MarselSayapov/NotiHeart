using Microsoft.Extensions.Hosting;
using NotiHeart.Orchestrator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<NotificationOrchestratorWorker>();

var host = builder.Build();
host.Run();
