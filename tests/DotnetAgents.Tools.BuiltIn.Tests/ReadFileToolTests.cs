using DotnetAgents.Tools.BuiltIn;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn.Tests;

[TestFixture]
public class ReadFileToolTests
{
    private string _sandbox = null!;
    private string _outside = null!;

    [SetUp]
    public void SetUp()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "rf-" + Guid.NewGuid());
        _outside = Path.Combine(Path.GetTempPath(), "out-" + Guid.NewGuid());
        Directory.CreateDirectory(_sandbox);
        Directory.CreateDirectory(_outside);
        File.WriteAllText(Path.Combine(_sandbox, "ok.txt"), "inside");
        File.WriteAllText(Path.Combine(_outside, "secret.txt"), "secret");
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_sandbox, true);
        Directory.Delete(_outside, true);
    }

    [Test]
    public async Task Read_FileInSandbox_ReturnsContent()
    {
        var fn = new ReadFileTool(_sandbox).AsAIFunction();
        var result = await fn.InvokeAsync(new AIFunctionArguments { ["path"] = "ok.txt" });
        Assert.That(result?.ToString(), Is.EqualTo("inside"));
    }

    [Test]
    public void Read_PathTraversal_Blocked()
    {
        var fn = new ReadFileTool(_sandbox).AsAIFunction();
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fn.InvokeAsync(new AIFunctionArguments { ["path"] = "../" + Path.GetFileName(_outside) + "/secret.txt" }).AsTask());
    }

    [Test]
    public void Read_AbsolutePathOutsideSandbox_Blocked()
    {
        var fn = new ReadFileTool(_sandbox).AsAIFunction();
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fn.InvokeAsync(new AIFunctionArguments { ["path"] = Path.Combine(_outside, "secret.txt") }).AsTask());
    }
}
