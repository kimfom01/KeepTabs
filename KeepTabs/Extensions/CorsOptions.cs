using System.ComponentModel.DataAnnotations;

namespace KeepTabs.Extensions;

/// <summary>
/// Configurable browser origins permitted to call the API.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public static readonly string[] DefaultAllowedOrigins = ["http://localhost:5173"];

    [MinLength(1)]
    public string[] AllowedOrigins { get; set; } = DefaultAllowedOrigins;

    /// <summary>
    /// Parses a comma-separated origins value from appsettings or an environment
    /// variable into the origin array required by the CORS policy.
    /// </summary>
    public static string[] ParseAllowedOrigins(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultAllowedOrigins;
        }

        var origins = NormalizeOrigins(raw.Split(','));

        return origins.Length > 0 ? origins : DefaultAllowedOrigins;
    }

    /// <summary>
    /// Normalizes configured origins: trims whitespace, drops empties, removes
    /// trailing slashes (origins must not contain a path), and drops duplicates.
    /// </summary>
    public static string[] NormalizeOrigins(IEnumerable<string?>? origins)
    {
        if (origins is null)
        {
            return [];
        }

        return origins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin!.Trim().TrimEnd('/'))
            .Where(origin => origin.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
