namespace KeepTabs.Extensions;

/// <summary>
/// Authorization policies used by KeepTabs endpoints.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Authenticated users may use either a JWT bearer token or an API key.
    /// </summary>
    public const string UserAccess = "UserAccess";
}
