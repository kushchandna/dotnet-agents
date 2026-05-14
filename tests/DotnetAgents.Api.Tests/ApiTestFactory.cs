using System.Text.Json;
using DotnetAgents.Core.Models;
using DotnetAgents.Core.Runtime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;

namespace DotnetAgents.Api.Tests;

public static class ApiTestFactory
{
    public static WebApplicationFactory<Program> Create(
        AgentsConfig? config = null, IAgentRuntime? runtime = null)
    {
        var cfg = config ?? new AgentsConfig
        {
            Agents = [new AgentConfig { Id = "assistant", Name = "Assistant", ModelId = "m", Tools = ["echo"] }],
            Models = [new ModelConfig { Id = "m", Provider = ModelProvider.OpenAI, ModelName = "gpt-4o", ApiKeyEnvVar = "DUMMY" }],
            Users  = [new UserConfig { Id = "alice", DisplayName = "Alice" }, new UserConfig { Id = "bob", DisplayName = "Bob" }],
            Sessions = new SessionsConfig { Directory = Path.Combine(Path.GetTempPath(), "api-test-" + Guid.NewGuid()) }
        };

        var configPath = Path.GetTempFileName();
        File.WriteAllText(configPath,
            JsonSerializer.Serialize(cfg, DotnetAgents.Core.Configuration.ConfigLoader.JsonOptions));

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AgentsConfigPath", configPath);
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                if (runtime is not null)
                {
                    var existing = services.SingleOrDefault(d => d.ServiceType == typeof(IAgentRuntime));
                    if (existing is not null) services.Remove(existing);
                    services.AddSingleton(runtime);
                }
            });
        });
    }
}
