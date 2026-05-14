namespace DotnetAgents.Core.Runtime;

public abstract record AgentStreamUpdate;
public sealed record DeltaUpdate(string Content) : AgentStreamUpdate;
public sealed record ToolCallUpdate(string CallId, string Name, string Arguments) : AgentStreamUpdate;
public sealed record ToolResultUpdate(string CallId, string Name, string Result) : AgentStreamUpdate;
public sealed record DoneUpdate(string MessageId) : AgentStreamUpdate;
public sealed record ErrorUpdate(string Message) : AgentStreamUpdate;
