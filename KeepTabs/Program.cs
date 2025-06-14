using System.Text.Json.Serialization;
using Hangfire;
using KeepTabs.Database;
using KeepTabs.EndPoints;
using KeepTabs.Extensions;
using KeepTabs.Jobs;
using KeepTabs.Services;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<KeepTabsDbContext>("keeptabsdb");

builder.Host.ConfigureSerilog();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger();
builder.Services.ConfigureHangfire();
builder.Services.ConfigureApiVersioning();
builder.Services.ConfigureForwardedHeadersOptions();
builder.Services.AddTransient<MonitorService>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.ApplyMigrations();

app.UseHttpsRedirection();
app.SetupHangfireDashboard();
app.SetupScalarDocs();
app.SetupSwaggerDocs();

app.MapGet("/", () => Results.Ok("Hello world"))
    .WithSummary("Greetings")
    .WithDescription("""Returns a "Hello world" message""")
    .WithTags("KeepTabs");

app.MapMonitorEndpoints();

app.MapDefaultEndpoints();

var manager = app.Services.CreateAsyncScope()
    .ServiceProvider.GetRequiredService<IRecurringJobManager>();
manager.AddOrUpdate<JobHistoryCleaner>("CleanUp", x => x.CleanUp(CancellationToken.None), Cron.Daily);

await app.RunAsync();