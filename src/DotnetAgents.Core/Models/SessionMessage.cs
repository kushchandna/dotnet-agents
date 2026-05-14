namespace DotnetAgents.Core.Models;

public record SessionMessage
{
    public required string Id { get; init; }
    public required string Role { get; init; }   // "user" | "assistant" | "system" | "tool"
    public string? Content { get; init; }
    public IReadOnlyList<ToolCallRecord>? ToolCalls { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
