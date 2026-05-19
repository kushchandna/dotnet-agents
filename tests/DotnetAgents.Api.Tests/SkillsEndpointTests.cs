using System.Net;
using System.Net.Http.Json;
using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Models;
using DotnetAgents.Core.Skills;
using NSubstitute;

namespace DotnetAgents.Api.Tests;

[TestFixture]
public class SkillsEndpointTests
{
    [Test]
    public async Task GetSkillDirectories_ReturnsEmptyListInitially()
    {
        await using var f = ApiTestFactory.Create();
        var dirs = await f.CreateClient().GetFromJsonAsync<string[]>("/api/config/skill-directories");
        Assert.That(dirs, Is.Not.Null);
        Assert.That(dirs, Is.Empty);
    }

    [Test]
    public async Task PostSkillDirectory_AddsDirectory_AppearsInGet()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/config/skill-directories", new { path = "/tmp/skills" });
        Assert.That((int)resp.StatusCode, Is.EqualTo(200));

        var dirs = await client.GetFromJsonAsync<string[]>("/api/config/skill-directories");
        Assert.That(dirs, Contains.Item("/tmp/skills"));
    }

    [Test]
    public async Task DeleteSkillDirectory_RemovesDirectory_AbsentInGet()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        await client.PostAsJsonAsync("/api/config/skill-directories", new { path = "/tmp/skills" });

        var del = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/config/skill-directories")
        {
            Content = JsonContent.Create(new { path = "/tmp/skills" })
        });
        Assert.That((int)del.StatusCode, Is.EqualTo(204));

        var dirs = await client.GetFromJsonAsync<string[]>("/api/config/skill-directories");
        Assert.That(dirs, Does.Not.Contain("/tmp/skills"));
    }

    [Test]
    public async Task GetSkills_ReturnsMockedSkills()
    {
        var discovery = Substitute.For<ISkillDiscoveryService>();
        discovery.GetAllSkillsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<SkillInfo>
            {
                new("greet", "Greets the user", "echo hello", "/tmp/skills/greet.md"),
                new("farewell", "Says goodbye", "echo bye", "/tmp/skills/farewell.md")
            });

        await using var f = ApiTestFactory.Create(skillDiscovery: discovery);
        var skills = await f.CreateClient().GetFromJsonAsync<SkillDto[]>("/api/config/skills");

        Assert.That(skills, Is.Not.Null);
        Assert.That(skills!.Length, Is.EqualTo(2));
        Assert.That(skills.Select(s => s.Id), Is.EquivalentTo(new[] { "greet", "farewell" }));
        Assert.That(skills.Single(s => s.Id == "greet").Description, Is.EqualTo("Greets the user"));
    }
}
