using DotnetAgents.Core.Models;
using Microsoft.Extensions.AI;

namespace DotnetAgents.Runtime.Providers;

public interface IModelProviderFactory
{
    IChatClient Create(ModelConfig model);
}
