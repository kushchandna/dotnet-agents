using DotnetAgents.Tools.BuiltIn;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn.Tests;

[TestFixture]
public class TextStatsToolTests
{
    [Test]
    public async Task Invoke_ReturnsSomething()
    {
        var fn = new TextStatsTool().AsAIFunction();
        var result = await fn.InvokeAsync(new AIFunctionArguments { ["text"] = "one two three" });
        Assert.That(result, Is.Not.Null);
    }
}
