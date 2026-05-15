using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using DotnetAgents.Core.Runtime;

namespace DotnetAgents.Api.Tests;

[TestFixture]
public class MessagesStreamingTests
{
    private sealed class StubRuntime(Func<AgentRunRequest, IAsyncEnumerable<AgentStreamUpdate>> source) : IAgentRuntime
    {
        public IAsyncEnumerable<AgentStreamUpdate> RunStreamingAsync(AgentRunRequest request, CancellationToken ct = default)
            => source(request);
    }

    [Test]
    public async Task PostMessage_StreamsDeltaToolsAndDone()
    {
        async IAsyncEnumerable<AgentStreamUpdate> Stream([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new DeltaUpdate("Hello ");
            yield return new ToolCallUpdate("c1", "echo", "{\"text\":\"x\"}");
            yield return new ToolResultUpdate("c1", "echo", "x");
            yield return new DeltaUpdate("world");
            yield return new DoneUpdate("msg_42");
            await Task.CompletedTask;
        }

        await using var f = ApiTestFactory.Create(runtime: new StubRuntime(_ => Stream()));
        var client = f.CreateClient();
        var s = await (await client.PostAsJsonAsync("/api/users/alice/sessions", new { agentId = "assistant" }))
            .Content.ReadFromJsonAsync<DotnetAgents.Api.Dto.SessionDto>();

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"/api/users/alice/sessions/{s!.Id}/messages")
        { Content = JsonContent.Create(new { content = "hello" }) };
        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        Assert.That(resp.Content.Headers.ContentType!.MediaType, Is.EqualTo("text/event-stream"));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("\"type\":\"delta\""));
        Assert.That(body, Does.Contain("\"type\":\"tool_call\""));
        Assert.That(body, Does.Contain("\"type\":\"tool_result\""));
        Assert.That(body, Does.Contain("\"type\":\"done\""));
        Assert.That(body, Does.Contain("\"messageId\":\"msg_42\""));

        var eventCount = body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries).Count(p => p.StartsWith("data:"));
        Assert.That(eventCount, Is.EqualTo(5));
    }

    [Test]
    public async Task PostMessage_UnknownSession_Returns404()
    {
        await using var f = ApiTestFactory.Create();
        using var resp = await f.CreateClient().PostAsJsonAsync(
            "/api/users/alice/sessions/missing/messages", new { content = "x" });
        Assert.That((int)resp.StatusCode, Is.EqualTo(404));
    }
}
