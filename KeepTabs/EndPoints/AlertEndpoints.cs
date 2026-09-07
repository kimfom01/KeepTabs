using KeepTabs.Application.Alerts;
using KeepTabs.Application.Alerts.Dtos;
using KeepTabs.Domain.Common;
using KeepTabs.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KeepTabs.EndPoints;

public static class AlertEndpoints
{
    public static void MapAlertEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("alerts")
            .WithTags("Alerts")
            .RequireAuthorization(AuthorizationPolicies.UserAccess);

        group.MapPost("/", CreateAlertRule)
            .AddEndpointFilter<ValidationFilter<CreateAlertRuleRequest>>()
            .WithName("CreateAlertRule")
            .WithSummary("Create an alert rule")
            .WithDescription("Creates an alert rule on a monitor owned by the authenticated caller.")
            .Produces<GetAlertRuleResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", ListAlertRules)
            .WithName("ListAlertRules")
            .WithSummary("List alert rules")
            .WithDescription("Lists alert rules on monitors owned by the authenticated caller, optionally filtered by monitor.")
            .Produces<IReadOnlyList<GetAlertRuleResponse>>();

        group.MapGet("/logs", GetAlertLogs)
            .WithName("GetAlertLogs")
            .WithSummary("List alert deliveries")
            .WithDescription("Returns recent alert delivery attempts for the authenticated caller, optionally filtered by monitor.")
            .Produces<IReadOnlyList<AlertLogResponse>>();

        group.MapGet("/{alertRuleId:guid}", GetAlertRuleById)
            .WithName("GetAlertRuleById")
            .WithSummary("Get an alert rule")
            .Produces<GetAlertRuleResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{alertRuleId:guid}", UpdateAlertRule)
            .AddEndpointFilter<ValidationFilter<UpdateAlertRuleRequest>>()
            .WithName("UpdateAlertRule")
            .WithSummary("Update an alert rule")
            .Produces<GetAlertRuleResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{alertRuleId:guid}", DeleteAlertRule)
            .WithName("DeleteAlertRule")
            .WithSummary("Delete an alert rule")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{alertRuleId:guid}/test", TestAlertRule)
            .WithName("TestAlertRule")
            .WithSummary("Send a test alert")
            .WithDescription("Sends a test delivery through the rule's channel and records the attempt.")
            .Produces<TestAlertResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<CreatedAtRoute<GetAlertRuleResponse>, ValidationProblem>> CreateAlertRule(
        CreateAlertRuleRequest request,
        IUser currentUser,
        IAlertService alertService,
        CancellationToken cancellationToken)
    {
        var response = await alertService.CreateAsync(RequireUserId(currentUser), request, cancellationToken);

        return TypedResults.CreatedAtRoute(
            response,
            "GetAlertRuleById",
            new { alertRuleId = response.AlertRuleId });
    }

    private static async Task<Ok<IReadOnlyList<GetAlertRuleResponse>>> ListAlertRules(
        IUser currentUser,
        IAlertService alertService,
        Guid? monitorId,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await alertService.ListAsync(RequireUserId(currentUser), monitorId, cancellationToken));
    }

    private static async Task<Ok<IReadOnlyList<AlertLogResponse>>> GetAlertLogs(
        IUser currentUser,
        IAlertService alertService,
        Guid? monitorId,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await alertService.GetLogsAsync(RequireUserId(currentUser), monitorId, cancellationToken));
    }

    private static async Task<Results<Ok<GetAlertRuleResponse>, ProblemHttpResult>> GetAlertRuleById(
        Guid alertRuleId,
        IUser currentUser,
        IAlertService alertService,
        CancellationToken cancellationToken)
    {
        var rule = await alertService.GetByIdAsync(RequireUserId(currentUser), alertRuleId, cancellationToken);

        return rule is null ? AlertRuleNotFound() : TypedResults.Ok(rule);
    }

    private static async Task<Results<Ok<GetAlertRuleResponse>, ProblemHttpResult, ValidationProblem>> UpdateAlertRule(
        Guid alertRuleId,
        UpdateAlertRuleRequest request,
        IUser currentUser,
        IAlertService alertService,
        CancellationToken cancellationToken)
    {
        var rule = await alertService.UpdateAsync(RequireUserId(currentUser), alertRuleId, request, cancellationToken);

        return rule is null ? AlertRuleNotFound() : TypedResults.Ok(rule);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAlertRule(
        Guid alertRuleId,
        IUser currentUser,
        IAlertService alertService,
        CancellationToken cancellationToken)
    {
        var deleted = await alertService.DeleteAsync(RequireUserId(currentUser), alertRuleId, cancellationToken);

        return deleted ? TypedResults.NoContent() : AlertRuleNotFound();
    }

    private static async Task<Results<Ok<TestAlertResponse>, ProblemHttpResult>> TestAlertRule(
        Guid alertRuleId,
        IUser currentUser,
        IAlertService alertService,
        CancellationToken cancellationToken)
    {
        var response = await alertService.TestAsync(RequireUserId(currentUser), alertRuleId, cancellationToken);

        return response is null ? AlertRuleNotFound() : TypedResults.Ok(response);
    }

    private static string RequireUserId(IUser currentUser)
    {
        return currentUser.Id ?? throw new UnauthorizedAccessException();
    }

    private static ProblemHttpResult AlertRuleNotFound()
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not found.",
            detail: "The requested alert rule was not found.");
    }
}
