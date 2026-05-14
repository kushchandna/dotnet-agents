using System.Net;
using DotnetAgents.Tools.BuiltIn;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn.Tests;

[TestFixture]
public class HttpGetToolTests
{
    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        public Uri? LastUri;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            LastUri = req.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    [Test]
    public async Task AllowedUrl_ReturnsBody()
    {
        var handler = new StubHandler("ok-body");
        var tool = new HttpGetTool(["https://api.example.com"], new StubFactory(handler));
        var fn = tool.AsAIFunction();
        var result = await fn.InvokeAsync(new AIFunctionArguments { ["url"] = "https://api.example.com/path" });
        Assert.That(result?.ToString(), Is.EqualTo("ok-body"));
        Assert.That(handler.LastUri?.ToString(), Is.EqualTo("https://api.example.com/path"));
    }

    [Test]
    public void DisallowedUrl_Throws()
    {
        var tool = new HttpGetTool(["https://api.example.com"], new StubFactory(new StubHandler("")));
        var fn = tool.AsAIFunction();
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fn.InvokeAsync(new AIFunctionArguments { ["url"] = "https://evil.test/x" }).AsTask());
    }

    [Test]
    public void SubdomainSuffixBypass_IsBlocked()
    {
        var tool = new HttpGetTool(["https://api.example.com"], new StubFactory(new StubHandler("")));
        var fn = tool.AsAIFunction();
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fn.InvokeAsync(new AIFunctionArguments { ["url"] = "https://api.example.com.evil.test/leak" }).AsTask());
    }

    [Test]
    public void UserinfoBypass_IsBlocked()
    {
        var tool = new HttpGetTool(["https://api.example.com"], new StubFactory(new StubHandler("")));
        var fn = tool.AsAIFunction();
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fn.InvokeAsync(new AIFunctionArguments { ["url"] = "https://api.example.com@evil.test/leak" }).AsTask());
    }

    [Test]
    public void SchemeMismatch_IsBlocked()
    {
        var tool = new HttpGetTool(["https://api.example.com"], new StubFactory(new StubHandler("")));
        var fn = tool.AsAIFunction();
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fn.InvokeAsync(new AIFunctionArguments { ["url"] = "http://api.example.com/" }).AsTask());
    }

    [Test]
    public void PathPrefix_DifferentPath_IsBlocked()
    {
        var tool = new HttpGetTool(["https://api.example.com/v1"], new StubFactory(new StubHandler("")));
        var fn = tool.AsAIFunction();
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fn.InvokeAsync(new AIFunctionArguments { ["url"] = "https://api.example.com/v2/x" }).AsTask());
    }

    [Test]
    public async Task PathPrefix_MatchingPath_IsAllowed()
    {
        var handler = new StubHandler("v1-body");
        var tool = new HttpGetTool(["https://api.example.com/v1"], new StubFactory(handler));
        var fn = tool.AsAIFunction();
        var result = await fn.InvokeAsync(new AIFunctionArguments { ["url"] = "https://api.example.com/v1/foo" });
        Assert.That(result?.ToString(), Is.EqualTo("v1-body"));
    }

    [Test]
    public void NonHttpScheme_Throws()
    {
        var tool = new HttpGetTool(["https://api.example.com"], new StubFactory(new StubHandler("")));
        var fn = tool.AsAIFunction();
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fn.InvokeAsync(new AIFunctionArguments { ["url"] = "file:///etc/passwd" }).AsTask());
    }
}
