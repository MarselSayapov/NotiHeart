using Microsoft.Extensions.Hosting;
using NotiHeart.Channels.Email;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<EmailChannelWorker>();

var host = builder.Build();
host.Run();
