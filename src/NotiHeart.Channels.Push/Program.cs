using Microsoft.Extensions.Hosting;
using NotiHeart.Channels.Push;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<PushChannelWorker>();

var host = builder.Build();
host.Run();
