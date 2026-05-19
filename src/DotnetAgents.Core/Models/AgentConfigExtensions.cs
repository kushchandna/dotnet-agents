namespace DotnetAgents.Core.Models;

public static class AgentConfigExtensions
{
    /// <summary>Returns the resolved MCP server IDs for an agent given the full server list.</summary>
    public static IEnumerable<string> ResolveMcpServerIds(
        this AgentConfig agent, IEnumerable<string> allServerIds) =>
        agent.McpServersInheritance switch
        {
            McpServersInheritance.All    => allServerIds,
            McpServersInheritance.None   => [],
            McpServersInheritance.Custom => agent.McpServers,
            _                            => []
        };

    /// <summary>Returns the resolved skill IDs for an agent given the full skill list.</summary>
    public static IEnumerable<string> ResolveSkillIds(
        this AgentConfig agent, IEnumerable<string> allSkillIds) =>
        agent.SkillsInheritance switch
        {
            SkillsInheritance.All    => allSkillIds,
            SkillsInheritance.None   => [],
            SkillsInheritance.Custom => agent.Skills,
            _                        => []
        };
}
