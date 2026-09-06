using KeepTabs.Application.Users.Dtos;

namespace KeepTabs.Application.Users;

/// <summary>
/// Reads the current authenticated user profile.
/// </summary>
public interface IUserService
{
    Task<GetUserResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
}
