using Asp.Versioning;
using Hangfire;
using KeepTabs.Extensions;
using KeepTabs.Jobs;
using KeepTabs.Requests;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger();
builder.Services.ConfigureHangfire();
builder.Services.ConfigureApiVersioning();
builder.Services.ConfigureForwardedHeadersOptions();

var app = builder.Build();

app.UseHttpsRedirection();
app.SetupHangfireDashboard();
app.SetupScalarDocs();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

app.MapGet("v{v:apiVersion}/", () => Results.Ok("Hello world"))
    .WithApiVersionSet(apiVersionSet)
    .WithSummary("Greetings")
    .WithDescription("""Returns a "Hello world" message""");

app.MapPost("v{v:apiVersion}/monitor",
        (MonitorRequest request) =>
        {
            var jobId = $"{request.Title}_{Guid.NewGuid()}";

            var cronExpression = $"*/{request.Interval} * * * *";

            RecurringJob.AddOrUpdate<MonitorJob>(jobId, x => x.PrintRequest(request), cronExpression);

            return Results.Ok(jobId);
        })
    .WithApiVersionSet(apiVersionSet)
    .WithSummary("Monitor")
    .WithDescription("Endpoint to initiate a monitoring request. Returns the job ID");


await app.RunAsync();