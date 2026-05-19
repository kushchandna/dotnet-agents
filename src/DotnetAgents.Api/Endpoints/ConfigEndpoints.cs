using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using DotnetAgents.Runtime.Mcp;

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

            var updatedAgent = FromRequest(req) with { Id = id };
            var newAgents = cfg.Config.Agents
                .Select(a => a.Id == id ? updatedAgent : a)
                .ToList();
            var newConfig = cfg.Config with { Agents = newAgents };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Ok(ToDto(updatedAgent));
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

            var updated = new UserConfig { Id = id, DisplayName = req.DisplayName };
            var newUsers = cfg.Config.Users
                .Select(u => u.Id == id ? updated : u)
                .ToList();
            var newConfig = cfg.Config with { Users = newUsers };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Ok(new UserDto(id, req.DisplayName));
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

        // status must be declared before {id} routes to avoid parameter matching
        app.MapGet("/api/config/mcp-servers/status", (IMcpConnectionManager mcpManager) =>
            mcpManager.GetStatus()
                .Select(s => new McpServerStatusDto(s.Id, s.State, s.Error, s.LastConnectedAt, s.RetryAttempt, s.ToolNames))
                .ToArray())
           .WithName("GetMcpServerStatuses").WithOpenApi();

        app.MapGet("/api/config/mcp-servers", (IConfigurationService cfg) =>
            cfg.Config.McpServers.Select(ToDto).ToArray())
           .WithName("ListConfigMcpServers").WithOpenApi();

        app.MapPost("/api/config/mcp-servers", async (
            UpsertMcpServerRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            IMcpConnectionManager mcpManager,
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
            await mcpManager.ReconfigureAsync(newConfig, ct);
            return Results.Created($"/api/config/mcp-servers/{req.Id}", ToDto(FromRequest(req)));
        }).WithName("CreateConfigMcpServer").WithOpenApi();

        app.MapPut("/api/config/mcp-servers/{id}", async (
            string id,
            UpsertMcpServerRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            IMcpConnectionManager mcpManager,
            CancellationToken ct) =>
        {
            if (!cfg.Config.McpServers.Any(s => s.Id == id))
                return Results.NotFound();

            var updatedServer = FromRequest(req) with { Id = id };
            var newServers = cfg.Config.McpServers
                .Select(s => s.Id == id ? updatedServer : s)
                .ToList();
            var newConfig = cfg.Config with { McpServers = newServers };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            await mcpManager.ReconfigureAsync(newConfig, ct);
            return Results.Ok(ToDto(updatedServer));
        }).WithName("UpdateConfigMcpServer").WithOpenApi();

        app.MapDelete("/api/config/mcp-servers/{id}", async (
            string id,
            IConfigurationService cfg,
            IConfigSaver saver,
            IMcpConnectionManager mcpManager,
            CancellationToken ct) =>
        {
            if (!cfg.Config.McpServers.Any(s => s.Id == id))
                return Results.NotFound();

            var newServers = cfg.Config.McpServers.Where(s => s.Id != id).ToList();
            var newConfig = cfg.Config with { McpServers = newServers };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            await mcpManager.ReconfigureAsync(newConfig, ct);
            return Results.NoContent();
        }).WithName("DeleteConfigMcpServer").WithOpenApi();

        // ── Models ───────────────────────────────────────────────────────────

        app.MapGet("/api/config/models", (IConfigurationService cfg) =>
            cfg.Config.Models.Select(ToDto).ToArray())
           .WithName("ListConfigModels").WithOpenApi();

        app.MapPost("/api/config/models", async (
            UpsertModelRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (cfg.Config.Models.Any(m => m.Id == req.Id))
                return Results.Conflict(new { message = $"Model '{req.Id}' already exists." });

            var newModel = FromRequest(req);
            var newModels = cfg.Config.Models.Append(newModel).ToList();
            var newConfig = cfg.Config with { Models = newModels };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Created($"/api/config/models/{req.Id}", ToDto(newModel));
        }).WithName("CreateConfigModel").WithOpenApi();

        app.MapPut("/api/config/models/{id}", async (
            string id,
            UpsertModelRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (!cfg.Config.Models.Any(m => m.Id == id))
                return Results.NotFound();

            var updatedModel = FromRequest(req) with { Id = id };
            var newModels = cfg.Config.Models
                .Select(m => m.Id == id ? updatedModel : m)
                .ToList();
            var newConfig = cfg.Config with { Models = newModels };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Ok(ToDto(updatedModel));
        }).WithName("UpdateConfigModel").WithOpenApi();

        app.MapDelete("/api/config/models/{id}", async (
            string id,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            if (!cfg.Config.Models.Any(m => m.Id == id))
                return Results.NotFound();

            var newModels = cfg.Config.Models.Where(m => m.Id != id).ToList();
            var newConfig = cfg.Config with { Models = newModels };
            var errors = ConfigValidator.Validate(newConfig);
            if (errors.Count > 0)
                return Results.BadRequest(errors.Select(e => new { e.Path, e.Message }));

            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.NoContent();
        }).WithName("DeleteConfigModel").WithOpenApi();

        app.MapPost("/api/config/mcp-servers/{id}/retry", async (
            string id,
            IConfigurationService cfg,
            IMcpConnectionManager mcpManager,
            CancellationToken ct) =>
        {
            var server = cfg.Config.McpServers.FirstOrDefault(s => s.Id == id);
            if (server is null) return Results.NotFound();
            if (!server.Enabled) return Results.BadRequest(new { message = $"MCP server '{id}' is disabled." });

            await mcpManager.RetryAsync(id, ct);
            return Results.Accepted();
        }).WithName("RetryMcpServer").WithOpenApi();
    }

    private static AgentSettingsDto ToDto(AgentConfig a) =>
        new(a.Id, a.Name, a.Description, a.ModelId, a.SystemPrompt, a.Tools, a.SkillsInheritance, a.Skills, a.McpServersInheritance, a.McpServers);

    private static AgentConfig FromRequest(UpsertAgentRequest r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        ModelId = r.ModelId,
        SystemPrompt = r.SystemPrompt,
        Tools = r.Tools ?? [],
        SkillsInheritance = r.SkillsInheritance,
        Skills = r.Skills ?? [],
        McpServersInheritance = r.McpServersInheritance,
        McpServers = r.McpServers ?? []
    };

    private static McpServerDto ToDto(McpServerConfig s) =>
        new(s.Id, s.Command, s.Args, s.Env, s.Url, s.Enabled, s.RetryLimit, s.RetryInterval);

    private static McpServerConfig FromRequest(UpsertMcpServerRequest r) => new()
    {
        Id = r.Id,
        Command = r.Command,
        Args = r.Args ?? [],
        Env = r.Env ?? new Dictionary<string, string>(),
        Url = r.Url,
        Enabled = r.Enabled ?? true,
        RetryLimit = r.RetryLimit ?? 3,
        RetryInterval = r.RetryInterval ?? 5
    };

    private static ModelSettingsDto ToDto(ModelConfig m) =>
        new(m.Id, m.Provider, m.ModelName, m.Endpoint, m.ApiKeyEnvVar);

    private static ModelConfig FromRequest(UpsertModelRequest r) => new()
    {
        Id = r.Id,
        Provider = r.Provider,
        ModelName = r.ModelName,
        Endpoint = r.Endpoint,
        ApiKeyEnvVar = r.ApiKeyEnvVar
    };
}
