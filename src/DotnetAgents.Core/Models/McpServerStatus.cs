namespace DotnetAgents.Core.Models;

public enum McpServerState
{
    Disabled,
    Connecting,
    Connected,
    Failed
}

public record McpServerStatus(
    string Id,
    McpServerState State,
    string? Error,
    DateTimeOffset? LastConnectedAt,
    int RetryAttempt,
    IReadOnlyList<string> ToolNames);
