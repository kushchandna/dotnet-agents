using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Models;
using DotnetAgents.Runtime.Mcp;
using NSubstitute;

namespace DotnetAgents.Api.Tests;

[TestFixture]
public class ConfigEndpointTests
{
    // ── Agents ───────────────────────────────────────────────────────────────

    [Test]
    public async Task GetConfigAgents_ReturnsConfiguredAgents()
    {
        await using var f = ApiTestFactory.Create();
        var agents = await f.CreateClient().GetFromJsonAsync<AgentSettingsDto[]>("/api/config/agents", ApiTestFactory.TestJsonOptions);
        Assert.That(agents, Is.Not.Null);
        Assert.That(agents!.Single().Id, Is.EqualTo("assistant"));
    }

    [Test]
    public async Task GetConfigUsers_ReturnsConfiguredUsers()
    {
        await using var f = ApiTestFactory.Create();
        var users = await f.CreateClient().GetFromJsonAsync<UserDto[]>("/api/config/users");
        Assert.That(users, Is.Not.Null);
        Assert.That(users!.Select(u => u.Id), Is.EquivalentTo(new[] { "alice", "bob" }));
    }

    [Test]
    public async Task GetConfigMcpServers_ReturnsEmptyList()
    {
        await using var f = ApiTestFactory.Create();
        var servers = await f.CreateClient().GetFromJsonAsync<McpServerDto[]>("/api/config/mcp-servers");
        Assert.That(servers, Is.Not.Null);
        Assert.That(servers, Is.Empty);
    }

    [Test]
    public async Task PostConfigAgent_CreatesAgent_AppearsInGet()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var req = new UpsertAgentRequest("coder", "Coder", null, "m", null, null, McpServersInheritance.All, null);
        var post = await client.PostAsJsonAsync("/api/config/agents", req);
        Assert.That((int)post.StatusCode, Is.EqualTo(201));

