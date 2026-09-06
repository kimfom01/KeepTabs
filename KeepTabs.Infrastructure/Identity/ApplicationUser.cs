using Microsoft.AspNetCore.Identity;

namespace KeepTabs.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public const int MaxNameLength = 100;
    public const int MaxApiKeyHashLength = 64;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ApiKeyHash { get; set; }
}