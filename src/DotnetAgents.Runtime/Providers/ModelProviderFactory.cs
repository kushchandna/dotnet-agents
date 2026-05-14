using System.ClientModel;
using DotnetAgents.Core.Models;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace DotnetAgents.Runtime.Providers;

public sealed class ModelProviderFactory : IModelProviderFactory
{
    public IChatClient Create(ModelConfig model) => model.Provider switch
    {
        ModelProvider.OpenAI => CreateOpenAI(model),
        ModelProvider.OpenAICompatible => CreateOpenAICompatible(model),
        ModelProvider.Ollama => CreateOllama(model),
        _ => throw new InvalidOperationException($"Unknown provider: {model.Provider}")
    };

    private static IChatClient CreateOpenAI(ModelConfig m)
    {
        var key = RequireEnv(m.ApiKeyEnvVar
            ?? throw new InvalidOperationException("apiKeyEnvVar is required for openai"));
        var client = new OpenAIClient(new ApiKeyCredential(key));
        return client.GetChatClient(m.ModelName).AsIChatClient();
    }

    private static IChatClient CreateOpenAICompatible(ModelConfig m)
    {
        var endpoint = new Uri(m.Endpoint
            ?? throw new InvalidOperationException("endpoint is required for openai-compatible"));
        var key = !string.IsNullOrWhiteSpace(m.ApiKeyEnvVar) ? RequireEnv(m.ApiKeyEnvVar) : "no-key";
        var options = new OpenAIClientOptions { Endpoint = endpoint };
        var client = new OpenAIClient(new ApiKeyCredential(key), options);
        return client.GetChatClient(m.ModelName).AsIChatClient();
    }

    private static IChatClient CreateOllama(ModelConfig m)
    {
        var endpoint = new Uri(m.Endpoint ?? "http://localhost:11434");
        return new OllamaApiClient(endpoint, m.ModelName);
    }

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Environment variable '{name}' is not set.");
}
