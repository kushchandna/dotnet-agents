namespace DotnetAgents.Api.Dto;

public record AgentDto(string Id, string Name, string? Description, string ModelId, IReadOnlyList<string> Tools);
