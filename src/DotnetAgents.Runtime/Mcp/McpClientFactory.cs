using DotnetAgents.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace DotnetAgents.Runtime.Mcp;

public sealed class McpClientFactory(ILoggerFactory loggerFactory) : IMcpClientFactory
{
    public async Task<IMcpClientHandle> CreateAsync(McpServerConfig cfg, CancellationToken ct)
    {
        // Fix 9: dispose transport on failure (HttpClientTransport is IAsyncDisposable;
        // StdioClientTransport implements neither IDisposable nor IAsyncDisposable in SDK 1.2.0).
        IClientTransport transport = cfg.Command is not null
            ? new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = cfg.Id,
                Command = cfg.Command,
                Arguments = cfg.Args.ToList(),
                EnvironmentVariables = cfg.Env.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value)
            }, loggerFactory)
            : new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = cfg.Id,
                Endpoint = new Uri(cfg.Url!),
                TransportMode = HttpTransportMode.AutoDetect
            }, loggerFactory);

        try
        {
            var client = await McpClient.CreateAsync(transport, cancellationToken: ct);
            return new McpClientHandle(client);
        }
        catch
        {
            if (transport is IAsyncDisposable adisp) try { await adisp.DisposeAsync(); } catch { }
            else if (transport is IDisposable disp) try { disp.Dispose(); } catch { }
            throw;
        }
    }

    private sealed class McpClientHandle(McpClient client) : IMcpClientHandle
    {
        public async Task<IReadOnlyList<AIFunction>> ListToolsAsync(CancellationToken ct)
        {
            var tools = await client.ListToolsAsync(cancellationToken: ct);
            return tools.Cast<AIFunction>().ToList();
        }

        // Real ping — uses the MCP ping primitive defined in the spec.
        public async Task PingAsync(CancellationToken ct) => await client.PingAsync(cancellationToken: ct);

        public ValueTask DisposeAsync() => client.DisposeAsync();
    }
}
