using KeepTabs.Application.Users;
using KeepTabs.Infrastructure.Identity;

namespace KeepTabs.EndPoints;

public static class Users
{
    public static void MapUserEndpoints(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("auth")
            .WithTags("Auth");

        group.MapIdentityApi<ApplicationUser>();
        group.MapGet("/", GetUsers);
        group.MapGet("/{userId}", GetUserById);
    }

    private static IResult GetUsers(IUserService userService)
    {
        var users = userService.GetUsers();

        return Results.Ok(users);
    }

    private static IResult GetUserById(string userId, IUserService userService)
    {
        var users = userService.GetUsers();

        return Results.Ok(users);
    }
}