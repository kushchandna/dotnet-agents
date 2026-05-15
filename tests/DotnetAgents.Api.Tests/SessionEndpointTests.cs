using System.Net;
using System.Net.Http.Json;
using DotnetAgents.Api.Dto;

namespace DotnetAgents.Api.Tests;

[TestFixture]
public class SessionEndpointTests
{
    [Test]
    public async Task Create_Then_List_RoundtripsSession()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();
        var post = await client.PostAsJsonAsync("/api/users/alice/sessions", new { agentId = "assistant" });
        post.EnsureSuccessStatusCode();
        var created = await post.Content.ReadFromJsonAsync<SessionDto>();

        var list = await client.GetFromJsonAsync<SessionDto[]>("/api/users/alice/sessions");
        Assert.That(list!.Select(s => s.Id), Has.Member(created!.Id));
    }

    [Test]
    public async Task GetSession_Unknown_Returns404()
    {
        await using var f = ApiTestFactory.Create();
        var resp = await f.CreateClient().GetAsync("/api/users/alice/sessions/missing");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_RemovesSession()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();
        var s = await (await client.PostAsJsonAsync("/api/users/alice/sessions", new { agentId = "assistant" }))
            .Content.ReadFromJsonAsync<SessionDto>();
        var del = await client.DeleteAsync($"/api/users/alice/sessions/{s!.Id}");
        Assert.That(del.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var get = await client.GetAsync($"/api/users/alice/sessions/{s.Id}");
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_WithUnknownAgent_Returns400()
    {
        await using var f = ApiTestFactory.Create();
        var resp = await f.CreateClient().PostAsJsonAsync("/api/users/alice/sessions", new { agentId = "nope" });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_WithUnknownUser_Returns400()
    {
        await using var f = ApiTestFactory.Create();
        var resp = await f.CreateClient().PostAsJsonAsync("/api/users/ghost/sessions", new { agentId = "assistant" });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetMessages_ForNewSession_ReturnsEmpty()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();
        var s = await (await client.PostAsJsonAsync("/api/users/alice/sessions", new { agentId = "assistant" }))
            .Content.ReadFromJsonAsync<SessionDto>();
        var msgs = await client.GetFromJsonAsync<MessageDto[]>($"/api/users/alice/sessions/{s!.Id}/messages");
        Assert.That(msgs, Is.Empty);
    }
}
