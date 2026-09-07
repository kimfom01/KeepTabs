using KeepTabs.Application.StatusPages;
using KeepTabs.Application.StatusPages.Dtos;
using KeepTabs.Domain.Common;
using KeepTabs.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KeepTabs.EndPoints;

public static class StatusPageEndpoints
{
    public static void MapStatusPageEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("status-pages")
            .WithTags("StatusPages")
            .RequireAuthorization(Extensions.AuthorizationPolicies.UserAccess);

        group.MapPost("/", CreateStatusPage)
            .AddEndpointFilter<ValidationFilter<CreateStatusPageRequest>>()
            .WithName("CreateStatusPage")
            .WithSummary("Create a status page")
            .Produces<GetStatusPageResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", ListStatusPages)
            .WithName("ListStatusPages")
            .WithSummary("List status pages")
            .Produces<IReadOnlyList<GetStatusPageResponse>>();

        group.MapGet("/check-slug", CheckSlug)
            .WithName("CheckStatusPageSlug")
            .WithSummary("Check slug availability")
            .WithDescription("Normalizes a candidate slug and reports whether it is free, with a suggestion when taken.")
            .Produces<SlugAvailabilityResponse>();

        group.MapGet("/{statusPageId:guid}", GetStatusPageById)
            .WithName("GetStatusPageById")
            .WithSummary("Get a status page")
            .Produces<GetStatusPageResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{statusPageId:guid}", UpdateStatusPage)
            .AddEndpointFilter<ValidationFilter<UpdateStatusPageRequest>>()
            .WithName("UpdateStatusPage")
            .WithSummary("Update a status page")
            .Produces<GetStatusPageResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{statusPageId:guid}", DeleteStatusPage)
            .WithName("DeleteStatusPage")
            .WithSummary("Delete a status page")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // Public, unauthenticated read of a published page.
        app.MapGet("/status/{slug}", GetPublicStatusPage)
            .AllowAnonymous()
            .WithTags("StatusPages")
            .WithName("GetPublicStatusPage")
            .WithSummary("Get a published status page")
            .Produces<PublicStatusPageResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<CreatedAtRoute<GetStatusPageResponse>, ValidationProblem>> CreateStatusPage(
        CreateStatusPageRequest request,
        IUser currentUser,
        IStatusPageService statusPageService,
        CancellationToken cancellationToken)
    {
        var response = await statusPageService.CreateAsync(RequireUserId(currentUser), request, cancellationToken);

        return TypedResults.CreatedAtRoute(
            response,
            "GetStatusPageById",
            new { statusPageId = response.StatusPageId });
    }

    private static async Task<Ok<IReadOnlyList<GetStatusPageResponse>>> ListStatusPages(
        IUser currentUser,
        IStatusPageService statusPageService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await statusPageService.ListAsync(RequireUserId(currentUser), cancellationToken));
    }

    private static async Task<Results<Ok<GetStatusPageResponse>, ProblemHttpResult>> GetStatusPageById(
        Guid statusPageId,
        IUser currentUser,
        IStatusPageService statusPageService,
        CancellationToken cancellationToken)
    {
        var page = await statusPageService.GetByIdAsync(RequireUserId(currentUser), statusPageId, cancellationToken);

        return page is null ? StatusPageNotFound() : TypedResults.Ok(page);
    }

    private static async Task<Results<Ok<GetStatusPageResponse>, ProblemHttpResult, ValidationProblem>> UpdateStatusPage(
        Guid statusPageId,
        UpdateStatusPageRequest request,
        IUser currentUser,
        IStatusPageService statusPageService,
        CancellationToken cancellationToken)
    {
        var page = await statusPageService.UpdateAsync(RequireUserId(currentUser), statusPageId, request, cancellationToken);

        return page is null ? StatusPageNotFound() : TypedResults.Ok(page);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteStatusPage(
        Guid statusPageId,
        IUser currentUser,
        IStatusPageService statusPageService,
        CancellationToken cancellationToken)
    {
        var deleted = await statusPageService.DeleteAsync(RequireUserId(currentUser), statusPageId, cancellationToken);

        return deleted ? TypedResults.NoContent() : StatusPageNotFound();
    }

    private static async Task<Results<Ok<PublicStatusPageResponse>, ProblemHttpResult>> GetPublicStatusPage(
        string slug,
        IStatusPageService statusPageService,
        CancellationToken cancellationToken)
    {
        var page = await statusPageService.GetPublicAsync(slug, cancellationToken);

        return page is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not found.",
                detail: "The requested status page was not found.")
            : TypedResults.Ok(page);
    }

    private static async Task<Ok<SlugAvailabilityResponse>> CheckSlug(
        IUser currentUser,
        IStatusPageService statusPageService,
        string? slug,
        string? name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        _ = RequireUserId(currentUser);

        return TypedResults.Ok(await statusPageService.CheckSlugAsync(slug, name, excludeId, cancellationToken));
    }

    private static string RequireUserId(IUser currentUser)
    {
        return currentUser.Id ?? throw new UnauthorizedAccessException();
    }

    private static ProblemHttpResult StatusPageNotFound()
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not found.",
            detail: "The requested status page was not found.");
    }
}
