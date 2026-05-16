using DotnetAgents.Core.Models;
namespace DotnetAgents.Core.Configuration;
public interface IConfigSaver
{
    Task SaveAsync(AgentsConfig config, CancellationToken ct = default);
}
