namespace KeepTabs.Application.Monitoring;

/// <summary>
/// Parsed TCP host/port endpoint shared by validation and probes.
/// </summary>
public sealed record TcpEndpoint(string Host, int Port)
{
    public static bool TryParse(string? value, out TcpEndpoint? endpoint)
    {
        endpoint = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[1], out var port)
            || port is < 1 or > 65_535
            || Uri.CheckHostName(parts[0]) == UriHostNameType.Unknown)
        {
            return false;
        }

        endpoint = new TcpEndpoint(parts[0], port);

        return true;
    }
}
