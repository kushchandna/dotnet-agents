using DotnetAgents.Core.Models;

namespace DotnetAgents.Api.Dto;

public record AgentSettingsDto(
    string Id,
    string Name,
    string? Description,
    string ModelId,
    string? SystemPrompt,
    IReadOnlyList<string> Tools,
    SkillsInheritance SkillsInheritance,
    IReadOnlyList<string> Skills,
    McpServersInheritance McpServersInheritance,
    IReadOnlyList<string> McpServers);
