using DotnetAgents.Core.Models;

namespace DotnetAgents.Api.Dto;

public record AgentSettingsDto(
    string Id,
    string Name,
    string? Description,
    string ModelId,
    string? SystemPrompt,
    IReadOnlyList<string> Tools,
    McpServersInheritance McpServersInheritance,
    IReadOnlyList<string> McpServers);
