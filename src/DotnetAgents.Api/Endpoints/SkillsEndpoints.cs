using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Skills;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAgents.Api.Endpoints;

public static class SkillsEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/config/skill-directories", (IConfigurationService cfg) =>
            cfg.Config.SkillDirectories.ToArray())
           .WithName("ListSkillDirectories").WithOpenApi();

        app.MapPost("/api/config/skill-directories", async (
            SkillDirectoryRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            var newDirs = cfg.Config.SkillDirectories.Append(req.Path).ToList();
            var newConfig = cfg.Config with { SkillDirectories = newDirs };
            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.Ok(newConfig.SkillDirectories.ToArray());
        }).WithName("AddSkillDirectory").WithOpenApi();

        app.MapDelete("/api/config/skill-directories", async (
            [FromBody] SkillDirectoryRequest req,
            IConfigurationService cfg,
            IConfigSaver saver,
            CancellationToken ct) =>
        {
            var newDirs = cfg.Config.SkillDirectories.Where(d => d != req.Path).ToList();
            var newConfig = cfg.Config with { SkillDirectories = newDirs };
            cfg.Update(newConfig);
            await saver.SaveAsync(newConfig, ct);
            return Results.NoContent();
        }).WithName("RemoveSkillDirectory").WithOpenApi();

        app.MapGet("/api/config/skills", async (
            IConfigurationService cfg,
            ISkillDiscoveryService skillDiscovery,
            CancellationToken ct) =>
        {
            var skills = await skillDiscovery.GetAllSkillsAsync(cfg.Config.SkillDirectories, ct);
            return skills.Select(s => new SkillDto(s.Id, s.Description)).ToArray();
        }).WithName("ListSkills").WithOpenApi();
    }
}

internal record SkillDirectoryRequest(string Path);
