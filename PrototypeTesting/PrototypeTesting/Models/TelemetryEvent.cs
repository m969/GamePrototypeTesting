namespace PrototypeTesting.Models;

public sealed class TelemetryEvent
{
    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public Dictionary<string, string> Payload { get; set; } = [];
}
