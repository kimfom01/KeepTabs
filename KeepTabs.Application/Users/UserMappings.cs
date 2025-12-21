using KeepTabs.Application.Users.Dtos;
using KeepTabs.Infrastructure.Identity;

namespace KeepTabs.Application.Users;

public static class UserMappings
{
    public static GetUserResponse ToResponse(this ApplicationUser user)
    {
        return new GetUserResponse
        {
            UserId = user.Id,
            ApiKey = user.ApiKey,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}