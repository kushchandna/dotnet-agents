namespace DotnetAgents.Api.Dto;
public record SessionDto(string Id, string UserId, string AgentId, string? Title,
                          DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
