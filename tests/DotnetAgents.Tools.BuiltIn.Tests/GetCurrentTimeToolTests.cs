using DotnetAgents.Tools.BuiltIn;

namespace DotnetAgents.Tools.BuiltIn.Tests;

[TestFixture]
public class GetCurrentTimeToolTests
{
    [Test]
    public async Task Invoke_ReturnsRoundTripUtcString()
    {
        var tool = new GetCurrentTimeTool();
        var fn = tool.AsAIFunction();
        var result = (await fn.InvokeAsync())?.ToString();
        Assert.That(DateTimeOffset.TryParse(result, out var parsed), Is.True);
        Assert.That(parsed.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Tool_Id_IsExpected()
    {
        Assert.That(new GetCurrentTimeTool().Id, Is.EqualTo("get_current_time"));
    }
}
