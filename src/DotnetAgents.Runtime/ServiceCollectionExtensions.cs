using DotnetAgents.Core.Configuration;
using DotnetAgents.Core.Models;
using DotnetAgents.Core.Runtime;
using DotnetAgents.Core.Sessions;
using DotnetAgents.Runtime.Execution;
using DotnetAgents.Runtime.Mcp;
using DotnetAgents.Runtime.Providers;
using DotnetAgents.Tools.BuiltIn;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetAgents.Runtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotnetAgents(this IServiceCollection services, AgentsConfig config)
    {
        ConfigValidator.ValidateOrThrow(config);
        services.AddHttpClient();
        services.AddLogging();
        services.AddSingleton(config);
        services.AddSingleton<IConfigurationService>(sp => new ConfigurationService(sp.GetRequiredService<AgentsConfig>()));
        services.AddSingleton<ISessionStore>(_ => new JsonSessionStore(Path.GetFullPath(config.Sessions.Directory)));
        services.AddSingleton<IModelProviderFactory, ModelProviderFactory>();
        services.AddSingleton(sp => new BuiltInToolRegistry(config.Tools, sp.GetRequiredService<IHttpClientFactory>()));
        services.AddSingleton<IAgentRuntime, AgentExecutor>();
        services.AddSingleton<IMcpClientFactory, McpClientFactory>();
        services.AddSingleton<McpConnectionManager>();
        services.AddSingleton<IMcpConnectionManager>(sp => sp.GetRequiredService<McpConnectionManager>());
        services.AddHostedService(sp => sp.GetRequiredService<McpConnectionManager>());
        return services;
    }
}
