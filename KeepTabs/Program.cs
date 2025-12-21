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
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.SetupHangfireDashboard();
app.SetupSwaggerDocs();

var apiGroup = app.MapGroup("api");

apiGroup.MapGet("/", () => Results.Ok("Hello world"))
    .ExcludeFromDescription();

apiGroup.MapUserEndpoints();
apiGroup.MapMonitorEndpoints();
app.MapDefaultEndpoints();

await app.RunAsync();