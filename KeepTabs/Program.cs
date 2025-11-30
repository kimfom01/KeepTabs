using System.Text.Json.Serialization;
using KeepTabs.Database;
using KeepTabs.EndPoints;
using KeepTabs.Entities;
using KeepTabs.Extensions;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ApplicationDbContext>("keeptabsdb");

builder.Host.ConfigureSerilog();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.ConfigureIdentity();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger();
builder.Services.ConfigureHangfire();
builder.Services.ConfigureApiVersioning();
builder.Services.ConfigureForwardedHeadersOptions();
builder.Services.AddHttpClient();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrations();
}

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
app.MapIdentityApi<User>()
    .WithTags("Auth");

await app.RunAsync();