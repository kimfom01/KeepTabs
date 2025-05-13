namespace KeepTabs.Entities;

public class ResponseStatus
{
    public Guid Id { get; set; }
    public int? StatusCode { get; set; }
    public double ResponseLatency { get; set; }
    public string? StatusMessage { get; set; }
    public RunningState RunningState { get; set; } = RunningState.Up;
    public string RunningStateName { get; set; } = nameof(RunningState);
}