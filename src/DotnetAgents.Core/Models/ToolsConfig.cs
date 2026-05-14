namespace DotnetAgents.Core.Models;

public record ToolsConfig
{
    public ReadFileToolConfig? ReadFile { get; init; }
    public HttpGetToolConfig?  HttpGet  { get; init; }
}

public record ReadFileToolConfig
{
    public required string SandboxRoot { get; init; }
}

public record HttpGetToolConfig
{
    public required IReadOnlyList<string> Allowlist { get; init; }
}
