using DotnetAgents.Core.Models;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Tools.BuiltIn;

public sealed class BuiltInToolRegistry
{
    private readonly Dictionary<string, AIFunction> _functions;

    public BuiltInToolRegistry(ToolsConfig? toolsConfig, IHttpClientFactory httpClientFactory)
    {
        var tools = new List<IBuiltInTool> { new GetCurrentTimeTool(), new EchoTool() };
        if (toolsConfig?.ReadFile is { } rf)
            tools.Add(new ReadFileTool(rf.SandboxRoot));
        if (toolsConfig?.HttpGet is { } hg)
            tools.Add(new HttpGetTool(hg.Allowlist, httpClientFactory));
        _functions = tools.ToDictionary(t => t.Id, t => t.AsAIFunction());
    }

    public AIFunction? Get(string toolId) => _functions.GetValueOrDefault(toolId);

    public IReadOnlyList<AIFunction> Resolve(IEnumerable<string> toolIds) =>
        toolIds.Select(Get).Where(f => f is not null).Select(f => f!).ToList();
}
