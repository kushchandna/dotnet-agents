using DotnetAgents.Api.Dto;
using DotnetAgents.Core.Configuration;

namespace DotnetAgents.Api.Endpoints;

public static class UsersEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/users", (IConfigurationService cfg) =>
            cfg.Config.Users.Select(u => new UserDto(u.Id, u.DisplayName)).ToArray())
           .WithName("ListUsers").WithOpenApi();
    }
}
