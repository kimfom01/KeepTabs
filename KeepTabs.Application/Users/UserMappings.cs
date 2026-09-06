namespace KeepTabs.Application.Users;

/// <summary>
/// Maps application-level user accounts to API responses.
/// </summary>
public static class UserMappings
{
    public static Dtos.GetUserResponse ToResponse(this UserAccount user)
    {
        return new Dtos.GetUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName);
    }
}
