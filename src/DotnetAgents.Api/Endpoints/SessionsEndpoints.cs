using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using DotnetAgents.Core.Sessions;

namespace DotnetAgents.Api.Endpoints;

public static class SessionsEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/users/{userId}/sessions", async (string userId, ISessionStore store,
                                                          IConfigurationService cfg) =>
        {
            if (cfg.Config.Users.All(u => u.Id != userId)) return Results.NotFound();
            var sessions = await store.ListAsync(userId);
            return Results.Ok(sessions.Select(ToDto).ToArray());
        }).WithName("ListSessions").WithOpenApi();

        app.MapPost("/api/users/{userId}/sessions",
            async (string userId, CreateSessionRequest req, ISessionStore store, IConfigurationService cfg) =>
            {
                if (cfg.Config.Users.All(u => u.Id != userId))
                    return Results.BadRequest(new { error = $"Unknown user '{userId}'" });
                if (cfg.Config.Agents.All(a => a.Id != req.AgentId))
                    return Results.BadRequest(new { error = $"Unknown agent '{req.AgentId}'" });
                var s = await store.CreateAsync(userId, req.AgentId);
                return Results.Created($"/api/users/{userId}/sessions/{s.Id}", ToDto(s));
            }).WithName("CreateSession").WithOpenApi();

        app.MapGet("/api/users/{userId}/sessions/{sessionId}",
            async (string userId, string sessionId, ISessionStore store) =>
            {
                var s = await store.GetAsync(userId, sessionId);
                return s is null ? Results.NotFound() : Results.Ok(ToDto(s));
            }).WithName("GetSession").WithOpenApi();

        app.MapDelete("/api/users/{userId}/sessions/{sessionId}",
            async (string userId, string sessionId, ISessionStore store) =>
            {
                await store.DeleteAsync(userId, sessionId);
                return Results.NoContent();
            }).WithName("DeleteSession").WithOpenApi();

        app.MapGet("/api/users/{userId}/sessions/{sessionId}/messages",
            async (string userId, string sessionId, ISessionStore store) =>
            {
                var session = await store.GetAsync(userId, sessionId);
                if (session is null) return Results.NotFound();
                var msgs = await store.GetMessagesAsync(userId, sessionId);
                return Results.Ok(msgs.Select(m => new MessageDto(m.Id, m.Role, m.Content, m.ToolCalls, m.Timestamp)).ToArray());
            }).WithName("ListMessages").WithOpenApi();

        app.MapGet("/api/users/{userId}/sessions/{sessionId}/raw",
            async (string userId, string sessionId, ISessionStore store, IConfigurationService cfg) =>
            {
                var session = await store.GetAsync(userId, sessionId);
                if (session is null) return Results.NotFound();

                var agent = cfg.Config.Agents.FirstOrDefault(a => a.Id == session.AgentId);
                if (agent is null) return Results.NotFound();

                var model = cfg.Config.Models.FirstOrDefault(m => m.Id == agent.ModelId);
                if (model is null) return Results.NotFound();

                IReadOnlyList<string> mcpServers = agent.McpServersInheritance switch
                {
                    DotnetAgents.Core.Models.McpServersInheritance.All    => cfg.Config.McpServers.Select(s => s.Id).ToArray(),
                    DotnetAgents.Core.Models.McpServersInheritance.None   => [],
                    DotnetAgents.Core.Models.McpServersInheritance.Custom => agent.McpServers,
                    _                                                      => []
                };

                var msgs = await store.GetMessagesAsync(userId, sessionId);
                var dto = new SessionRawDto(
                    agent.SystemPrompt,
                    new AgentRawDto(agent.Id, agent.Name, agent.Description, agent.ModelId,
                        agent.Tools, agent.Skills, mcpServers),
                    new ModelRawDto(model.Id, model.Provider, model.ModelName, model.Endpoint),
                    msgs.Select(m => new MessageDto(m.Id, m.Role, m.Content, m.ToolCalls, m.Timestamp)).ToArray());

                return Results.Ok(dto);
            }).WithName("GetSessionRaw").WithOpenApi();
    }

    private static SessionDto ToDto(Session s) =>
        new(s.Id, s.UserId, s.AgentId, s.Title, s.CreatedAt, s.UpdatedAt);
}
