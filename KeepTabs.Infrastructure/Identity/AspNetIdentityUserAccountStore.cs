using KeepTabs.Application.Users;
using KeepTabs.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KeepTabs.Infrastructure.Identity;

/// <summary>
/// Identity-backed implementation of the application user-account boundary.
/// </summary>
public sealed class AspNetIdentityUserAccountStore : IUserAccountStore
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public AspNetIdentityUserAccountStore(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return null;
        }

        return await ToAccountAsync(user);
    }

    public async Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        return await ToAccountAsync(user);
    }

    public async Task<UserAccount?> FindByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.ApiKeyHash == apiKeyHash, cancellationToken);

        return user is null ? null : await ToAccountAsync(user);
    }

    public Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<AccountCreationResult> CreateAsync(
        string email,
        string password,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim()
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return new AccountCreationResult(false, null, result.Errors.Select(error => error.Description).ToList());
        }

        return new AccountCreationResult(true, user.Id, []);
    }

    public async Task<bool> UpdateApiKeyHashAsync(string userId, string? apiKeyHash, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        user.ApiKeyHash = apiKeyHash;
        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    private async Task<UserAccount> ToAccountAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserAccount(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles.ToList());
    }
}
