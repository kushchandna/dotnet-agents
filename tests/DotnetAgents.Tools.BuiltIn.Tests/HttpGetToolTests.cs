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
}
