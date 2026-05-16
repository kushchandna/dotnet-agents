using System.Collections.Concurrent;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace DotnetAgents.Runtime.Mcp;

public sealed class McpConnectionManager(
    IConfigurationService configurationService,
    IMcpClientFactory factory) : IMcpConnectionManager, IHostedService
{
    private readonly ConcurrentDictionary<string, ServerRuntime> _runtimes = new();

    // ── IHostedService ────────────────────────────────────────────────────────

    public Task StartAsync(CancellationToken ct)
    {
        var config = configurationService.Config;
        foreach (var mcpCfg in config.McpServers)
        {
            var runtime = new ServerRuntime(mcpCfg);
            _runtimes[mcpCfg.Id] = runtime;
            StartLoop(runtime);
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        var runtimes = _runtimes.Values.ToArray();
        foreach (var runtime in runtimes)
            runtime.Cts.Cancel();

        var loops = runtimes.Select(r => r.Loop ?? Task.CompletedTask).ToArray();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await Task.WhenAll(loops).WaitAsync(cts.Token); }
        catch (OperationCanceledException) { /* timeout — continue with dispose */ }
        catch (Exception) { /* loop exceptions are swallowed on stop */ }

        foreach (var runtime in runtimes)
        {
            lock (runtime.Lock)
            {
                var client = runtime.Client;
                runtime.Client = null;
                if (client is not null)
                    _ = client.DisposeAsync().AsTask();
            }
        }
    }

    // ── IMcpConnectionManager ─────────────────────────────────────────────────

    public IReadOnlyList<McpServerStatus> GetStatus()
    {
        return _runtimes.Values
            .OrderBy(r => r.Config.Id)
            .Select(r =>
            {
                lock (r.Lock)
                {
                    return new McpServerStatus(
                        r.Config.Id,
                        r.State,
                        r.Error,
                        r.LastConnectedAt,
                        r.RetryAttempt,
                        r.Tools.Select(t => t.Name).ToList());
                }
            })
            .ToArray();
    }

    public async Task RetryAsync(string serverId, CancellationToken ct = default)
    {
        if (!_runtimes.TryGetValue(serverId, out var runtime))
            return;

        if (runtime.State == McpServerState.Disabled)
            return;

        await CancelAndWaitAsync(runtime);

        IMcpClientHandle? oldClient;
        lock (runtime.Lock)
        {
            oldClient = runtime.Client;
            runtime.Client = null;
            runtime.RetryAttempt = 0;
            runtime.Cts = new CancellationTokenSource();
        }

        if (oldClient is not null)
            try { await oldClient.DisposeAsync(); } catch { }

        StartLoop(runtime);
    }

    public async Task ReconfigureAsync(AgentsConfig config, CancellationToken ct)
    {
        var newConfigs = config.McpServers.ToDictionary(c => c.Id);
        var existingIds = _runtimes.Keys.ToHashSet();

        var tasks = new List<Task>();

        // Add new servers
        foreach (var (id, cfg) in newConfigs)
        {
            if (!existingIds.Contains(id))
            {
                var runtime = new ServerRuntime(cfg);
                _runtimes[id] = runtime;
                StartLoop(runtime);
            }
        }

        // Update changed or remove deleted servers
        foreach (var id in existingIds)
        {
            if (!newConfigs.TryGetValue(id, out var newCfg))
            {
                // Server removed
                if (_runtimes.TryRemove(id, out var removed))
                    tasks.Add(CancelDisposeAsync(removed));
            }
            else if (_runtimes.TryGetValue(id, out var existing) && existing.Config != newCfg)
            {
                // Config changed — replace runtime
                tasks.Add(ReplaceRuntimeAsync(id, newCfg, existing));
            }
            // unchanged — leave alone
        }

        await Task.WhenAll(tasks);
    }

    public IReadOnlyList<AIFunction> ResolveToolsForAgent(AgentConfig agent)
    {
        IEnumerable<string> visible = agent.McpServersInheritance switch
        {
            McpServersInheritance.All    => _runtimes.Keys,
            McpServersInheritance.None   => [],
            McpServersInheritance.Custom => agent.McpServers,
            _                            => []
        };

        return visible
            .Select(id => _runtimes.TryGetValue(id, out var r) ? r : null)
            .Where(r => r is { State: McpServerState.Connected })
            .SelectMany(r => r!.Tools)
            .ToList();
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private void StartLoop(ServerRuntime runtime)
    {
        runtime.Loop = Task.Run(() => ConnectLoopAsync(runtime, runtime.Cts.Token));
    }

    private async Task ConnectLoopAsync(ServerRuntime runtime, CancellationToken ct)
    {
        if (!runtime.Config.Enabled)
        {
            SetState(runtime, McpServerState.Disabled, error: null, tools: []);
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            SetState(runtime, McpServerState.Connecting, error: runtime.Error, tools: []);

            lock (runtime.Lock) runtime.RetryAttempt = 0;

            IMcpClientHandle? client = null;
            bool connected = false;

            // ── Retry loop ────────────────────────────────────────────────────
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    client = await factory.CreateAsync(runtime.Config, ct);
                    var tools = await client.ListToolsAsync(ct);

                    lock (runtime.Lock)
                    {
                        runtime.Client = client;
                        runtime.State = McpServerState.Connected;
                        runtime.Error = null;
                        runtime.Tools = tools;
                        runtime.LastConnectedAt = DateTimeOffset.UtcNow;
                    }

                    connected = true;
                    break; // exit retry loop → fall through to ping loop
                }
                catch (OperationCanceledException)
                {
                    if (client is not null) await SafeDisposeAsync(client);
                    throw;
                }
                catch (Exception ex)
                {
                    if (client is not null) { await SafeDisposeAsync(client); client = null; }

                    int attempt;
                    lock (runtime.Lock)
                    {
                        runtime.RetryAttempt++;
                        runtime.Error = ex.Message;
                        attempt = runtime.RetryAttempt;
                    }

                    if (attempt > runtime.Config.RetryLimit)
                    {
                        SetState(runtime, McpServerState.Failed, error: ex.Message, tools: []);
                        return;
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(runtime.Config.RetryInterval), ct);
                    }
                    catch (OperationCanceledException) { throw; }
                }
            }

            if (!connected)
                return; // was cancelled during retry loop

            // ── Ping loop: detect disconnects ─────────────────────────────────
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ct);

                    IMcpClientHandle? pingClient;
                    lock (runtime.Lock) pingClient = runtime.Client;
                    if (pingClient is not null)
                        await pingClient.PingAsync(ct);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                // Connection dropped — dispose and loop back to retry
                IMcpClientHandle? dropped;
                lock (runtime.Lock)
                {
                    dropped = runtime.Client;
                    runtime.Client = null;
                    runtime.Error = $"Connection lost: {ex.Message}";
                }
                if (dropped is not null) await SafeDisposeAsync(dropped);
                // outer while will reconnect with fresh retries
            }
        }
    }

    private async Task CancelAndWaitAsync(ServerRuntime runtime)
    {
        CancellationTokenSource oldCts;
        Task? loop;
        lock (runtime.Lock)
        {
            oldCts = runtime.Cts;
            loop = runtime.Loop;
            runtime.Cts = new CancellationTokenSource();
        }
        oldCts.Cancel();
        if (loop is not null)
        {
            try { await loop.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }
            catch (Exception) { }
        }
        oldCts.Dispose();
    }

    private async Task CancelDisposeAsync(ServerRuntime runtime)
    {
        await CancelAndWaitAsync(runtime);
        IMcpClientHandle? client;
        lock (runtime.Lock) { client = runtime.Client; runtime.Client = null; }
        if (client is not null) await SafeDisposeAsync(client);
        runtime.Cts.Dispose();
    }

    private async Task ReplaceRuntimeAsync(string id, McpServerConfig newCfg, ServerRuntime old)
    {
        await CancelDisposeAsync(old);
        var fresh = new ServerRuntime(newCfg);
        _runtimes[id] = fresh;
        StartLoop(fresh);
    }

    private static void SetState(
        ServerRuntime runtime,
        McpServerState state,
        string? error,
        IReadOnlyList<AIFunction> tools)
    {
        lock (runtime.Lock)
        {
            runtime.State = state;
            runtime.Error = error;
            runtime.Tools = tools;
        }
    }

    private static async Task SafeDisposeAsync(IMcpClientHandle client)
    {
        try { await client.DisposeAsync(); } catch { }
    }

    // ── Nested mutable state ──────────────────────────────────────────────────

    internal sealed class ServerRuntime(McpServerConfig config)
    {
        public readonly Lock Lock = new();

        public McpServerConfig Config { get; } = config;
        public McpServerState State { get; set; } = McpServerState.Connecting;
        public string? Error { get; set; }
        public DateTimeOffset? LastConnectedAt { get; set; }
        public int RetryAttempt { get; set; }
        public IReadOnlyList<AIFunction> Tools { get; set; } = [];
        public IMcpClientHandle? Client { get; set; }
        public CancellationTokenSource Cts { get; set; } = new();
        public Task? Loop { get; set; }
    }
}
