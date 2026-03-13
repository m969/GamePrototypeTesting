namespace PrototypeTesting.Models;

public sealed record PrototypeDefinition(
    string Id,
    string Title,
    string Category,
    string Description,
    string ControlsHint,
    string TestGoal);
