using Microsoft.Extensions.Hosting;
using NotiHeart.Channels.Sms;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<SmsChannelWorker>();

var host = builder.Build();
host.Run();