        var agents = await client.GetFromJsonAsync<AgentSettingsDto[]>("/api/config/agents", ApiTestFactory.TestJsonOptions);
        Assert.That(agents!.Any(a => a.Id == "coder"), Is.True);
    }

    [Test]
    public async Task PostConfigAgent_DuplicateId_Returns409()
    {
        await using var f = ApiTestFactory.Create();
        var req = new UpsertAgentRequest("assistant", "Dup", null, "m", null, null, McpServersInheritance.All, null);
        var resp = await f.CreateClient().PostAsJsonAsync("/api/config/agents", req);
        Assert.That((int)resp.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task PostConfigAgent_UnknownModelId_Returns400()
    {
        await using var f = ApiTestFactory.Create();
        var req = new UpsertAgentRequest("new-agent", "New", null, "unknown-model", null, null, McpServersInheritance.All, null);
        var resp = await f.CreateClient().PostAsJsonAsync("/api/config/agents", req);
        Assert.That((int)resp.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task PutConfigAgent_UpdatesName_ReflectedInGet()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var req = new UpsertAgentRequest("assistant", "Renamed Assistant", null, "m", null, null, McpServersInheritance.All, null);
        var put = await client.PutAsJsonAsync("/api/config/agents/assistant", req);
        Assert.That((int)put.StatusCode, Is.EqualTo(200));

        var agents = await client.GetFromJsonAsync<AgentSettingsDto[]>("/api/config/agents", ApiTestFactory.TestJsonOptions);
        Assert.That(agents!.Single(a => a.Id == "assistant").Name, Is.EqualTo("Renamed Assistant"));
    }

    [Test]
    public async Task DeleteConfigAgent_RemovesIt_AbsentInGet()
    {
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents =
            [
                new AgentConfig { Id = "a1", Name = "A1", ModelId = "m" },
                new AgentConfig { Id = "a2", Name = "A2", ModelId = "m" }
            ],
            Models = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users  = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-test-" + Guid.NewGuid()) }
        });
        var client = f.CreateClient();

        var del = await client.DeleteAsync("/api/config/agents/a1");
        Assert.That((int)del.StatusCode, Is.EqualTo(204));

        var agents = await client.GetFromJsonAsync<AgentSettingsDto[]>("/api/config/agents", ApiTestFactory.TestJsonOptions);
        Assert.That(agents!.Any(a => a.Id == "a1"), Is.False);
    }

    [Test]
    public async Task DeleteConfigAgent_LastAgent_Returns400()
    {
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents   = [new AgentConfig { Id = "only", Name = "Only", ModelId = "m" }],
            Models   = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users    = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-test-" + Guid.NewGuid()) }
        });
        var resp = await f.CreateClient().DeleteAsync("/api/config/agents/only");
        Assert.That((int)resp.StatusCode, Is.EqualTo(400));
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostConfigUser_CreatesUser()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var req = new UpsertUserRequest("charlie", "Charlie");
        var post = await client.PostAsJsonAsync("/api/config/users", req);
        Assert.That((int)post.StatusCode, Is.EqualTo(201));

        var users = await client.GetFromJsonAsync<UserDto[]>("/api/config/users");
        Assert.That(users!.Any(u => u.Id == "charlie"), Is.True);
    }

    [Test]
    public async Task PutConfigUser_UpdatesDisplayName()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var req = new UpsertUserRequest("alice", "Alice Updated");
        var put = await client.PutAsJsonAsync("/api/config/users/alice", req);
        Assert.That((int)put.StatusCode, Is.EqualTo(200));

        var users = await client.GetFromJsonAsync<UserDto[]>("/api/config/users");
        Assert.That(users!.Single(u => u.Id == "alice").DisplayName, Is.EqualTo("Alice Updated"));
    }

    [Test]
    public async Task DeleteConfigUser_RemovesUser()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var del = await client.DeleteAsync("/api/config/users/bob");
        Assert.That((int)del.StatusCode, Is.EqualTo(204));

        var users = await client.GetFromJsonAsync<UserDto[]>("/api/config/users");
        Assert.That(users!.Any(u => u.Id == "bob"), Is.False);
    }

    // ── MCP Servers ───────────────────────────────────────────────────────────

    [Test]
    public async Task PostConfigMcpServer_CreatesStdioServer()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var req = new UpsertMcpServerRequest("fs", "npx", ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"], null, null, null, null, null);
        var post = await client.PostAsJsonAsync("/api/config/mcp-servers", req);
        Assert.That((int)post.StatusCode, Is.EqualTo(201));

        var servers = await client.GetFromJsonAsync<McpServerDto[]>("/api/config/mcp-servers");
        Assert.That(servers!.Single(s => s.Id == "fs").Command, Is.EqualTo("npx"));
    }

    [Test]
    public async Task PostConfigMcpServer_NeitherCommandNorUrl_Returns400()
    {
        await using var f = ApiTestFactory.Create();
        var req = new UpsertMcpServerRequest("bad", null, null, null, null, null, null, null);
        var resp = await f.CreateClient().PostAsJsonAsync("/api/config/mcp-servers", req);
        Assert.That((int)resp.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task GetConfigMcpServers_IncludesNewFields()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns([]);
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents   = [new AgentConfig { Id = "a1", Name = "A1", ModelId = "m" }],
            Models   = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users    = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-test-" + Guid.NewGuid()) },
            McpServers = [new McpServerConfig { Id = "s1", Command = "x" }]
        }, mcpManager: mcpManager);

        var servers = await f.CreateClient().GetFromJsonAsync<McpServerDto[]>("/api/config/mcp-servers");
        var s = servers!.Single(s => s.Id == "s1");
        Assert.That(s.Enabled, Is.True);
        Assert.That(s.RetryLimit, Is.EqualTo(3));
        Assert.That(s.RetryInterval, Is.EqualTo(5));
    }

    [Test]
    public async Task PostConfigMcpServer_WithRetrySettings_Persists()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns([]);
        await using var f = ApiTestFactory.Create(mcpManager: mcpManager);
        var client = f.CreateClient();

        var req = new UpsertMcpServerRequest("s1", "cmd", null, null, null, false, 10, 30);
        var post = await client.PostAsJsonAsync("/api/config/mcp-servers", req);
        Assert.That((int)post.StatusCode, Is.EqualTo(201));

        var servers = await client.GetFromJsonAsync<McpServerDto[]>("/api/config/mcp-servers");
        var s = servers!.Single(s => s.Id == "s1");
        Assert.That(s.Enabled, Is.False);
        Assert.That(s.RetryLimit, Is.EqualTo(10));
        Assert.That(s.RetryInterval, Is.EqualTo(30));
    }

    [Test]
    public async Task PostConfigMcpServer_NegativeRetryLimit_Returns400()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns([]);
        await using var f = ApiTestFactory.Create(mcpManager: mcpManager);
        var req = new UpsertMcpServerRequest("s1", "cmd", null, null, null, true, -1, null);
        var resp = await f.CreateClient().PostAsJsonAsync("/api/config/mcp-servers", req);
        Assert.That((int)resp.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task PutConfigMcpServer_UrlIdWinsOverBodyId()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns([]);
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents   = [new AgentConfig { Id = "a1", Name = "A1", ModelId = "m" }],
            Models   = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users    = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-test-" + Guid.NewGuid()) },
            McpServers = [new McpServerConfig { Id = "s1", Command = "x" }]
        }, mcpManager: mcpManager);
        var client = f.CreateClient();

        // PUT with body Id = "s2" but URL id = "s1"
        var req = new UpsertMcpServerRequest("s2", "x", null, null, null, null, null, null);
        var put = await client.PutAsJsonAsync("/api/config/mcp-servers/s1", req);
        Assert.That((int)put.StatusCode, Is.EqualTo(200));

        var servers = await client.GetFromJsonAsync<McpServerDto[]>("/api/config/mcp-servers");
        Assert.That(servers!.Any(s => s.Id == "s1"), Is.True, "s1 must still exist");
        Assert.That(servers!.Any(s => s.Id == "s2"), Is.False, "s2 must not exist (rename via body was blocked)");
    }

    [Test]
    public async Task DeleteConfigMcpServer_ReferencedByCustomAgent_Returns400()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns([]);
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents =
            [
                new AgentConfig
                {
                    Id = "a1", Name = "A1", ModelId = "m",
                    McpServersInheritance = McpServersInheritance.Custom,
                    McpServers = ["s1"]
                }
            ],
            Models     = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users      = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions   = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-test-" + Guid.NewGuid()) },
            McpServers = [new McpServerConfig { Id = "s1", Command = "x" }]
        }, mcpManager: mcpManager);

        var resp = await f.CreateClient().DeleteAsync("/api/config/mcp-servers/s1");
        Assert.That((int)resp.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task GetMcpServerStatus_ReturnsStatusList()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns(
        [
            new McpServerStatus("s1", McpServerState.Connected, null, DateTimeOffset.UtcNow, 0, ["tool-a"]),
            new McpServerStatus("s2", McpServerState.Failed, "connection refused", null, 2, [])
        ]);

        await using var f = ApiTestFactory.Create(mcpManager: mcpManager);
        var client = f.CreateClient();

        var resp = await client.GetAsync("/api/config/mcp-servers/status");
        Assert.That((int)resp.StatusCode, Is.EqualTo(200));

        var body = await resp.Content.ReadAsStringAsync();
        var statuses = JsonSerializer.Deserialize<McpServerStatusDto[]>(body, ApiTestFactory.TestJsonOptions);
        Assert.That(statuses, Is.Not.Null);
        Assert.That(statuses!.Single(s => s.Id == "s1").State, Is.EqualTo(McpServerState.Connected));
        Assert.That(statuses!.Single(s => s.Id == "s2").State, Is.EqualTo(McpServerState.Failed));
    }

    [Test]
    public async Task PostMcpServerRetry_DisabledServer_Returns400()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns([]);
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents     = [new AgentConfig { Id = "a1", Name = "A1", ModelId = "m" }],
            Models     = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users      = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions   = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-test-" + Guid.NewGuid()) },
            McpServers = [new McpServerConfig { Id = "s1", Command = "x", Enabled = false }]
        }, mcpManager: mcpManager);

        var resp = await f.CreateClient().PostAsync("/api/config/mcp-servers/s1/retry", null);
        Assert.That((int)resp.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task PostMcpServerRetry_UnknownServer_Returns404()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns([]);
        await using var f = ApiTestFactory.Create(mcpManager: mcpManager);

        var resp = await f.CreateClient().PostAsync("/api/config/mcp-servers/unknown/retry", null);
        Assert.That((int)resp.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task PostMcpServerRetry_EnabledServer_CallsManagerAndReturns202()
    {
        var mcpManager = Substitute.For<IMcpConnectionManager>();
        mcpManager.GetStatus().Returns([]);
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents     = [new AgentConfig { Id = "a1", Name = "A1", ModelId = "m" }],
            Models     = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users      = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions   = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-test-" + Guid.NewGuid()) },
            McpServers = [new McpServerConfig { Id = "s1", Command = "x", Enabled = true }]
        }, mcpManager: mcpManager);

        var resp = await f.CreateClient().PostAsync("/api/config/mcp-servers/s1/retry", null);
        Assert.That((int)resp.StatusCode, Is.EqualTo(202));
        await mcpManager.Received(1).RetryAsync("s1", Arg.Any<CancellationToken>());
    }
}
