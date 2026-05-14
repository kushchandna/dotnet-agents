using System.Net.Http.Json;

namespace DotnetAgents.Api.Tests;

[TestFixture]
public class HealthEndpointTests
{
    [Test]
    public async Task Health_Returns200WithStatusOk()
    {
        await using var factory = ApiTestFactory.Create();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.That(body!["status"], Is.EqualTo("ok"));
    }
}
