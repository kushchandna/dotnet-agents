namespace DotnetAgents.Core.Models;

public record McpServerConfig
{
    public required string Id { get; init; }
    public string? Command { get; init; }
    public IReadOnlyList<string> Args { get; init; } = [];
    public IReadOnlyDictionary<string, string> Env { get; init; } = new Dictionary<string, string>();
    public string? Url { get; init; }
    public bool Enabled { get; init; } = true;
    public int RetryLimit { get; init; } = 3;
    public int RetryInterval { get; init; } = 5;
}
