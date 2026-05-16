using Microsoft.Extensions.AI;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Runtime.Mcp;

public interface IMcpClientFactory
{
    Task<IMcpClientHandle> CreateAsync(McpServerConfig cfg, CancellationToken ct);
}

public interface IMcpClientHandle : IAsyncDisposable
{
    Task<IReadOnlyList<AIFunction>> ListToolsAsync(CancellationToken ct);
    Task PingAsync(CancellationToken ct);
}
