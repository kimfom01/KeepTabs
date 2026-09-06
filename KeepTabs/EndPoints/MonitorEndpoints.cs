using KeepTabs.Application.Monitors;
using KeepTabs.Application.Monitors.Dtos;
using KeepTabs.Domain.Common;
using KeepTabs.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KeepTabs.EndPoints;

public static class MonitorEndpoints
{
    public static void MapMonitorEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("monitors")
            .WithTags("Monitors")
            .RequireAuthorization(Extensions.AuthorizationPolicies.UserAccess);

        group.MapPost("/", CreateMonitor)
            .AddEndpointFilter<ValidationFilter<CreateMonitorRequest>>()
            .WithName("CreateMonitor")
            .WithSummary("Create a monitor")
            .WithDescription("Creates a monitor owned by the authenticated caller.")
            .Produces<GetMonitorResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", GetMonitors)
            .WithName("GetMonitors")
            .WithSummary("List monitors")
            .WithDescription("Lists monitors owned by the authenticated caller.")
            .Produces<IReadOnlyList<GetMonitorResponse>>();

        group.MapGet("/{monitorId:guid}", GetMonitorById)
            .WithName("GetMonitorById")
            .WithSummary("Get a monitor")
            .WithDescription("Returns a monitor owned by the authenticated caller.")
            .Produces<GetMonitorResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{monitorId:guid}", UpdateMonitor)
            .AddEndpointFilter<ValidationFilter<UpdateMonitorRequest>>()
            .WithName("UpdateMonitor")
            .WithSummary("Update a monitor")
            .WithDescription("Applies a partial update to a monitor owned by the authenticated caller.")
            .Produces<GetMonitorResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{monitorId:guid}", DeleteMonitor)
            .WithName("DeleteMonitor")
            .WithSummary("Delete a monitor")
            .WithDescription("Deletes a monitor owned by the authenticated caller.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{monitorId:guid}/pause", PauseMonitor)
            .WithName("PauseMonitor")
            .WithSummary("Pause a monitor")
            .WithDescription("Pauses checks for a monitor owned by the authenticated caller.")
            .Produces<GetMonitorResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{monitorId:guid}/resume", ResumeMonitor)
            .WithName("ResumeMonitor")
            .WithSummary("Resume a monitor")
            .WithDescription("Resumes checks for a monitor owned by the authenticated caller.")
            .Produces<GetMonitorResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{monitorId:guid}/summary", GetSummary)
            .WithName("GetMonitorSummary")
            .WithSummary("Get monitor availability summary")
            .WithDescription("Returns aggregate availability and response-time statistics.")
            .Produces<MonitorSummaryResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{monitorId:guid}/history", GetHistory)
            .WithName("GetMonitorHistory")
            .WithSummary("Get monitor check history")
            .WithDescription("Returns recent persisted check results. Defaults to 30 days.")
            .Produces<IReadOnlyList<MonitorCheckHistoryItem>>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<CreatedAtRoute<GetMonitorResponse>, ValidationProblem>> CreateMonitor(
        CreateMonitorRequest request,
        IUser currentUser,
        IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var response = await monitorService.CreateMonitorAsync(RequireUserId(currentUser), request, cancellationToken);

        return TypedResults.CreatedAtRoute(
            response,
            "GetMonitorById",
            new { monitorId = response.MonitorId });
    }

    private static async Task<Ok<IReadOnlyList<GetMonitorResponse>>> GetMonitors(
        IUser currentUser,
        IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var monitors = await monitorService.GetMonitorsAsync(RequireUserId(currentUser), cancellationToken);

        return TypedResults.Ok(monitors);
    }

    private static async Task<Results<Ok<GetMonitorResponse>, ProblemHttpResult>> GetMonitorById(
        Guid monitorId,
        IUser currentUser,
        IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var monitor = await monitorService.GetMonitorByIdAsync(RequireUserId(currentUser), monitorId, cancellationToken);

        return monitor is null
            ? MonitorNotFound()
            : TypedResults.Ok(monitor);
    }

    private static async Task<Results<Ok<GetMonitorResponse>, ProblemHttpResult, ValidationProblem>> UpdateMonitor(
        Guid monitorId,
        UpdateMonitorRequest request,
        IUser currentUser,
        IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var monitor = await monitorService.UpdateMonitorAsync(RequireUserId(currentUser), monitorId, request, cancellationToken);

        return monitor is null
            ? MonitorNotFound()
            : TypedResults.Ok(monitor);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteMonitor(
        Guid monitorId,
        IUser currentUser,
        IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var deleted = await monitorService.DeleteMonitorAsync(RequireUserId(currentUser), monitorId, cancellationToken);

        return deleted
            ? TypedResults.NoContent()
            : MonitorNotFound();
    }

    private static async Task<Results<Ok<GetMonitorResponse>, ProblemHttpResult>> PauseMonitor(
        Guid monitorId,
        IUser currentUser,
        IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var monitor = await monitorService.SetPausedAsync(RequireUserId(currentUser), monitorId, true, cancellationToken);

        return monitor is null
            ? MonitorNotFound()
            : TypedResults.Ok(monitor);
    }

    private static async Task<Results<Ok<GetMonitorResponse>, ProblemHttpResult>> ResumeMonitor(
        Guid monitorId,
        IUser currentUser,
        IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var monitor = await monitorService.SetPausedAsync(RequireUserId(currentUser), monitorId, false, cancellationToken);

        return monitor is null
            ? MonitorNotFound()
            : TypedResults.Ok(monitor);
    }

    private static async Task<Results<Ok<MonitorSummaryResponse>, ProblemHttpResult>> GetSummary(
        Guid monitorId,
        IUser currentUser,
        IMonitorService monitorService,
        CancellationToken cancellationToken)
    {
        var summary = await monitorService.GetSummaryAsync(RequireUserId(currentUser), monitorId, cancellationToken);

        return summary is null
            ? MonitorNotFound()
            : TypedResults.Ok(summary);
    }

    private static async Task<Results<Ok<IReadOnlyList<MonitorCheckHistoryItem>>, ProblemHttpResult>> GetHistory(
        Guid monitorId,
        IUser currentUser,
        IMonitorService monitorService,
        int? days,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(currentUser);
        var monitor = await monitorService.GetMonitorByIdAsync(userId, monitorId, cancellationToken);
        if (monitor is null)
        {
            return MonitorNotFound();
        }

        var history = await monitorService.GetHistoryAsync(userId, monitorId, days ?? 30, cancellationToken);

        return TypedResults.Ok(history);
    }

    private static string RequireUserId(IUser currentUser)
    {
        return currentUser.Id ?? throw new UnauthorizedAccessException();
    }

    private static ProblemHttpResult MonitorNotFound()
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not found.",
            detail: "The requested monitor was not found.");
    }
}
