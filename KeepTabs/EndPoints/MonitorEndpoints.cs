using FluentValidation;
using KeepTabs.Application.Monitors;
using KeepTabs.Application.Monitors.Dtos;

namespace KeepTabs.EndPoints;

public static class MonitorEndpoints
{
    public static void MapMonitorEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("monitors")
            .WithTags("Monitors");

        group.MapPost("/", CreateMonitor);
        group.MapGet("/{monitorId:guid}", GetMonitorById);
        group.MapGet("/", GetMonitors);
    }

    private static async Task<IResult> CreateMonitor(CreateMonitorRequest request,
        IValidator<CreateMonitorRequest> validator, IMonitorService monitorService, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }
        
        var monitor = await monitorService.CreateMonitor(request, cancellationToken);

        return Results.Created($"/api/monitors/${monitor.MonitorId}", monitor);
    }

    private static async Task<IResult> GetMonitorById(Guid monitorId, IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var monitor = await monitorService.GetMonitorById(monitorId, cancellationToken);

        if (monitor is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(monitor);
    }

    private static IResult GetMonitors(string userId, IMonitorService monitorService)
    {
        var monitors = monitorService.GetMonitors(userId);

        return Results.Ok(monitors);
    }
}