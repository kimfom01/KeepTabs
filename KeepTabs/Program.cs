using KeepTabs.Application;
using KeepTabs.EndPoints;
using KeepTabs.Extensions;
using KeepTabs.Infrastructure;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKeepTabsPersistence();
builder.Host.ConfigureSerilog();
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(CorsPolicies.Frontend);
app.UseAuthentication();
app.UseAuthorization();
app.SetupHangfireDashboard();
app.SetupSwaggerDocs();

await app.ApplyMigrationsAsync();

var apiGroup = app.MapGroup("api");

apiGroup.MapGet("/", () => TypedResults.Ok("Hello world"))
    .ExcludeFromDescription();

apiGroup.MapUserEndpoints();
apiGroup.MapMonitorEndpoints();

// Unknown /api/* routes stay JSON 404s instead of falling through to the SPA.
apiGroup.Map("{*path}", () => TypedResults.NotFound())
    .ExcludeFromDescription();

app.MapDefaultEndpoints();

// Serves the Vite SPA from wwwroot; client-side routes fall back to index.html.
app.MapFallbackToFile("index.html");

await app.RunAsync();
