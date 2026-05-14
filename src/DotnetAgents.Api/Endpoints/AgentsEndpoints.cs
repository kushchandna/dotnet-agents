using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Configuration;

namespace DotnetAgents.Api.Endpoints;

public static class AgentsEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/agents", (IConfigurationService cfg) =>
            cfg.Config.Agents.Select(a => new AgentDto(a.Id, a.Name, a.Description, a.ModelId, a.Tools)).ToArray())
           .WithName("ListAgents").WithOpenApi();

        app.MapGet("/api/agents/{agentId}", (string agentId, IConfigurationService cfg) =>
        {
            var a = cfg.Config.Agents.FirstOrDefault(x => x.Id == agentId);
            return a is null
                ? Results.NotFound()
                : Results.Ok(new AgentDto(a.Id, a.Name, a.Description, a.ModelId, a.Tools));
        }).WithName("GetAgent").WithOpenApi();
    }
}
