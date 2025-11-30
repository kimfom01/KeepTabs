using Microsoft.AspNetCore.Identity;

namespace KeepTabs.Entities;

public class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ApiKey { get; set; }
}