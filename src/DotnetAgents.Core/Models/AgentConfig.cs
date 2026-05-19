namespace DotnetAgents.Core.Models;

public record AgentConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string ModelId { get; init; }
    public string? SystemPrompt { get; init; }
    public IReadOnlyList<string> Tools { get; init; } = [];
    public IReadOnlyList<string> Skills { get; init; } = [];
    public SkillsInheritance SkillsInheritance { get; init; } = SkillsInheritance.All;
    public McpServersInheritance McpServersInheritance { get; init; } = McpServersInheritance.All;
    public IReadOnlyList<string> McpServers { get; init; } = [];
    public IReadOnlyList<string> KnowledgeBases { get; init; } = [];
}
