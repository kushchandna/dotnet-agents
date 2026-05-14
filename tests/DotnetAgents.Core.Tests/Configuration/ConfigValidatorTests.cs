using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Tests.Configuration;

[TestFixture]
public class ConfigValidatorTests
{
    private static AgentsConfig Valid() => new()
    {
        Agents = [new() { Id="a", Name="A", ModelId="m", Tools=["echo"] }],
        Models = [new() { Id="m", Provider=ModelProvider.OpenAI, ModelName="gpt-4o", ApiKeyEnvVar="OPENAI_API_KEY" }],
        Users  = [new() { Id="u", DisplayName="U" }],
        Sessions = new() { Directory="./sessions" }
    };

    [Test]
    public void Valid_ReturnsNoErrors()
    {
        var errors = ConfigValidator.Validate(Valid());
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void DuplicateAgentIds_ReportedOnce()
    {
        var cfg = Valid() with { Agents = [
            new(){Id="dup",Name="A",ModelId="m",Tools=["echo"]},
            new(){Id="dup",Name="B",ModelId="m",Tools=["echo"]}] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Select(e => e.Path), Has.Some.Contains("agents"));
        Assert.That(errors.Count(e => e.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
    }

    [Test]
    public void AgentReferencesUnknownModel_Reports()
    {
        var cfg = Valid() with { Agents = [new(){Id="a",Name="A",ModelId="missing",Tools=["echo"]}] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("missing")), Is.True);
    }

    [Test]
    public void AgentReferencesUnknownTool_Reports()
    {
        var cfg = Valid() with { Agents = [new(){Id="a",Name="A",ModelId="m",Tools=["nonexistent"]}] };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("nonexistent")), Is.True);
    }

    [Test]
    public void OpenAICompatible_WithoutEndpoint_Reports()
    {
        var cfg = Valid() with { Models = [new(){Id="m",Provider=ModelProvider.OpenAICompatible,ModelName="x"}],
                                  Agents=[new(){Id="a",Name="A",ModelId="m",Tools=[]}]};
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("endpoint", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void OpenAI_WithoutApiKeyEnvVar_Reports()
    {
        var cfg = Valid() with { Models = [new(){Id="m",Provider=ModelProvider.OpenAI,ModelName="x"}],
                                  Agents=[new(){Id="a",Name="A",ModelId="m",Tools=[]}]};
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Any(e => e.Message.Contains("apiKeyEnvVar", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void MultipleErrors_AllReturned()
    {
        var cfg = Valid() with {
            Agents = [new(){Id="",Name="",ModelId="oops",Tools=["x"]}],
            Models = [new(){Id="",Provider=ModelProvider.OpenAI,ModelName=""}],
            Users  = [new(){Id="",DisplayName=""}],
            Sessions = new(){Directory=""}
        };
        var errors = ConfigValidator.Validate(cfg);
        Assert.That(errors.Count, Is.GreaterThanOrEqualTo(5));
    }
}
