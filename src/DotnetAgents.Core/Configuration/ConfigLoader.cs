using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetAgents.Core.Models;

namespace DotnetAgents.Core.Configuration;

public static class ConfigLoader
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower, allowIntegerValues: false) },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static async Task<AgentsConfig> LoadAsync(
        string? explicitPath = null, CancellationToken ct = default)
    {
        var path = ResolvePath(explicitPath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}", path);

        await using var fs = File.OpenRead(path);
        var cfg = await JsonSerializer.DeserializeAsync<AgentsConfig>(fs, JsonOptions, ct)
                  ?? throw new InvalidDataException("Config deserialized to null");
        return cfg;
    }

    public static string ResolvePath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath)) return explicitPath;
        var env = Environment.GetEnvironmentVariable("DOTNET_AGENTS_CONFIG");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        return Path.Combine(Directory.GetCurrentDirectory(), "config.json");
    }
}
