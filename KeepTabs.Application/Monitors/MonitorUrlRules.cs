using System;

namespace KeepTabs.Application.Monitors;

/// <summary>
/// Shared URL-shape checks for monitor protocols.
/// </summary>
internal static class MonitorUrlRules
{
    public static bool IsTcpEndpoint(string? value)
    {
        return Monitoring.TcpEndpoint.TryParse(value, out _);
    }

    public static bool IsPingTarget(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !value.Contains("://", StringComparison.Ordinal)
            && !value.Contains(':')
            && Uri.CheckHostName(value) != UriHostNameType.Unknown;
    }
}
