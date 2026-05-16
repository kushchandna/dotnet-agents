using DotnetAgents.Tools.BuiltIn;

namespace DotnetAgents.Api.Endpoints;

public static class ToolsEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/tools", (BuiltInToolRegistry registry) =>
            registry.GetAvailableToolIds())
           .WithName("ListTools").WithOpenApi();
    }
}
