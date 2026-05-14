namespace DotnetAgents.Core.Models;

public record Session
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string AgentId { get; init; }
    public string? Title { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyList<SessionMessage> Messages { get; init; } = [];
}
