using DotnetAgents.Core.Models;
namespace DotnetAgents.Core.Configuration;
public sealed class ConfigurationService(AgentsConfig config) : IConfigurationService
{
    private AgentsConfig _config = config;
    private readonly Lock _lock = new();

    public AgentsConfig Config { get { lock (_lock) return _config; } }
    public void Update(AgentsConfig config) { lock (_lock) _config = config; }
}
