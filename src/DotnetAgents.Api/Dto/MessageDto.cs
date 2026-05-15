using DotnetAgents.Core.Models;
namespace DotnetAgents.Api.Dto;
public record MessageDto(string Id, string Role, string? Content,
                          IReadOnlyList<ToolCallRecord>? ToolCalls, DateTimeOffset Timestamp);
