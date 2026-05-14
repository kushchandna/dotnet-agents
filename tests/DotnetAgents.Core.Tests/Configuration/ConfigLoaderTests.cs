using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Tests.Configuration;

[TestFixture]
public class ConfigLoaderTests
{
    private string _tempFile = null!;

    [SetUp]
    public void SetUp() => _tempFile = Path.GetTempFileName();

    [TearDown]
    public void TearDown() { if (File.Exists(_tempFile)) File.Delete(_tempFile); }

    private void Write(string json) => File.WriteAllText(_tempFile, json);

    [Test]
    public async Task LoadAsync_FromExplicitPath_ParsesAllSections()
    {
        Write("""
        {
          "agents": [{"id":"a1","name":"A","modelId":"m1","systemPrompt":"hi","tools":["echo"]}],
          "models": [{"id":"m1","provider":"openai","modelName":"gpt-4o","apiKeyEnvVar":"OPENAI_API_KEY"}],
          "users":  [{"id":"u1","displayName":"User One"}],
          "sessions": {"directory":"./sessions"},
          "tools": {"readFile":{"sandboxRoot":"./files"},"httpGet":{"allowlist":["https://api.x"]}}
        }
        """);

        var cfg = await ConfigLoader.LoadAsync(_tempFile);

        Assert.That(cfg.Agents, Has.Count.EqualTo(1));
        Assert.That(cfg.Agents[0].Id, Is.EqualTo("a1"));
        Assert.That(cfg.Models[0].Provider, Is.EqualTo(ModelProvider.OpenAI));
        Assert.That(cfg.Users[0].DisplayName, Is.EqualTo("User One"));
        Assert.That(cfg.Sessions.Directory, Is.EqualTo("./sessions"));
        Assert.That(cfg.Tools!.HttpGet!.Allowlist, Has.Member("https://api.x"));
    }

    [Test]
    public void LoadAsync_FileMissing_Throws()
    {
        Assert.ThrowsAsync<FileNotFoundException>(() => ConfigLoader.LoadAsync("/nope/x.json"));
    }

    [Test]
    public async Task LoadAsync_ProviderEnumStrings_AreCaseInsensitive()
    {
        Write("""
        {
          "agents":[{"id":"a","name":"A","modelId":"m","tools":[]}],
          "models":[
            {"id":"m","provider":"OPENAI","modelName":"gpt-4o","apiKeyEnvVar":"K"},
            {"id":"o","provider":"Ollama","modelName":"llama3","endpoint":"http://x:11434"},
            {"id":"c","provider":"openai-compatible","modelName":"q","endpoint":"http://x/v1"}
          ],
          "users":[{"id":"u","displayName":"U"}],
          "sessions":{"directory":"./s"}
        }
        """);

        var cfg = await ConfigLoader.LoadAsync(_tempFile);
        Assert.That(cfg.Models[0].Provider, Is.EqualTo(ModelProvider.OpenAI));
        Assert.That(cfg.Models[1].Provider, Is.EqualTo(ModelProvider.Ollama));
        Assert.That(cfg.Models[2].Provider, Is.EqualTo(ModelProvider.OpenAICompatible));
    }
}
