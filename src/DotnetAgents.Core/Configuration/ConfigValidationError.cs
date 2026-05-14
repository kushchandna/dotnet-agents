namespace DotnetAgents.Core.Configuration;

public record ConfigValidationError(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}
