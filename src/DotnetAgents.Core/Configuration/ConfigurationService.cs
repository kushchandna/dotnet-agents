using DotnetAgents.Core.Models;
namespace DotnetAgents.Core.Configuration;
public sealed class ConfigurationService(AgentsConfig config) : IConfigurationService
{
    public AgentsConfig Config { get; } = config;
}
