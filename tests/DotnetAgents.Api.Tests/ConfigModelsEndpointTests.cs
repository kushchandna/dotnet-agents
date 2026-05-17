using System.Net.Http.Json;
using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Api.Tests;

[TestFixture]
public class ConfigModelsEndpointTests
{
    // ── GET ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetConfigModels_ReturnsConfiguredModels()
    {
        await using var f = ApiTestFactory.Create();
        var models = await f.CreateClient().GetFromJsonAsync<ModelSettingsDto[]>("/api/config/models", ApiTestFactory.TestJsonOptions);
        Assert.That(models, Is.Not.Null);
        Assert.That(models!.Single().Id, Is.EqualTo("m"));
    }

    // ── POST ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task PostConfigModel_CreatesModel_AppearsInGet()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var req = new UpsertModelRequest("gpt-4", ModelProvider.OpenAI, "gpt-4", null, "OPENAI_KEY");
        var post = await client.PostAsJsonAsync("/api/config/models", req);
        Assert.That((int)post.StatusCode, Is.EqualTo(201));

        var models = await client.GetFromJsonAsync<ModelSettingsDto[]>("/api/config/models", ApiTestFactory.TestJsonOptions);
        Assert.That(models!.Any(m => m.Id == "gpt-4"), Is.True);
    }

    [Test]
    public async Task PostConfigModel_DuplicateId_Returns409()
    {
        await using var f = ApiTestFactory.Create();
        var req = new UpsertModelRequest("m", ModelProvider.OpenAI, "gpt-4o", null, "DUMMY");
        var resp = await f.CreateClient().PostAsJsonAsync("/api/config/models", req);
        Assert.That((int)resp.StatusCode, Is.EqualTo(409));
    }

    // ── PUT ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task PutConfigModel_UpdatesModelName_ReflectedInGet()
    {
        await using var f = ApiTestFactory.Create();
        var client = f.CreateClient();

        var req = new UpsertModelRequest("m", ModelProvider.OpenAI, "gpt-4-turbo", null, "DUMMY");
        var put = await client.PutAsJsonAsync("/api/config/models/m", req);
        Assert.That((int)put.StatusCode, Is.EqualTo(200));

        var models = await client.GetFromJsonAsync<ModelSettingsDto[]>("/api/config/models", ApiTestFactory.TestJsonOptions);
        Assert.That(models!.Single(m => m.Id == "m").ModelName, Is.EqualTo("gpt-4-turbo"));
    }

    [Test]
    public async Task PutConfigModel_MissingId_Returns404()
    {
        await using var f = ApiTestFactory.Create();
        var req = new UpsertModelRequest("nonexistent", ModelProvider.OpenAI, "gpt-4o", null, "DUMMY");
        var resp = await f.CreateClient().PutAsJsonAsync("/api/config/models/nonexistent", req);
        Assert.That((int)resp.StatusCode, Is.EqualTo(404));
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteConfigModel_RemovesIt_AbsentInGet()
    {
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents   = [new AgentConfig { Id = "a1", Name = "A1", ModelId = "m1" }],
            Models   =
            [
                new ModelConfig { Id = "m1", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" },
                new ModelConfig { Id = "m2", Provider = ModelProvider.OpenAI, ModelName = "gpt-3.5", ApiKeyEnvVar = "DUMMY" }
            ],
            Users    = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-models-test-" + Guid.NewGuid()) }
        });
        var client = f.CreateClient();

        var del = await client.DeleteAsync("/api/config/models/m2");
        Assert.That((int)del.StatusCode, Is.EqualTo(204));

        var models = await client.GetFromJsonAsync<ModelSettingsDto[]>("/api/config/models", ApiTestFactory.TestJsonOptions);
        Assert.That(models!.Any(m => m.Id == "m2"), Is.False);
    }

    [Test]
    public async Task DeleteConfigModel_MissingId_Returns404()
    {
        await using var f = ApiTestFactory.Create();
        var resp = await f.CreateClient().DeleteAsync("/api/config/models/nonexistent");
        Assert.That((int)resp.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task DeleteConfigModel_LastModel_Returns400()
    {
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents   = [new AgentConfig { Id = "a1", Name = "A1", ModelId = "m" }],
            Models   = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users    = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-models-test-" + Guid.NewGuid()) }
        });
        var resp = await f.CreateClient().DeleteAsync("/api/config/models/m");
        Assert.That((int)resp.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task DeleteConfigModel_ReferencedByAgent_Returns400()
    {
        await using var f = ApiTestFactory.Create(new AgentsConfig
        {
            Agents   = [new AgentConfig { Id = "a1", Name = "A1", ModelId = "m1" }],
            Models   =
            [
                new ModelConfig { Id = "m1", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" },
                new ModelConfig { Id = "m2", Provider = ModelProvider.OpenAI, ModelName = "gpt-3.5", ApiKeyEnvVar = "DUMMY" }
            ],
            Users    = [new UserConfig { Id = "alice", DisplayName = "Alice" }],
            Sessions = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "cfg-models-test-" + Guid.NewGuid()) }
        });
        // m1 is used by a1 — deleting it should fail validation
        var resp = await f.CreateClient().DeleteAsync("/api/config/models/m1");
        Assert.That((int)resp.StatusCode, Is.EqualTo(400));
    }
}
