namespace PrototypeTesting.Models;

public sealed class PrototypeSessionResult
{
    public string SessionId { get; set; } = string.Empty;

    public string PrototypeId { get; set; } = string.Empty;

    public string PrototypeTitle { get; set; } = string.Empty;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset FinishedAt { get; set; }

    public double DurationSeconds { get; set; }

    public string Status { get; set; } = string.Empty;

    public GameSnapshot FinalSnapshot { get; set; } = GameSnapshot.Empty;

    public TestSurveyResponse? Survey { get; set; }

    public List<TelemetryEvent> Events { get; set; } = [];
}
