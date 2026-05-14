namespace DotnetAgents.Core.Models;

public record UserConfig
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
}
