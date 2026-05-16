using System.Collections.Concurrent;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using DotnetAgents.Runtime.Mcp;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Runtime.Tests.Mcp;

[TestFixture]
public class McpConnectionManagerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AgentsConfig MakeConfig(params McpServerConfig[] servers) =>
        new()
        {
            Agents = [],
            Models = [],
            Users = [],
            Sessions = new SessionsConfig { Directory = "/tmp" },
            McpServers = servers
        };

    private static McpServerConfig MakeServer(
        string id,
        bool enabled = true,
        int retryLimit = 3,
        int retryInterval = 0) =>
        new()
        {
            Id = id,
            Command = "fake",
            Enabled = enabled,
            RetryLimit = retryLimit,
            RetryInterval = retryInterval
        };

    private static AIFunction MakeFakeAIFunction(string name) =>
        AIFunctionFactory.Create(() => name, name);

    private static async Task<bool> WaitForStateAsync(
        IMcpConnectionManager mgr,
        string id,
        McpServerState expected,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var status = mgr.GetStatus().FirstOrDefault(s => s.Id == id);
            if (status?.State == expected) return true;
            await Task.Delay(20);
        }
        return false;
    }

    private static McpConnectionManager MakeManager(
        IMcpClientFactory factory,
        params McpServerConfig[] servers)
    {
        var config = MakeConfig(servers);
        var cfgSvc = new ConfigurationService(config);
        return new McpConnectionManager(cfgSvc, factory);
    }

    // ── Fake factory helpers ──────────────────────────────────────────────────

    /// <summary>Factory that always succeeds and returns the given tools.</summary>
    private sealed class SucceedingFactory(IReadOnlyList<AIFunction> tools) : IMcpClientFactory
    {
        public Task<IMcpClientHandle> CreateAsync(McpServerConfig cfg, CancellationToken ct)
            => Task.FromResult<IMcpClientHandle>(new FakeHandle(tools));
    }

    /// <summary>Factory that fails N times, then succeeds.</summary>
    private sealed class FailThenSucceedFactory(int failCount, IReadOnlyList<AIFunction> tools) : IMcpClientFactory
    {
        private int _attempts;

        public Task<IMcpClientHandle> CreateAsync(McpServerConfig cfg, CancellationToken ct)
        {
            if (Interlocked.Increment(ref _attempts) <= failCount)
                throw new InvalidOperationException($"Simulated failure #{_attempts}");
            return Task.FromResult<IMcpClientHandle>(new FakeHandle(tools));
        }
    }

    /// <summary>Factory that always throws.</summary>
    private sealed class AlwaysFailFactory : IMcpClientFactory
    {
        public Task<IMcpClientHandle> CreateAsync(McpServerConfig cfg, CancellationToken ct)
            => throw new InvalidOperationException("Simulated failure");
    }

    private sealed class FakeHandle(IReadOnlyList<AIFunction> tools) : IMcpClientHandle
    {
        public Task<IReadOnlyList<AIFunction>> ListToolsAsync(CancellationToken ct)
            => Task.FromResult(tools);

        public Task PingAsync(CancellationToken ct) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task DisabledServer_StaysDisabled()
    {
        var callCount = 0;
        var factory = new CountingFactory(() =>
        {
            Interlocked.Increment(ref callCount);
            return new FakeHandle([]);
        });

        var mgr = MakeManager(factory, MakeServer("s1", enabled: false));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(100); // give loop a chance to run
            var status = mgr.GetStatus().Single();
            Assert.That(status.State, Is.EqualTo(McpServerState.Disabled));
            Assert.That(callCount, Is.EqualTo(0));
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task EnabledServer_FirstTrySuccess_BecomesConnected()
    {
        var tools = new AIFunction[]
        {
            MakeFakeAIFunction("tool_a"),
            MakeFakeAIFunction("tool_b")
        };
        var factory = new SucceedingFactory(tools);
        var mgr = MakeManager(factory, MakeServer("s1"));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            var reached = await WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(3));
            Assert.That(reached, Is.True, "Server did not reach Connected state");
            var status = mgr.GetStatus().Single();
            Assert.That(status.ToolNames, Has.Count.EqualTo(2));
            Assert.That(status.State, Is.EqualTo(McpServerState.Connected));
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task Failure_Then_Success_Within_RetryLimit_BecomesConnected()
    {
        var tools = new AIFunction[] { MakeFakeAIFunction("tool_ok") };
        var factory = new FailThenSucceedFactory(failCount: 2, tools);
        var mgr = MakeManager(factory, MakeServer("s1", retryLimit: 3, retryInterval: 0));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            var reached = await WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(5));
            Assert.That(reached, Is.True, "Server did not reach Connected state after retries");
            var status = mgr.GetStatus().Single();
            Assert.That(status.State, Is.EqualTo(McpServerState.Connected));
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task Failure_ExceedsRetryLimit_BecomesFailed()
    {
        var factory = new AlwaysFailFactory();
        var mgr = MakeManager(factory, MakeServer("s1", retryLimit: 1, retryInterval: 0));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            var reached = await WaitForStateAsync(mgr, "s1", McpServerState.Failed, TimeSpan.FromSeconds(5));
            Assert.That(reached, Is.True, "Server did not reach Failed state");
            var status = mgr.GetStatus().Single();
            Assert.That(status.Error, Is.Not.Null.And.Not.Empty);
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task ResolveToolsForAgent_AllInheritance_ReturnsAllConnectedTools()
    {
        var tools = new AIFunction[]
        {
            MakeFakeAIFunction("t1"),
            MakeFakeAIFunction("t2")
        };
        var factory = new SucceedingFactory(tools);
        var mgr = MakeManager(factory, MakeServer("s1"), MakeServer("s2"));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            await Task.WhenAll(
                WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(3)),
                WaitForStateAsync(mgr, "s2", McpServerState.Connected, TimeSpan.FromSeconds(3)));

            var agent = new AgentConfig
            {
                Id = "a1", Name = "A1", ModelId = "m",
                McpServersInheritance = McpServersInheritance.All
            };
            var resolved = mgr.ResolveToolsForAgent(agent);
            // 2 servers × 2 tools each = 4
            Assert.That(resolved, Has.Count.EqualTo(4));
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task ResolveToolsForAgent_NoneInheritance_ReturnsEmpty()
    {
        var factory = new SucceedingFactory([MakeFakeAIFunction("t1")]);
        var mgr = MakeManager(factory, MakeServer("s1"));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            await WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(3));
            var agent = new AgentConfig
            {
                Id = "a1", Name = "A1", ModelId = "m",
                McpServersInheritance = McpServersInheritance.None
            };
            var resolved = mgr.ResolveToolsForAgent(agent);
            Assert.That(resolved, Is.Empty);
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task ResolveToolsForAgent_CustomInheritance_ReturnsOnlyListedServers()
    {
        var factory = new SucceedingFactory([MakeFakeAIFunction("t1")]);
        var mgr = MakeManager(factory, MakeServer("s1"), MakeServer("s2"), MakeServer("s3"));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            foreach (var id in new[] { "s1", "s2", "s3" })
                await WaitForStateAsync(mgr, id, McpServerState.Connected, TimeSpan.FromSeconds(3));

            var agent = new AgentConfig
            {
                Id = "a1", Name = "A1", ModelId = "m",
                McpServersInheritance = McpServersInheritance.Custom,
                McpServers = ["s1", "s3"]
            };
            var resolved = mgr.ResolveToolsForAgent(agent);
            // only s1 and s3: 2 tools total
            Assert.That(resolved, Has.Count.EqualTo(2));
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task ResolveToolsForAgent_SkipsNonConnectedServers()
    {
        var factory = new AlwaysFailFactory();
        var mgr = MakeManager(factory, MakeServer("s1", retryLimit: 0, retryInterval: 0));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            await WaitForStateAsync(mgr, "s1", McpServerState.Failed, TimeSpan.FromSeconds(5));

            var agent = new AgentConfig
            {
                Id = "a1", Name = "A1", ModelId = "m",
                McpServersInheritance = McpServersInheritance.All
            };
            var resolved = mgr.ResolveToolsForAgent(agent);
            Assert.That(resolved, Is.Empty);
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task RetryAsync_OnFailedServer_TriggersNewConnectionAttempt()
    {
        // Fail first, then succeed once retry is triggered
        var callCount = 0;
        var factory = new CountingFactory(() =>
        {
            var n = Interlocked.Increment(ref callCount);
            if (n <= 2) // fail on initial attempts to hit Failed state
                throw new InvalidOperationException("initial failure");
            return new FakeHandle([MakeFakeAIFunction("t1")]);
        });

        // RetryLimit=1 so it reaches Failed quickly
        var mgr = MakeManager(factory, MakeServer("s1", retryLimit: 1, retryInterval: 0));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            var failed = await WaitForStateAsync(mgr, "s1", McpServerState.Failed, TimeSpan.FromSeconds(5));
            Assert.That(failed, Is.True, "Server did not reach Failed state");

            // Now retry — the factory will succeed on the next call
            await mgr.RetryAsync("s1");

            var connected = await WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(5));
            Assert.That(connected, Is.True, "Server did not reconnect after retry");
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task ReconfigureAsync_AddsNewServer_StartsLoop()
    {
        var factory = new SucceedingFactory([MakeFakeAIFunction("t1")]);
        var mgr = MakeManager(factory); // start with no servers
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            Assert.That(mgr.GetStatus(), Is.Empty);

            var newConfig = MakeConfig(MakeServer("s1"));
            await mgr.ReconfigureAsync(newConfig, CancellationToken.None);

            var reached = await WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(3));
            Assert.That(reached, Is.True, "New server did not connect after reconfigure");
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task ReconfigureAsync_RemovesDeletedServer_DisposesClient()
    {
        var disposeCount = 0;
        var factory = new CountingFactory(() => new TrackingHandle(
            [MakeFakeAIFunction("t1")],
            onDispose: () => Interlocked.Increment(ref disposeCount)));

        var mgr = MakeManager(factory, MakeServer("s1"));
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            await WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(3));

            // Reconfigure with no servers → s1 should be removed
            await mgr.ReconfigureAsync(MakeConfig(), CancellationToken.None);
            await Task.Delay(200); // give async dispose time

            Assert.That(mgr.GetStatus(), Is.Empty);
            Assert.That(disposeCount, Is.GreaterThanOrEqualTo(1));
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    [Test]
    public async Task ReconfigureAsync_DetectsEditedServer_ReplacesRuntime()
    {
        var factory = new SucceedingFactory([MakeFakeAIFunction("t1")]);
        var original = MakeServer("s1");
        var mgr = MakeManager(factory, original);
        await mgr.StartAsync(CancellationToken.None);
        try
        {
            await WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(3));

            // Change RetryLimit to force record inequality
            var edited = original with { RetryLimit = 99 };
            await mgr.ReconfigureAsync(MakeConfig(edited), CancellationToken.None);

            // Should reconnect (back through Connecting → Connected)
            var reached = await WaitForStateAsync(mgr, "s1", McpServerState.Connected, TimeSpan.FromSeconds(5));
            Assert.That(reached, Is.True, "Server did not reconnect after config edit");
        }
        finally { await mgr.StopAsync(CancellationToken.None); }
    }

    // ── Additional fake factory helpers ──────────────────────────────────────

    private sealed class CountingFactory(Func<IMcpClientHandle> handleFactory) : IMcpClientFactory
    {
        public Task<IMcpClientHandle> CreateAsync(McpServerConfig cfg, CancellationToken ct)
            => Task.FromResult(handleFactory());
    }

    private sealed class TrackingHandle(IReadOnlyList<AIFunction> tools, Action onDispose) : IMcpClientHandle
    {
        public Task<IReadOnlyList<AIFunction>> ListToolsAsync(CancellationToken ct)
            => Task.FromResult(tools);

        public Task PingAsync(CancellationToken ct) => Task.CompletedTask;

        public ValueTask DisposeAsync()
        {
            onDispose();
            return ValueTask.CompletedTask;
        }
    }
}
