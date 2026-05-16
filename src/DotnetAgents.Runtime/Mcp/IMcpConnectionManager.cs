using Microsoft.Extensions.AI;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Runtime.Mcp;

public interface IMcpConnectionManager
{
    IReadOnlyList<McpServerStatus> GetStatus();
    Task RetryAsync(string serverId, CancellationToken ct = default);
    Task ReconfigureAsync(AgentsConfig config, CancellationToken ct);
    IReadOnlyList<AIFunction> ResolveToolsForAgent(AgentConfig agent);
}
