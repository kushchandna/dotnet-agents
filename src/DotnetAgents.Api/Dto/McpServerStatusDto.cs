using DotnetAgents.Core.Models;

namespace DotnetAgents.Api.Dto;

public record McpServerStatusDto(
    string Id,
    McpServerState State,
    string? Error,
    DateTimeOffset? LastConnectedAt,
    int RetryAttempt,
    IReadOnlyList<string> ToolNames);
