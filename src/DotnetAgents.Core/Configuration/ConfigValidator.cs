using System.Text.RegularExpressions;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Configuration;

public static class ConfigValidator
{
    private static readonly HashSet<string> KnownTools = new(StringComparer.Ordinal)
    {
        "get_current_time", "echo", "read_file", "http_get"
    };

    private static readonly Regex EnvVarPattern = new("^[A-Z_][A-Z0-9_]*$", RegexOptions.Compiled);

    public static IReadOnlyList<ConfigValidationError> Validate(AgentsConfig cfg)
    {
        var errors = new List<ConfigValidationError>();
        void Err(string path, string msg) => errors.Add(new(path, msg));

        // Models
        if (cfg.Models.Count == 0) Err("models", "must not be empty");
        var modelIds = new HashSet<string>();
        for (int i = 0; i < cfg.Models.Count; i++)
        {
            var m = cfg.Models[i];
            var p = $"models[{i}]";
            if (string.IsNullOrWhiteSpace(m.Id)) Err($"{p}.id", "is required");
            else if (!modelIds.Add(m.Id)) Err($"{p}.id", $"duplicate id '{m.Id}'");
            if (string.IsNullOrWhiteSpace(m.ModelName)) Err($"{p}.modelName", "is required");
            if (m.Provider == ModelProvider.OpenAI && string.IsNullOrWhiteSpace(m.ApiKeyEnvVar))
                Err($"{p}.apiKeyEnvVar", "apiKeyEnvVar is required for provider 'openai'");
            if (m.Provider == ModelProvider.OpenAICompatible && string.IsNullOrWhiteSpace(m.Endpoint))
                Err($"{p}.endpoint", "endpoint is required for provider 'openai-compatible'");
            if (!string.IsNullOrWhiteSpace(m.ApiKeyEnvVar) && !EnvVarPattern.IsMatch(m.ApiKeyEnvVar))
                Err($"{p}.apiKeyEnvVar", $"'{m.ApiKeyEnvVar}' is not a valid env var name");
        }

        // Agents
        if (cfg.Agents.Count == 0) Err("agents", "must not be empty");
        var agentIds = new HashSet<string>();
        for (int i = 0; i < cfg.Agents.Count; i++)
        {
            var a = cfg.Agents[i];
            var p = $"agents[{i}]";
            if (string.IsNullOrWhiteSpace(a.Id)) Err($"{p}.id", "is required");
            else if (!agentIds.Add(a.Id)) Err($"{p}.id", $"duplicate id '{a.Id}'");
            if (string.IsNullOrWhiteSpace(a.Name)) Err($"{p}.name", "is required");
            if (string.IsNullOrWhiteSpace(a.ModelId)) Err($"{p}.modelId", "is required");
            else if (!modelIds.Contains(a.ModelId)) Err($"{p}.modelId", $"references unknown model '{a.ModelId}'");
            for (int j = 0; j < a.Tools.Count; j++)
                if (!KnownTools.Contains(a.Tools[j]))
                    Err($"{p}.tools[{j}]", $"unknown built-in tool '{a.Tools[j]}'");
        }

        // Users
        if (cfg.Users.Count == 0) Err("users", "must not be empty");
        var userIds = new HashSet<string>();
        for (int i = 0; i < cfg.Users.Count; i++)
        {
            var u = cfg.Users[i];
            var p = $"users[{i}]";
            if (string.IsNullOrWhiteSpace(u.Id)) Err($"{p}.id", "is required");
            else if (!userIds.Add(u.Id)) Err($"{p}.id", $"duplicate id '{u.Id}'");
            if (string.IsNullOrWhiteSpace(u.DisplayName)) Err($"{p}.displayName", "is required");
        }

        // Sessions
        if (string.IsNullOrWhiteSpace(cfg.Sessions?.Directory))
            Err("sessions.directory", "is required");

        // MCP servers (optional root registry)
        var mcpServerIds = new HashSet<string>();
        for (int i = 0; i < cfg.McpServers.Count; i++)
        {
            var s = cfg.McpServers[i];
            var p = $"mcpServers[{i}]";
            if (string.IsNullOrWhiteSpace(s.Id)) Err($"{p}.id", "is required");
            else if (!mcpServerIds.Add(s.Id)) Err($"{p}.id", $"duplicate id '{s.Id}'");
            if (string.IsNullOrWhiteSpace(s.Command) && string.IsNullOrWhiteSpace(s.Url))
                Err(p, "must specify either 'command' (stdio) or 'url' (SSE/HTTP)");
            if (s.RetryLimit < 0) Err($"{p}.retryLimit", "must be ≥ 0");
            if (s.RetryInterval < 1) Err($"{p}.retryInterval", "must be ≥ 1");
        }

        // Agent MCP inheritance
        for (int i = 0; i < cfg.Agents.Count; i++)
        {
            var a = cfg.Agents[i];
            var p = $"agents[{i}]";
            if (a.McpServersInheritance != Models.McpServersInheritance.Custom && a.McpServers.Count > 0)
                Err($"{p}.mcpServers", $"non-empty mcpServers list has no effect when mcpServersInheritance is '{a.McpServersInheritance.ToString().ToLowerInvariant()}'; set it to 'custom' or remove the list");
            if (a.McpServersInheritance == Models.McpServersInheritance.Custom)
                for (int j = 0; j < a.McpServers.Count; j++)
                    if (!mcpServerIds.Contains(a.McpServers[j]))
                        Err($"{p}.mcpServers[{j}]", $"references unknown MCP server '{a.McpServers[j]}'");
        }

        // Tools (optional)
        if (cfg.Tools?.ReadFile is not null && string.IsNullOrWhiteSpace(cfg.Tools.ReadFile.SandboxRoot))
            Err("tools.readFile.sandboxRoot", "is required");
        if (cfg.Tools?.HttpGet is not null && cfg.Tools.HttpGet.Allowlist.Count == 0)
            Err("tools.httpGet.allowlist", "must not be empty");

        return errors;
    }

    public static void ValidateOrThrow(AgentsConfig cfg)
    {
        var errors = Validate(cfg);
        if (errors.Count == 0) return;
        throw new InvalidDataException(
            "Configuration is invalid:\n" + string.Join("\n", errors.Select(e => "  - " + e)));
    }
}
