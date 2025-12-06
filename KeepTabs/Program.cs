using KeepTabs.Application;
using KeepTabs.EndPoints;
using KeepTabs.Extensions;
using KeepTabs.Infrastructure;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.ConfigureAspireDbContext();
builder.Host.ConfigureSerilog();
builder.Services.AddWebServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrations();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.SetupHangfireDashboard();
app.SetupSwaggerDocs();

app.MapGet("/", () => Results.Ok("Hello world"))
    .WithSummary("Greetings")
    .WithDescription("""Returns a "Hello world" message""")
    .WithTags("KeepTabs");

app.MapUserEndpoints();
app.MapMonitorEndpoints();
app.MapDefaultEndpoints();

await app.RunAsync();