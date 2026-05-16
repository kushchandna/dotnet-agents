using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Tests.Models;

[TestFixture]
public class McpServerConfigEqualityTests
{
    private static McpServerConfig Base() => new()
    {
        Id = "srv1",
        Command = "npx",
        Args = ["--yes", "some-server"],
        Env = new Dictionary<string, string> { ["KEY"] = "VALUE" },
        Url = null,
        Enabled = true,
        RetryLimit = 3,
        RetryInterval = 5
    };

    [Test]
    public void SameContent_SeparateArgsInstances_AreEqual()
    {
        var a = Base() with { Args = new List<string> { "--yes", "some-server" } };
        var b = Base() with { Args = new List<string> { "--yes", "some-server" } };
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void SameContent_SeparateEnvDictionaryInstances_AreEqual()
    {
        var a = Base() with { Env = new Dictionary<string, string> { ["KEY"] = "VALUE" } };
        var b = Base() with { Env = new Dictionary<string, string> { ["KEY"] = "VALUE" } };
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void DifferentEnvValue_NotEqual()
    {
        var a = Base() with { Env = new Dictionary<string, string> { ["KEY"] = "VALUE" } };
        var b = Base() with { Env = new Dictionary<string, string> { ["KEY"] = "OTHER" } };
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void DifferentArgsOrdering_NotEqual()
    {
        var a = Base() with { Args = new List<string> { "alpha", "beta" } };
        var b = Base() with { Args = new List<string> { "beta", "alpha" } };
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void DifferentRetryLimit_NotEqual()
    {
        var a = Base() with { RetryLimit = 3 };
        var b = Base() with { RetryLimit = 5 };
        Assert.That(a, Is.Not.EqualTo(b));
    }
}
