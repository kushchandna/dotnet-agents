using System.Net.Http.Json;
using DotnetAgents.Api.Dto;

namespace DotnetAgents.Api.Tests;

[TestFixture]
public class UsersAndAgentsEndpointTests
{
    [Test]
    public async Task GetUsers_ReturnsAllConfiguredUsers()
    {
        await using var f = ApiTestFactory.Create();
        var users = await f.CreateClient().GetFromJsonAsync<UserDto[]>("/api/users");
        Assert.That(users, Is.Not.Null);
        Assert.That(users!.Select(u => u.Id), Is.EquivalentTo(new[] { "alice", "bob" }));
    }

    [Test]
    public async Task GetAgents_ReturnsAllConfiguredAgents()
    {
        await using var f = ApiTestFactory.Create();
        var agents = await f.CreateClient().GetFromJsonAsync<AgentDto[]>("/api/agents");
        Assert.That(agents!.Single().Id, Is.EqualTo("assistant"));
    }

    [Test]
    public async Task GetAgent_ById_Returns200()
    {
        await using var f = ApiTestFactory.Create();
        var agent = await f.CreateClient().GetFromJsonAsync<AgentDto>("/api/agents/assistant");
        Assert.That(agent!.Name, Is.EqualTo("Assistant"));
    }

    [Test]
    public async Task GetAgent_Unknown_Returns404()
    {
        await using var f = ApiTestFactory.Create();
        var resp = await f.CreateClient().GetAsync("/api/agents/nope");
        Assert.That((int)resp.StatusCode, Is.EqualTo(404));
    }
}
