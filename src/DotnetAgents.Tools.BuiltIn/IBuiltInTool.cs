using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn;

public interface IBuiltInTool
{
    string Id { get; }
    AIFunction AsAIFunction();
}
