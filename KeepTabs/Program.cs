using KeepTabs.Database;
using KeepTabs.EndPoints;
using KeepTabs.Extensions;
using KeepTabs.Services;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<KeepTabsDbContext>("keeptabsdb");

builder.Host.ConfigureSerilog();

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

app.MapGet("/", () => Results.Ok("Hello world"))
    .WithSummary("Greetings")
    .WithDescription("""Returns a "Hello world" message""")
    .WithTags("KeepTabs");

app.MapTrackingEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();