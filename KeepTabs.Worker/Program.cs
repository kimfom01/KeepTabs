using KeepTabs.Application;
using KeepTabs.Infrastructure;
using KeepTabs.Worker.Jobs;
using Quartz;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddKeepTabsPersistence();
builder.Services.AddMonitorChecking();
builder.Services.AddMonitorCheckingInfrastructure();
builder.Services.AddQuartz(q =>
{
    q.AddJob<MonitorScanJob>(options => options.WithIdentity("monitor-scan-job"));
    q.AddJob<MonitorCheckJob>(options => options
        .WithIdentity(MonitorCheckJob.JobName)
        .StoreDurably());
    q.AddTrigger(options => options
        .ForJob(new JobKey("monitor-scan-job"))
        .WithIdentity("monitor-scan-trigger")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(30).RepeatForever()));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var host = builder.Build();
host.Run();
