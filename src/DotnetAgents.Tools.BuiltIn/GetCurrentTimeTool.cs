using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn;

public sealed class GetCurrentTimeTool : IBuiltInTool
{
    public string Id => "get_current_time";

    public AIFunction AsAIFunction() =>
        AIFunctionFactory.Create(GetTime, "get_current_time", "Returns the current UTC time as an ISO 8601 string.");

    [Description("Returns the current UTC time as an ISO 8601 string.")]
    private static string GetTime() => DateTimeOffset.UtcNow.ToString("O");
}
