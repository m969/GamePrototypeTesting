using System.Text.Json;
using Microsoft.JSInterop;
using PrototypeTesting.Models;

namespace PrototypeTesting.Core;

public sealed class TelemetryService(IJSRuntime jsRuntime)
{
    private const string StorageKey = "prototype-testing-results";
    private PrototypeSessionDraft? _currentSession;

    public Task StartSessionAsync(PrototypeDefinition prototype)
    {
        _currentSession = new PrototypeSessionDraft
        {
            SessionId = Guid.NewGuid().ToString("N"),
            PrototypeId = prototype.Id,
            PrototypeTitle = prototype.Title,
            StartedAt = DateTimeOffset.UtcNow
        };

        return Task.CompletedTask;
    }

    public Task TrackEventAsync(string eventType, Dictionary<string, object?> payload)
    {
        if (_currentSession is null)
        {
            return Task.CompletedTask;
        }

        _currentSession.Events.Add(new TelemetryEvent
        {
            EventType = eventType,
            Timestamp = DateTimeOffset.UtcNow,
            Payload = NormalizePayload(payload)
        });

        return Task.CompletedTask;
    }

    public Task AttachSurveyAsync(TestSurveyResponse survey)
    {
        if (_currentSession is null)
        {
            return Task.CompletedTask;
        }

        _currentSession.Survey = survey;
        return Task.CompletedTask;
    }

    public async Task<PrototypeSessionResult> CompleteSessionAsync(string status, GameSnapshot snapshot)
    {
        if (_currentSession is null)
        {
            throw new InvalidOperationException("No active prototype session.");
        }

        var finishedAt = DateTimeOffset.UtcNow;
        var result = new PrototypeSessionResult
        {
            SessionId = _currentSession.SessionId,
            PrototypeId = _currentSession.PrototypeId,
            PrototypeTitle = _currentSession.PrototypeTitle,
            StartedAt = _currentSession.StartedAt,
            FinishedAt = finishedAt,
            DurationSeconds = (finishedAt - _currentSession.StartedAt).TotalSeconds,
            Status = status,
            FinalSnapshot = snapshot,
            Survey = _currentSession.Survey,
            Events = [.. _currentSession.Events]
        };

        var existing = (await GetResultsAsync()).ToList();
        existing.Insert(0, result);

        var serialized = JsonSerializer.Serialize(existing);
        await jsRuntime.InvokeVoidAsync("prototypeTestingStorage.save", StorageKey, serialized);

        _currentSession = null;
        return result;
    }

    public async Task<IReadOnlyList<PrototypeSessionResult>> GetResultsAsync()
    {
        var raw = await jsRuntime.InvokeAsync<string?>("prototypeTestingStorage.load", StorageKey);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<PrototypeSessionResult>>(raw) ?? [];
    }

    private static Dictionary<string, string> NormalizePayload(Dictionary<string, object?> payload)
    {
        return payload.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed class PrototypeSessionDraft
    {
        public required string SessionId { get; init; }

        public required string PrototypeId { get; init; }

        public required string PrototypeTitle { get; init; }

        public required DateTimeOffset StartedAt { get; init; }

        public List<TelemetryEvent> Events { get; } = [];

        public TestSurveyResponse? Survey { get; set; }
    }
}
