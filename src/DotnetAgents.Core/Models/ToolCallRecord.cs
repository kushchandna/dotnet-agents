namespace DotnetAgents.Core.Models;

public record ToolCallRecord
{
    public required string CallId { get; init; }
    public required string Name { get; init; }
    public required string Arguments { get; init; }
    public required string Result { get; init; }
}
