using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok("Hello world"))
    .WithSummary("Greetings")
    .WithDescription("""Returns a "Hello world" message""");

await app.RunAsync();