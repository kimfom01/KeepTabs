using KeepTabs.Application.Users;
using KeepTabs.Application.Users.Dtos;
using KeepTabs.Domain.Common;
using KeepTabs.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KeepTabs.EndPoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("auth")
            .WithTags("Auth")
            .RequireAuthorization(Extensions.AuthorizationPolicies.UserAccess);

        group.MapPost("/register", Register)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .WithName("RegisterUser")
            .WithSummary("Register a user")
            .WithDescription("Creates a user account and returns a JWT for the new account.")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .WithName("LoginUser")
            .WithSummary("Log in a user")
            .WithDescription("Validates credentials and returns a JWT for the account.")
            .Produces<AuthResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/api-key/regenerate", RegenerateApiKey)
            .WithName("RegenerateApiKey")
            .WithSummary("Regenerate the current user's API key")
            .WithDescription("Returns a one-time raw API key. Only its hash is retained.")
            .Produces<ApiKeyResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get the current user")
            .WithDescription("Returns the profile for the authenticated caller.")
            .Produces<GetUserResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Created<AuthResponse>, ValidationProblem>> Register(
        RegisterRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var (response, errors) = await authService.RegisterAsync(request, cancellationToken);
        if (response is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = errors.Count > 0 ? errors.ToArray() : ["Registration failed."]
            });
        }

        return TypedResults.Created("/api/auth/me", response);
    }

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> Login(
        LoginRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);

        return response is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized.",
                detail: "Invalid email or password.")
            : TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<ApiKeyResponse>, ProblemHttpResult>> RegenerateApiKey(
        IUser currentUser,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(currentUser);
        var response = await authService.RegenerateApiKeyAsync(userId, cancellationToken);

        return response is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not found.",
                detail: "The current user was not found.")
            : TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<GetUserResponse>, ProblemHttpResult>> GetCurrentUser(
        IUser currentUser,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(currentUser);
        var user = await userService.GetUserByIdAsync(userId, cancellationToken);

        return user is null
            ? TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not found.",
                detail: "The current user was not found.")
            : TypedResults.Ok(user);
    }

    private static string RequireUserId(IUser currentUser)
    {
        return currentUser.Id ?? throw new UnauthorizedAccessException();
    }
}
