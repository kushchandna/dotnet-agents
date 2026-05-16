namespace DotnetAgents.Api.Dto;

public record McpServerDto(
    string Id,
    string? Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env,
    string? Url);
