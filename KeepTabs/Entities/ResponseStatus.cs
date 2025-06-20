namespace KeepTabs.Entities;

public class ResponseStatus : BaseEntity
{
    public Guid Id { get; set; }
    public int? StatusCode { get; set; }
    public double ResponseLatency { get; set; }
    public string? StatusMessage { get; set; }
    public RunningState RunningState { get; set; } = RunningState.Up;
    public string RunningStateName { get; set; } = nameof(RunningState);

    public string MonitorId { get; set; }
    public Monitor? Monitor { get; set; }
}