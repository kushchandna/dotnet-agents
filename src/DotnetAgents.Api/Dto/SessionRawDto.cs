using DotnetAgents.Core.Models;

namespace DotnetAgents.Api.Dto;

public record SessionRawDto(
    string? SystemPrompt,
    AgentRawDto Agent,
    ModelRawDto Model,
    IReadOnlyList<MessageDto> Messages);

public record AgentRawDto(
    string Id,
    string Name,
    string? Description,
    string ModelId,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> McpServers);

public record ModelRawDto(
    string Id,
    ModelProvider Provider,
    string ModelName,
    string? Endpoint);
