using System.ComponentModel.DataAnnotations;

namespace KeepTabs.Infrastructure.Security;

/// <summary>
/// Strongly typed JWT configuration. Values must come from user secrets or the host environment.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(5, 1_440)]
    public int ExpiryMinutes { get; set; } = 60;
}
