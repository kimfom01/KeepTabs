namespace KeepTabs.Application.Users;

/// <summary>
/// Generates signed authentication tokens for application-level user accounts.
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(UserAccount user);
}
