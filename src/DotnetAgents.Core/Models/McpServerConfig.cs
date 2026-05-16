namespace DotnetAgents.Core.Models;

public record McpServerConfig
{
    public required string Id { get; init; }
    public string? Command { get; init; }
    public IReadOnlyList<string> Args { get; init; } = [];
    public IReadOnlyDictionary<string, string> Env { get; init; } = new Dictionary<string, string>();
    public string? Url { get; init; }
    public bool Enabled { get; init; } = true;
    public int RetryLimit { get; init; } = 3;
    public int RetryInterval { get; init; } = 5;

    public virtual bool Equals(McpServerConfig? other) =>
        other is not null &&
        Id == other.Id &&
        Command == other.Command &&
        Url == other.Url &&
        Enabled == other.Enabled &&
        RetryLimit == other.RetryLimit &&
        RetryInterval == other.RetryInterval &&
        Args.SequenceEqual(other.Args) &&
        EnvEquals(Env, other.Env);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(Id); hc.Add(Command); hc.Add(Url);
        hc.Add(Enabled); hc.Add(RetryLimit); hc.Add(RetryInterval);
        foreach (var a in Args) hc.Add(a);
        foreach (var kv in Env.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        { hc.Add(kv.Key); hc.Add(kv.Value); }
        return hc.ToHashCode();
    }

    private static bool EnvEquals(IReadOnlyDictionary<string, string> a, IReadOnlyDictionary<string, string> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
        return true;
    }
}
