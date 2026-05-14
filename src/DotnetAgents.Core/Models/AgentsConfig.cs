namespace DotnetAgents.Core.Models;

public record AgentsConfig
{
    public required IReadOnlyList<AgentConfig> Agents { get; init; }
    public required IReadOnlyList<ModelConfig> Models { get; init; }
    public required IReadOnlyList<UserConfig>  Users  { get; init; }
    public required SessionsConfig             Sessions { get; init; }
    public ToolsConfig?                        Tools { get; init; }
}
