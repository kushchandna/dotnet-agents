using DotnetAgents.Tools.BuiltIn;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn.Tests;

[TestFixture]
public class EchoToolTests
{
    [Test]
    public async Task Invoke_EchoesInput()
    {
        var tool = new EchoTool();
        var fn = tool.AsAIFunction();
        var args = new AIFunctionArguments { ["text"] = "hello" };
        var result = (await fn.InvokeAsync(args))?.ToString();
        Assert.That(result, Is.EqualTo("hello"));
    }
}
