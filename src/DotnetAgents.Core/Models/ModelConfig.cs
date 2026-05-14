using System.Text.Json.Serialization;

namespace DotnetAgents.Core.Models;

public enum ModelProvider
{
    OpenAI,
    [JsonStringEnumMemberName("openai-compatible")]
    OpenAICompatible,
    Ollama
}

public record ModelConfig
{
    public required string Id { get; init; }
    public required ModelProvider Provider { get; init; }
    public required string ModelName { get; init; }
    public string? Endpoint { get; init; }
    public string? ApiKeyEnvVar { get; init; }
}
