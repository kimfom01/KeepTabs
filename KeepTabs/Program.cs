using KeepTabs.EndPoints;
using KeepTabs.Extensions;
using KeepTabs.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureSerilog();

builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger();
builder.Services.ConfigureHangfire();
builder.Services.ConfigureApiVersioning();
builder.Services.ConfigureForwardedHeadersOptions();
builder.Services.AddTransient<MonitorService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<MongoDbProvider>();

var app = builder.Build();

app.UseHttpsRedirection();
app.SetupHangfireDashboard();
app.SetupScalarDocs();

app.MapGet("/", () => Results.Ok("Hello world"))
    .WithSummary("Greetings")
    .WithDescription("""Returns a "Hello world" message""")
    .WithTags("KeepTabs");

app.MapMonitorEndpoints();

await app.RunAsync();