namespace KeepTabs.Requests;

/// <summary>
/// This represents the payload used to initiate a monitoring request
/// </summary>
public class MonitorRequest
{
    /// <summary>Full url endpoint e.g. https://google.com/health</summary>
    /// <example>https://google.com/health</example>
    public required string Url { get; init; }

    /// <summary>Title of the resource to monitor</summary>
    /// <example>Google</example>
    public required string Title { get; init; }

    /// <summary>Interval in minutes (0-59)</summary>
    /// <example>5</example>
    public int Interval { get; init; }
}