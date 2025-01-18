using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KeepTabs.Entities;

public class JobTracking
{
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.String)]
    public string? Id { get; set; }

    [BsonElement("job_title")] public required string JobTitle { get; set; }

    [BsonElement("job_url")] public required string JobUrl { get; set; }

    [BsonElement("request_interval")] public int RequestInterval { get; set; }

    [BsonElement("response_status")] public required ResponseStatus ResponseStatus { get; set; }
}

public class ResponseStatus
{
    [BsonId]
    [BsonElement("_id"), BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("status_code")] public int StatusCode { get; set; }

    [BsonElement("response_latency")] public int ResponseLatency { get; set; }

    [BsonElement("status_message")] public string? StatusMessage { get; set; }
    [BsonElement("running_state")] public RunningState RunningState { get; set; } = RunningState.StartedState;
    [BsonElement("running_state_name")] public string RunningStateName { get; set; } = nameof(RunningState);
}

public enum RunningState
{
    StoppedState = 10,
    StartedState = 50,
}