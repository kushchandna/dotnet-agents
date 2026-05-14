using DotnetAgents.Core.Models;
using DotnetAgents.Tools.BuiltIn;

namespace DotnetAgents.Tools.BuiltIn.Tests;

[TestFixture]
public class BuiltInToolRegistryTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(), disposeHandler: false);
    }

    private static IHttpClientFactory NewFactory() => new StubFactory();

    [Test]
    public void Get_UnknownId_ReturnsNull()
    {
        var registry = new BuiltInToolRegistry(toolsConfig: null, NewFactory());
        Assert.That(registry.Get("does_not_exist"), Is.Null);
    }

    [Test]
    public void Get_KnownIds_ReturnNonNull()
    {
        var registry = new BuiltInToolRegistry(toolsConfig: null, NewFactory());
        Assert.That(registry.Get("echo"), Is.Not.Null);
        Assert.That(registry.Get("get_current_time"), Is.Not.Null);
    }

    [Test]
    public void Resolve_FiltersUnknownsAndReturnsOnlyMatched()
    {
        var registry = new BuiltInToolRegistry(toolsConfig: null, NewFactory());
        var resolved = registry.Resolve(["echo", "does_not_exist", "get_current_time"]);
        Assert.That(resolved, Has.Count.EqualTo(2));
        Assert.That(resolved.Select(f => f.Name), Is.EquivalentTo(new[] { "echo", "get_current_time" }));
    }

    [Test]
    public void NullToolsConfig_RegistersOnlyDefaultTools()
    {
        var registry = new BuiltInToolRegistry(toolsConfig: null, NewFactory());
        Assert.That(registry.Get("get_current_time"), Is.Not.Null);
        Assert.That(registry.Get("echo"), Is.Not.Null);
        Assert.That(registry.Get("read_file"), Is.Null);
        Assert.That(registry.Get("http_get"), Is.Null);
    }

    [Test]
    public void OnlyReadFileConfigured_RegistersReadFileNotHttpGet()
    {
        var cfg = new ToolsConfig
        {
            ReadFile = new ReadFileToolConfig { SandboxRoot = Path.GetTempPath() }
        };
        var registry = new BuiltInToolRegistry(cfg, NewFactory());
        Assert.That(registry.Get("read_file"), Is.Not.Null);
        Assert.That(registry.Get("http_get"), Is.Null);
        Assert.That(registry.Get("echo"), Is.Not.Null);
        Assert.That(registry.Get("get_current_time"), Is.Not.Null);
    }

    [Test]
    public void OnlyHttpGetConfigured_RegistersHttpGetNotReadFile()
    {
        var cfg = new ToolsConfig
        {
            HttpGet = new HttpGetToolConfig { Allowlist = new[] { "https://api.example.com" } }
        };
        var registry = new BuiltInToolRegistry(cfg, NewFactory());
        Assert.That(registry.Get("http_get"), Is.Not.Null);
        Assert.That(registry.Get("read_file"), Is.Null);
        Assert.That(registry.Get("echo"), Is.Not.Null);
        Assert.That(registry.Get("get_current_time"), Is.Not.Null);
    }
}
