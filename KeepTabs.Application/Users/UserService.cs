namespace KeepTabs.Application.Users;

/// <summary>
/// Reads the current authenticated user profile.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IUserAccountStore _userAccounts;

    public UserService(IUserAccountStore userAccounts)
    {
        _userAccounts = userAccounts;
    }

    public async Task<Dtos.GetUserResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userAccounts.FindByIdAsync(userId, cancellationToken);

        return user?.ToResponse();
    }
}
