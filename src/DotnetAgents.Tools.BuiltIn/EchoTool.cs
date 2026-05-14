using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn;

public sealed class EchoTool : IBuiltInTool
{
    public string Id => "echo";

    public AIFunction AsAIFunction() =>
        AIFunctionFactory.Create(Echo, "echo", "Echoes back the supplied text.");

    private static string Echo([Description("Text to echo back")] string text) => text;
}
