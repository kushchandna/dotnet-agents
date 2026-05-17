using DotnetAgents.Core.Models;

namespace DotnetAgents.Api.Dto;

public record ModelSettingsDto(string Id, ModelProvider Provider, string ModelName, string? Endpoint, string? ApiKeyEnvVar);
