using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Tests.Configuration;

[TestFixture]
public class ConfigValidatorTests
{
    private static AgentsConfig Valid() => new()
    {
        Agents = [new() { Id="a", Name="A", ModelId="m", Tools=["echo"] }],
        Models = [new() { Id="m", Provider=ModelProvider.OpenAI, ModelName="gpt-4o", ApiKeyEnvVar="OPENAI_API_KEY" }],
        Users  = [new() { Id="u", DisplayName="U" }],
        Sessions = new() { Directory="./sessions" }
    };

    [Test]
    public void Valid_ReturnsNoErrors()
    {
        var errors = ConfigValidator.Validate(Valid());
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void DuplicateAgentIds_ReportedOnce()
    {
        var cfg = Valid() with { Agents = [
            new(){Id="dup",Name="A",ModelId="m",Tools=["echo"]},
            new(){Id="dup",Name="B",ModelId="m",Tools=["echo"]}] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Select(e => e.Path), Has.Some.Contains("agents"));
        Assert.That(errors.Count(e => e.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
    }

    [Test]
    public void AgentReferencesUnknownModel_Reports()
    {
        var cfg = Valid() with { Agents = [new(){Id="a",Name="A",ModelId="missing",Tools=["echo"]}] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("missing")), Is.True);
    }

    [Test]
    public void AgentReferencesUnknownTool_Reports()
    {
        var cfg = Valid() with { Agents = [new(){Id="a",Name="A",ModelId="m",Tools=["nonexistent"]}] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("nonexistent")), Is.True);
    }

    [Test]
    public void OpenAICompatible_WithoutEndpoint_Reports()
    {
        var cfg = Valid() with { Models = [new(){Id="m",Provider=ModelProvider.OpenAICompatible,ModelName="x"}],
                                  Agents=[new(){Id="a",Name="A",ModelId="m",Tools=[]}]};
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("endpoint", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void OpenAI_WithoutApiKeyEnvVar_Reports()
    {
        var cfg = Valid() with { Models = [new(){Id="m",Provider=ModelProvider.OpenAI,ModelName="x"}],
                                  Agents=[new(){Id="a",Name="A",ModelId="m",Tools=[]}]};
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("apiKeyEnvVar", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    // --- MCP server tests ---

    private static McpServerConfig ValidMcpServer(string id = "fs") =>
        new() { Id = id, Command = "npx", Args = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"] };

    [Test]
    public void McpServers_Valid_NoErrors()
    {
        var cfg = Valid() with { McpServers = [ValidMcpServer()] };
        Assert.That(ConfigValidator.Validate(cfg), Is.Empty);
    }

    [Test]
    public void McpServer_DuplicateId_Reports()
    {
        var cfg = Valid() with { McpServers = [ValidMcpServer("s1"), ValidMcpServer("s1")] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("duplicate") && e.Message.Contains("s1")), Is.True);
    }

    [Test]
    public void McpServer_MissingCommandAndUrl_Reports()
    {
        var cfg = Valid() with { McpServers = [new() { Id = "bad" }] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Path.Contains("mcpServers") && e.Message.Contains("command")), Is.True);
    }

    [Test]
    public void McpServer_UrlOnly_NoErrors()
    {
        var cfg = Valid() with { McpServers = [new() { Id = "remote", Url = "http://mcp.example.com/sse" }] };
        Assert.That(ConfigValidator.Validate(cfg), Is.Empty);
    }

    [Test]
    public void Agent_Custom_ReferencesUnknownServer_Reports()
    {
        var cfg = Valid() with
        {
            McpServers = [ValidMcpServer("known")],
            Agents = [new() { Id="a", Name="A", ModelId="m", McpServersInheritance=McpServersInheritance.Custom, McpServers=["unknown"] }]
        };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("unknown")), Is.True);
    }

    [Test]
    public void Agent_Custom_NoServersInList_NoErrors()
    {
        var cfg = Valid() with
        {
            Agents = [new() { Id="a", Name="A", ModelId="m", McpServersInheritance=McpServersInheritance.Custom }]
        };
        Assert.That(ConfigValidator.Validate(cfg), Is.Empty);
    }

    [Test]
    public void Agent_NonCustom_WithNonEmptyMcpServersList_Reports()
    {
        var cfg = Valid() with
        {
            McpServers = [ValidMcpServer("fs")],
            Agents = [new() { Id="a", Name="A", ModelId="m", McpServersInheritance=McpServersInheritance.All, McpServers=["fs"] }]
        };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Path.Contains("mcpServers") && e.Message.Contains("no effect")), Is.True);
    }

    [Test]
    public void Agent_Custom_KnownServer_NoErrors()
    {
        var cfg = Valid() with
        {
            McpServers = [ValidMcpServer("fs")],
            Agents = [new() { Id="a", Name="A", ModelId="m", McpServersInheritance=McpServersInheritance.Custom, McpServers=["fs"] }]
        };
        Assert.That(ConfigValidator.Validate(cfg), Is.Empty);
    }

    [Test]
    public void MultipleErrors_AllReturned()
    {
        var cfg = Valid() with {
            Agents = [new(){Id="",Name="",ModelId="oops",Tools=["x"]}],
            Models = [new(){Id="",Provider=ModelProvider.OpenAI,ModelName=""}],
            Users  = [new(){Id="",DisplayName=""}],
            Sessions = new(){Directory=""}
        };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Count, Is.GreaterThanOrEqualTo(5));
    }

    [Test]
    public void Validate_McpServerNegativeRetryLimit_ReturnsError()
    {
        var cfg = Valid() with { McpServers = [new() { Id = "test", Url = "http://example.com", RetryLimit = -1 }] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Path.Contains("retryLimit") && e.Message.Contains("≥ 0")), Is.True);
    }

    [Test]
    public void Validate_McpServerZeroRetryInterval_ReturnsError()
    {
        var cfg = Valid() with { McpServers = [new() { Id = "test", Url = "http://example.com", RetryInterval = 0 }] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Path.Contains("retryInterval") && e.Message.Contains("≥ 1")), Is.True);
    }
}
