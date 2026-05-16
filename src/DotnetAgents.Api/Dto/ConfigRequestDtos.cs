using DotnetAgents.Core.Models;

namespace DotnetAgents.Api.Dto;

public record UpsertAgentRequest(
    string Id,
    string Name,
    string? Description,
    string ModelId,
    string? SystemPrompt,
    List<string>? Tools,
    McpServersInheritance McpServersInheritance,
    List<string>? McpServers);

public record UpsertUserRequest(string Id, string DisplayName);

public record UpsertMcpServerRequest(
    string Id,
    string? Command,
    List<string>? Args,
    Dictionary<string, string>? Env,
    string? Url);
