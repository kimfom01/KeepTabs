namespace KeepTabs.Entities;

public class JobTracking
{
    public string? Id { get; set; }
    public required string JobTitle { get; set; }
    public required string JobUrl { get; set; }
    public int RequestInterval { get; set; }
    public required ResponseStatus ResponseStatus { get; set; }
}