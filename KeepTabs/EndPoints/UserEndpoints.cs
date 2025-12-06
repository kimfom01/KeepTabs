using KeepTabs.Infrastructure.Identity;

namespace KeepTabs.EndPoints;

public static class Users
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapIdentityApi<ApplicationUser>()
            .WithTags("Auth");
    }
}