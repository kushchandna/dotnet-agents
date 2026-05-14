using DotnetAgents.Core.Models;
using DotnetAgents.Runtime.Providers;

namespace DotnetAgents.Runtime.Tests.Providers;

[TestFixture]
public class ModelProviderFactoryTests
{
    [Test]
    public void Ollama_NoEndpoint_UsesDefault()
    {
        var f = new ModelProviderFactory();
        var client = f.Create(new ModelConfig { Id = "m", Provider = ModelProvider.Ollama, ModelName = "llama3" });
        Assert.That(client, Is.Not.Null);
        client.Dispose();
    }

    [Test]
    public void OpenAI_MissingEnvVar_Throws()
    {
        Environment.SetEnvironmentVariable("UNIT_TEST_MISSING_KEY", null);
        var f = new ModelProviderFactory();
        var model = new ModelConfig
        {
            Id = "m",
            Provider = ModelProvider.OpenAI,
            ModelName = "gpt-4o",
            ApiKeyEnvVar = "UNIT_TEST_MISSING_KEY"
        };
        Assert.Throws<InvalidOperationException>(() => f.Create(model));
    }

    [Test]
    public void OpenAI_WithEnvVar_Constructs()
    {
        Environment.SetEnvironmentVariable("UNIT_TEST_OPENAI_KEY", "sk-test");
        try
        {
            var f = new ModelProviderFactory();
            var client = f.Create(new ModelConfig
            {
                Id = "m",
                Provider = ModelProvider.OpenAI,
                ModelName = "gpt-4o",
                ApiKeyEnvVar = "UNIT_TEST_OPENAI_KEY"
            });
            Assert.That(client, Is.Not.Null);
            client.Dispose();
        }
        finally { Environment.SetEnvironmentVariable("UNIT_TEST_OPENAI_KEY", null); }
    }

    [Test]
    public void OpenAICompatible_WithEndpoint_Constructs()
    {
        Environment.SetEnvironmentVariable("UNIT_TEST_COMPAT_KEY", "x");
        try
        {
            var f = new ModelProviderFactory();
            var client = f.Create(new ModelConfig
            {
                Id = "m",
                Provider = ModelProvider.OpenAICompatible,
                ModelName = "qwen",
                Endpoint = "http://localhost:8080/v1",
                ApiKeyEnvVar = "UNIT_TEST_COMPAT_KEY"
            });
            Assert.That(client, Is.Not.Null);
            client.Dispose();
        }
        finally { Environment.SetEnvironmentVariable("UNIT_TEST_COMPAT_KEY", null); }
    }
}
