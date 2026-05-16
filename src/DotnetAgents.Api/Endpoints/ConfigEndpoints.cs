using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Api.Endpoints;

public static class ConfigEndpoints
{
    public static void Map(WebApplication app)
    {
        // ── Agents ──────────────────────────────────────────────────────────

        app.MapGet("/api/config/agents", (IConfigurationService cfg) =>
            cfg.Config.Agents.Select(ToDto).ToArray())
           .WithName("ListConfigAgents").WithOpenApi();

        app.MapPost("/api/config/agents", async (
            UpsertAgentRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (cfg.Config.Agents.Any(a => a.Id == req.Id))
                return Results.Conflict(new { message = $"Agent '{req.Id}' already exists." });

            var newAgents = cfg.Config.Agents.Append(FromRequest(req)).ToList();
            var newConfig = cfg.Config with { Agents = newAgents };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Created($"/api/config/agents/{req.Id}", ToDto(FromRequest(req)));
        }).WithName("CreateConfigAgent").WithOpenApi();

        app.MapPut("/api/config/agents/{id}", async (
            string id,
            UpsertAgentRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (!cfg.Config.Agents.Any(a => a.Id == id))
                return Results.NotFound();

            var newAgents = cfg.Config.Agents
                .Select(a => a.Id == id ? FromRequest(req) : a)
                .ToList();
            var newConfig = cfg.Config with { Agents = newAgents };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Ok(ToDto(FromRequest(req)));
        }).WithName("UpdateConfigAgent").WithOpenApi();

        app.MapDelete("/api/config/agents/{id}", async (
            string id,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (!cfg.Config.Agents.Any(a => a.Id == id))
                return Results.NotFound();

            var newAgents = cfg.Config.Agents.Where(a => a.Id != id).ToList();
            var newConfig = cfg.Config with { Agents = newAgents };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.NoContent();
        }).WithName("DeleteConfigAgent").WithOpenApi();

        // ── Users ────────────────────────────────────────────────────────────

        app.MapGet("/api/config/users", (IConfigurationService cfg) =>
            cfg.Config.Users.Select(u => new UserDto(u.Id, u.DisplayName)).ToArray())
           .WithName("ListConfigUsers").WithOpenApi();

        app.MapPost("/api/config/users", async (
            UpsertUserRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (cfg.Config.Users.Any(u => u.Id == req.Id))
                return Results.Conflict(new { message = $"User '{req.Id}' already exists." });

            var newUser = new UserConfig { Id = req.Id, DisplayName = req.DisplayName };
            var newUsers = cfg.Config.Users.Append(newUser).ToList();
            var newConfig = cfg.Config with { Users = newUsers };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Created($"/api/config/users/{req.Id}", new UserDto(req.Id, req.DisplayName));
        }).WithName("CreateConfigUser").WithOpenApi();

        app.MapPut("/api/config/users/{id}", async (
            string id,
            UpsertUserRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (!cfg.Config.Users.Any(u => u.Id == id))
                return Results.NotFound();

            var updated = new UserConfig { Id = req.Id, DisplayName = req.DisplayName };
            var newUsers = cfg.Config.Users
                .Select(u => u.Id == id ? updated : u)
                .ToList();
            var newConfig = cfg.Config with { Users = newUsers };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Ok(new UserDto(req.Id, req.DisplayName));
        }).WithName("UpdateConfigUser").WithOpenApi();

        app.MapDelete("/api/config/users/{id}", async (
            string id,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (!cfg.Config.Users.Any(u => u.Id == id))
                return Results.NotFound();

            var newUsers = cfg.Config.Users.Where(u => u.Id != id).ToList();
            var newConfig = cfg.Config with { Users = newUsers };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.NoContent();
        }).WithName("DeleteConfigUser").WithOpenApi();

        // ── MCP Servers ──────────────────────────────────────────────────────

        app.MapGet("/api/config/mcp-servers", (IConfigurationService cfg) =>
            cfg.Config.McpServers.Select(ToDto).ToArray())
           .WithName("ListConfigMcpServers").WithOpenApi();

        app.MapPost("/api/config/mcp-servers", async (
            UpsertMcpServerRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (cfg.Config.McpServers.Any(s => s.Id == req.Id))
                return Results.Conflict(new { message = $"MCP server '{req.Id}' already exists." });

            var newServers = cfg.Config.McpServers.Append(FromRequest(req)).ToList();
            var newConfig = cfg.Config with { McpServers = newServers };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Created($"/api/config/mcp-servers/{req.Id}", ToDto(FromRequest(req)));
        }).WithName("CreateConfigMcpServer").WithOpenApi();

        app.MapPut("/api/config/mcp-servers/{id}", async (
            string id,
            UpsertMcpServerRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (!cfg.Config.McpServers.Any(s => s.Id == id))
                return Results.NotFound();

            var newServers = cfg.Config.McpServers
                .Select(s => s.Id == id ? FromRequest(req) : s)
                .ToList();
            var newConfig = cfg.Config with { McpServers = newServers };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Ok(ToDto(FromRequest(req)));
        }).WithName("UpdateConfigMcpServer").WithOpenApi();

        app.MapDelete("/api/config/mcp-servers/{id}", async (
            string id,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (!cfg.Config.McpServers.Any(s => s.Id == id))
                return Results.NotFound();

            var newServers = cfg.Config.McpServers.Where(s => s.Id != id).ToList();
            var newConfig = cfg.Config with { McpServers = newServers };
            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.NoContent();
        }).WithName("DeleteConfigMcpServer").WithOpenApi();
    }

    private static AgentSettingsDto ToDto(AgentConfig a) =>
        new(a.Id, a.Name, a.Description, a.ModelId, a.SystemPrompt, a.Tools, a.McpServersInheritance, a.McpServers);

    private static AgentConfig FromRequest(UpsertAgentRequest r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        ModelId = r.ModelId,
        SystemPrompt = r.SystemPrompt,
        Tools = r.Tools ?? [],
        McpServersInheritance = r.McpServersInheritance,
        McpServers = r.McpServers ?? []
    };

    private static McpServerDto ToDto(McpServerConfig s) =>
        new(s.Id, s.Command, s.Args, s.Env, s.Url);

    private static McpServerConfig FromRequest(UpsertMcpServerRequest r) => new()
    {
        Id = r.Id,
        Command = r.Command,
        Args = r.Args ?? [],
        Env = r.Env ?? new Dictionary<string, string>(),
        Url = r.Url
    };
}
