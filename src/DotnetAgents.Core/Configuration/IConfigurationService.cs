using DotnetAgents.Core.Models;
namespace DotnetAgents.Core.Configuration;
public interface IConfigurationService
{
    AgentsConfig Config { get; }
    void Update(AgentsConfig config);
}
