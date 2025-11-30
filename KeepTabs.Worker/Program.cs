using KeepTabs.Worker;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.AddServiceDefaults();

var host = builder.Build();
host.Run();